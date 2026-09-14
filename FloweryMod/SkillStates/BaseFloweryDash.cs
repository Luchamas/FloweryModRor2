using System.Collections.Generic;
using EntityStates;
using FloweryMod.Modules;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace FloweryMod.SkillStates
{
    /// <summary>
    /// Shared behaviour for Flowery's two dashes: he breaks straight through whatever is in the
    /// way, hitting each enemy exactly once and taking TP back for the trouble.
    ///
    /// Gravity stays off for the whole dash so the line is clean, which matters most when the
    /// dash starts in mid-air or during OMEGA flight.
    /// </summary>
    public abstract class BaseFloweryDash : BaseFloweryState, IFloweryMotorState
    {
        public static float hitRadius = 2.8f;
        public static float procCoefficient = 0.8f;
        public static float exitSpeedFraction = 0.25f;

        private readonly HashSet<HealthComponent> alreadyHit = new HashSet<HealthComponent>();
        private bool gravityDisabled;

        protected Vector3 dashDirection;

        protected abstract float DashSpeed { get; }
        protected abstract float DashDuration { get; }
        protected abstract float DamageCoefficient { get; }
        protected abstract float TensionPerEnemy { get; }
        /// <summary>Voice clip folder to shout on the dash, or null when another state owns it.</summary>
        protected abstract string VoiceCategory { get; }

        /// <summary>
        /// The pose to hold for the length of the dash. Virtual rather than abstract because
        /// there is an obvious answer - San Frandisco's launch simply wears it - and only Jarona
        /// overrides, to roll between its three.
        /// </summary>
        protected virtual string DashPose => FloweryAnimations.Jarona;

        public override void OnEnter()
        {
            base.OnEnter();

            Ray aimRay = GetAimRay();
            dashDirection = aimRay.direction.normalized;
            if (dashDirection.sqrMagnitude < 0.001f) dashDirection = transform.forward;

            Sounds.PlayAt(VoiceCategory, gameObject);
            // Jarona is the pose every bundle has carried since the first one, so it doubles as
            // the fallback for a dash that asks for a newer one.
            PlayPose(DashPose, 0.08f, FloweryAnimations.Jarona);

            LockFacing(dashDirection);

            if (characterMotor != null)
            {
                gravityDisabled = true;
                Gravity.Suspend(characterMotor);
                characterMotor.disableAirControlUntilCollision = false;
                characterMotor.velocity = Vector3.zero;
            }

            SpawnDashEffect();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (characterMotor != null) characterMotor.velocity = dashDirection * DashSpeed;

            // Idempotent: the same rotation, every frame, with no other writer left to race.
            LockFacing(dashDirection);

            DamagePassedThrough();

            if (fixedAge >= DashDuration && isAuthority)
            {
                outer.SetNextStateToMain();
            }
        }

        public override void OnExit()
        {
            ReleasePose();

            if (characterMotor != null)
            {
                if (gravityDisabled)
                {
                    Gravity.Restore(characterMotor);
                    gravityDisabled = false;
                }

                // Keep a little momentum so the dash lands instead of stopping dead.
                characterMotor.velocity = dashDirection * (DashSpeed * exitSpeedFraction);
            }

            base.OnExit();
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.PrioritySkill;

        protected virtual void SpawnDashEffect()
        {
            if (FloweryAssets.JaronaChargeEffect == null) return;

            FloweryEffects.Spawn(FloweryAssets.JaronaChargeEffect, new EffectData
            {
                origin = transform.position + Vector3.up,
                scale = 2.5f,
                rotation = Quaternion.LookRotation(dashDirection),
                color = FloweryAssets.SoulBlue,
            }, false);
        }

        private void DamagePassedThrough()
        {
            if (characterBody == null) return;

            TeamIndex myTeam = characterBody.teamComponent != null
                ? characterBody.teamComponent.teamIndex
                : TeamIndex.Player;

            Collider[] colliders = Physics.OverlapSphere(characterBody.corePosition, hitRadius,
                                                         LayerIndex.entityPrecise.mask,
                                                         QueryTriggerInteraction.Collide);

            for (int i = 0; i < colliders.Length; i++)
            {
                var hurtBox = colliders[i].GetComponent<HurtBox>();
                if (hurtBox == null || hurtBox.healthComponent == null) continue;
                if (hurtBox.teamIndex == myTeam) continue;
                if (!hurtBox.healthComponent.alive) continue;
                if (!alreadyHit.Add(hurtBox.healthComponent)) continue;

                if (isAuthority && tension != null) tension.Add(TensionPerEnemy);

                if (!NetworkServer.active) continue;

                var damageInfo = new DamageInfo
                {
                    attacker = gameObject,
                    inflictor = gameObject,
                    damage = damageStat * DamageCoefficient,
                    crit = Util.CheckRoll(critStat, characterBody.master),
                    force = dashDirection * 900f,
                    position = hurtBox.transform.position,
                    procCoefficient = procCoefficient,
                    damageType = FloweryDamage.Of(DamageType.Stun1s, DamageSource.Utility),
                    damageColorIndex = DamageColorIndex.Default,
                };

                hurtBox.healthComponent.TakeDamage(damageInfo);
                GlobalEventManager.instance.OnHitEnemy(damageInfo, hurtBox.healthComponent.gameObject);
                GlobalEventManager.instance.OnHitAll(damageInfo, hurtBox.healthComponent.gameObject);

                if (FloweryAssets.PelletHitEffect != null)
                {
                    FloweryEffects.Spawn(FloweryAssets.PelletHitEffect, new EffectData
                    {
                        origin = hurtBox.transform.position,
                        scale = 1.5f,
                        rotation = Quaternion.LookRotation(dashDirection),
                    }, true);
                }
            }
        }
    }
}
