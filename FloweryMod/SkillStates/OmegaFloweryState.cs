using EntityStates;
using FloweryMod.Modules;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace FloweryMod.SkillStates
{
    /// <summary>
    /// OMEGA FLOWERY. Erupts in vines and then hands the next several seconds over to the OMEGA
    /// form: flight, +25% damage, and LAST JARONA in the special slot.
    ///
    /// The TP is not spent up front. Like Void Fiend's corruption, the meter drains for as long
    /// as the form is up, and the form ends when the meter is empty - so the buff's duration is
    /// simply the TP you had, divided by the drain rate. The TP at activation is networked, so
    /// the server derives the exact same duration the owner's bar is showing.
    /// </summary>
    public class OmegaFloweryState : BaseFloweryState, IFloweryMotorState
    {
        public static float recoveryDuration = 0.4f;
        public static float baseRadius = 9f;
        public static float radiusPerTension = 0.07f;
        public static float damagePerTension = 0.08f;
        public static float procCoefficient = 1f;

        /// <summary>TP held at activation. Networked; drives both the blast size and the duration.</summary>
        public float storedTension;

        private bool hasErupted;
        private bool gravityDisabled;
        private float chargeDuration;

        public override void OnEnter()
        {
            if (isAuthority)
            {
                var meter = GetComponent<Components.TensionController>();
                if (meter != null) storedTension = meter.Tension;
            }

            base.OnEnter();

            chargeDuration = Mathf.Max(0f, FloweryConfig.OmegaChargeDuration.Value);

            StartAimMode(2f, false);
            PlayPose(FloweryAnimations.OmegaTransform, 0.1f);
            Sounds.PlayAt(Sounds.SpecialTransform, gameObject);

            if (characterBody != null) characterBody.SetAimTimer(2.5f);

            // Lift off, and stay untouchable for the wind-up: the transformation is a commitment,
            // and being killed halfway through it while standing still would be miserable.
            if (characterMotor != null)
            {
                gravityDisabled = true;
                Gravity.Suspend(characterMotor);
                if (characterMotor.Motor != null) characterMotor.Motor.ForceUnground();
            }

            if (NetworkServer.active && characterBody != null)
            {
                characterBody.AddTimedBuff(RoR2Content.Buffs.HiddenInvincibility, chargeDuration + 0.1f);
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (!hasErupted)
            {
                if (characterMotor != null)
                {
                    characterMotor.velocity = Vector3.up * FloweryConfig.OmegaRiseSpeed.Value;
                }

                if (fixedAge >= chargeDuration)
                {
                    hasErupted = true;
                    // The transform pose covers the wind-up only; once he has erupted he is
                    // OMEGA and flying, so hand him straight back to the flight pose rather
                    // than leaving him frozen mid-transformation for the recovery.
                    ReleasePose();
                    Erupt();
                }
                return;
            }

            // After the eruption OMEGA flight takes over, so just hang there for the recovery.
            if (characterMotor != null) characterMotor.velocity = Vector3.zero;

            if (fixedAge >= chargeDuration + recoveryDuration && isAuthority)
            {
                outer.SetNextStateToMain();
            }
        }

        public override void OnExit()
        {
            // Every other state ends on the flight pose; this one has to as well, or an
            // interruption during the wind-up leaves him stuck mid-transformation.
            ReleasePose();

            if (gravityDisabled)
            {
                // OMEGA flight suspends gravity again on its own next frame.
                Gravity.Restore(characterMotor);
                gravityDisabled = false;
            }

            base.OnExit();
        }

        public override void OnSerialize(NetworkWriter writer)
        {
            base.OnSerialize(writer);
            writer.Write(storedTension);
        }

        public override void OnDeserialize(NetworkReader reader)
        {
            base.OnDeserialize(reader);
            storedTension = reader.ReadSingle();
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.PrioritySkill;

        private void Erupt()
        {
            if (characterBody == null) return;

            float radius = baseRadius + storedTension * radiusPerTension;
            float damageCoefficient = FloweryConfig.OmegaBlastDamage.Value + storedTension * damagePerTension;

            SpawnEruptionVisual(radius);

            if (!NetworkServer.active) return;

            new BlastAttack
            {
                attacker = gameObject,
                inflictor = gameObject,
                teamIndex = characterBody.teamComponent != null
                    ? characterBody.teamComponent.teamIndex
                    : TeamIndex.Player,
                attackerFiltering = AttackerFiltering.NeverHitSelf,
                position = characterBody.corePosition,
                radius = radius,
                baseDamage = damageStat * damageCoefficient,
                baseForce = 1600f,
                bonusForce = Vector3.up * 900f,
                crit = Util.CheckRoll(critStat, characterBody.master),
                procCoefficient = procCoefficient,
                falloffModel = BlastAttack.FalloffModel.SweetSpot,
                damageColorIndex = DamageColorIndex.Poison,
                damageType = FloweryDamage.Of(DamageType.Stun1s, DamageSource.Special),
            }.Fire();

            // The meter and the buff have to run out together, so both come from one number.
            float drainRate = Mathf.Max(0.1f, FloweryConfig.OmegaDrainPerSecond.Value);
            float formDuration = storedTension / drainRate;

            if (Buffs.Omega != null && formDuration > 0f)
            {
                characterBody.AddTimedBuff(Buffs.Omega, formDuration);
            }
        }

        private void SpawnEruptionVisual(float radius)
        {
            if (FloweryAssets.OmegaEruptionEffect == null) return;

            FloweryEffects.Spawn(FloweryAssets.OmegaEruptionEffect, new EffectData
            {
                origin = characterBody.corePosition,
                scale = radius,
                rotation = Quaternion.identity,
            }, false);
        }
    }
}
