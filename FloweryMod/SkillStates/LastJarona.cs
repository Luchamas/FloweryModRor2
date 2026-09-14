using EntityStates;
using FloweryMod.Modules;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace FloweryMod.SkillStates
{
    /// <summary>
    /// LAST JARONA - the special slot while OMEGA is up.
    ///
    /// A committed high-speed dash that detonates on the first thing it touches. If it reaches
    /// the end of its travel without hitting anything it still detonates, so a whiffed cast
    /// leaves a crater instead of nothing at all.
    /// </summary>
    public class LastJarona : BaseFloweryState, IFloweryMotorState
    {
        public static float speed = 78f;
        public static float duration = 0.55f;
        public static float hitRadius = 3f;
        public static float blastRadius = 14f;
        public static float impactProcCoefficient = 1f;
        public static float blastProcCoefficient = 1f;

        private Vector3 dashDirection;
        private bool detonated;
        private bool gravityDisabled;
        private bool dashStarted;
        private float chargeDuration;

        public override void OnEnter()
        {
            base.OnEnter();

            chargeDuration = Mathf.Max(0f, FloweryConfig.LastJaronaChargeDuration.Value);

            Ray aimRay = GetAimRay();
            dashDirection = aimRay.direction.normalized;
            if (dashDirection.sqrMagnitude < 0.001f) dashDirection = transform.forward;

            StartAimMode(2f + chargeDuration, false);

            // The wind-up gets its own pose; the dive pose lands with the punch, in BeginDash.
            PlayPose(FloweryAnimations.ChargeLastJarona);

            // Fired at the start of the wind-up so the punch lands on "...Jarona!".
            Sounds.PlayAt(Sounds.SpecialLastJarona, gameObject);

            LockFacing(dashDirection);

            if (characterMotor != null)
            {
                gravityDisabled = true;
                Gravity.Suspend(characterMotor);
                characterMotor.velocity = Vector3.zero;
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (detonated) return;

            // Wind-up: hang in place, re-aiming, until the voice line reaches "Jarona!".
            if (fixedAge < chargeDuration)
            {
                if (characterMotor != null) characterMotor.velocity = Vector3.zero;
                AimAtCrosshair();
                return;
            }

            if (!dashStarted)
            {
                dashStarted = true;
                BeginDash();
            }

            if (characterMotor != null) characterMotor.velocity = dashDirection * speed;
            LockFacing(dashDirection);

            HurtBox victim = FindFirstVictim();
            if (victim != null)
            {
                Detonate(victim);
                return;
            }

            if (fixedAge >= chargeDuration + duration) Detonate(null);
        }

        /// <summary>Locks in the direction at the moment of the punch, not at the button press.</summary>
        private void BeginDash()
        {
            Ray aimRay = GetAimRay();
            dashDirection = aimRay.direction.normalized;
            if (dashDirection.sqrMagnitude < 0.001f) dashDirection = transform.forward;

            PlayPose(FloweryAnimations.LastJarona);
            // The direction was only just decided, and the model has one dash to catch up.
            LockFacing(dashDirection);

            if (FloweryAssets.JaronaChargeEffect != null)
            {
                FloweryEffects.Spawn(FloweryAssets.JaronaChargeEffect, new EffectData
                {
                    origin = transform.position + Vector3.up,
                    scale = 3.5f,
                    rotation = Quaternion.LookRotation(dashDirection),
                    color = FloweryAssets.Gold,
                }, false);
            }
        }

        private void AimAtCrosshair()
        {
            TrackFacing(GetAimRay().direction);
        }

        public override void OnExit()
        {
            ReleasePose();

            if (characterMotor != null)
            {
                if (gravityDisabled)
                {
                    // OMEGA flight turns gravity back off on its own next frame; handing it back
                    // here keeps the state honest if the buff expired mid-dash.
                    Gravity.Restore(characterMotor);
                    gravityDisabled = false;
                }
                characterMotor.velocity = dashDirection * (speed * 0.15f);
            }

            base.OnExit();
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.PrioritySkill;

        private HurtBox FindFirstVictim()
        {
            if (characterBody == null) return null;

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
                return hurtBox;
            }

            return null;
        }

        /// <summary>
        /// <paramref name="victim"/> is whatever the dash ran into, and eats the impact hit on
        /// top of the blast. A dash that timed out passes null and only leaves the blast.
        /// </summary>
        private void Detonate(HurtBox victim)
        {
            detonated = true;

            Vector3 origin = characterBody != null ? characterBody.corePosition : transform.position;

            if (FloweryAssets.JaronaExplosionEffect != null)
            {
                FloweryEffects.Spawn(FloweryAssets.JaronaExplosionEffect, new EffectData
                {
                    origin = origin,
                    scale = blastRadius,
                    rotation = Quaternion.identity,
                }, false);
            }

            if (NetworkServer.active && characterBody != null)
            {
                bool crit = Util.CheckRoll(critStat, characterBody.master);

                if (victim != null && victim.healthComponent != null && victim.healthComponent.alive)
                {
                    var impact = new DamageInfo
                    {
                        attacker = gameObject,
                        inflictor = gameObject,
                        damage = damageStat * FloweryConfig.LastJaronaImpactDamage.Value,
                        crit = crit,
                        force = dashDirection * 2000f,
                        position = victim.transform.position,
                        procCoefficient = impactProcCoefficient,
                        damageType = FloweryDamage.Of(DamageType.Stun1s, DamageSource.Special),
                        damageColorIndex = DamageColorIndex.Default,
                    };

                    victim.healthComponent.TakeDamage(impact);
                    GlobalEventManager.instance.OnHitEnemy(impact, victim.healthComponent.gameObject);
                    GlobalEventManager.instance.OnHitAll(impact, victim.healthComponent.gameObject);
                }

                new BlastAttack
                {
                    attacker = gameObject,
                    inflictor = gameObject,
                    teamIndex = characterBody.teamComponent != null
                        ? characterBody.teamComponent.teamIndex
                        : TeamIndex.Player,
                    attackerFiltering = AttackerFiltering.NeverHitSelf,
                    position = origin,
                    radius = blastRadius,
                    baseDamage = damageStat * FloweryConfig.LastJaronaBlastDamage.Value,
                    baseForce = 3000f,
                    bonusForce = Vector3.up * 1200f,
                    crit = crit,
                    procCoefficient = blastProcCoefficient,
                    falloffModel = BlastAttack.FalloffModel.SweetSpot,
                    damageColorIndex = DamageColorIndex.Default,
                    damageType = FloweryDamage.Of(DamageType.Stun1s, DamageSource.Special),
                }.Fire();
            }

            if (isAuthority) outer.SetNextStateToMain();
        }
    }
}
