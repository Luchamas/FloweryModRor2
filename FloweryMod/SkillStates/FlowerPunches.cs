using System.Collections.Generic;
using EntityStates;
using FloweryMod.Modules;
using RoR2;
using RoR2.Skills;
using UnityEngine;
using UnityEngine.Networking;

namespace FloweryMod.SkillStates
{
    /// <summary>
    /// Flower Punches - the melee primary. Alternating punches, driven off Loader's
    /// swing animations.
    ///
    /// Swings alternate arms through SteppedSkillDef, and the state blocks itself from being
    /// re-entered mid-swing so holding the button chains swings instead of restarting one.
    /// </summary>
    public class FlowerPunches : BaseFloweryState, SteppedSkillDef.IStepSetter
    {
        public const string HitBoxGroupName = "FloweryPunch";

        public static float attackStartFraction = 0.22f;
        public static float attackEndFraction = 0.55f;
        public static float procCoefficient = 1f;
        public static float pushAwayForce = 900f;

        private readonly List<HurtBox> hits = new List<HurtBox>();
        private OverlapAttack attack;
        private float duration;
        private bool attackStarted;
        private bool attackFinished;
        private int swingIndex;
        private bool healedThisSwing;
        private bool warnedAboutHitbox;

        /// <summary>Called by SteppedSkillDef so consecutive swings alternate arms.</summary>
        public void SetStep(int i) => swingIndex = i;

        public override void OnEnter()
        {
            base.OnEnter();

            duration = FloweryConfig.PunchDuration.Value / attackSpeedStat;

            StartAimMode(2f, false);
            if (characterBody != null) characterBody.SetAimTimer(1.5f);

            PlaySwingAnimation();
            Sounds.PlayAt(Sounds.Primary, gameObject);

            attack = InitMeleeOverlap(FloweryConfig.PunchDamage.Value, FloweryAssets.LashHitEffect,
                                      GetModelTransform(), HitBoxGroupName);
            attack.procCoefficient = procCoefficient;
            attack.pushAwayForce = pushAwayForce;
            attack.damageType = FloweryDamage.Of(DamageType.Generic, DamageSource.Primary);
            attack.damageColorIndex = DamageColorIndex.Default;

            if (isAuthority && attack.hitBoxGroup == null && !warnedAboutHitbox)
            {
                warnedAboutHitbox = true;
                Log.Error("Flower Punches found no HitBoxGroup named '" + HitBoxGroupName +
                          "' on the model - the swing will not damage anything.");
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            float fraction = duration > 0f ? fixedAge / duration : 1f;

            if (!attackStarted && fraction >= attackStartFraction)
            {
                attackStarted = true;
            }

            if (attackStarted && !attackFinished)
            {
                if (fraction >= attackEndFraction)
                {
                    attackFinished = true;
                }
                else if (isAuthority && attack != null)
                {
                    hits.Clear();
                    if (attack.Fire(hits))
                    {
                        if (tension != null) tension.AddForHits(hits.Count);
                        HealOnConnect();
                    }
                }
            }

            if (fixedAge >= duration && isAuthority)
            {
                outer.SetNextStateToMain();
            }
        }

        /// <summary>
        /// Skill, not Any. SkillDef.CanExecute never checks whether we are already in this state,
        /// so a primary that reports Any re-executes every frame the button is held: the crossfade
        /// restarts forever on frame one and the damage window is never reached. Reporting Skill
        /// blocks the primary (priority Any) from interrupting itself, while still letting the
        /// secondary, utility and special cut the swing short.
        /// </summary>
        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Skill;

        /// <summary>
        /// A punch that lands feeds him: one heal per swing, not per enemy caught in it, so a
        /// crowd is not worth more than a duel.
        ///
        /// Healing is server-side by nature - HealFraction does nothing on a client - and the
        /// overlap that detects the hit runs on the authority. Those are the same machine in
        /// singleplayer and for a host, so this covers both.
        /// </summary>
        private void HealOnConnect()
        {
            if (healedThisSwing || characterBody == null) return;
            healedThisSwing = true;

            float fraction = FloweryConfig.PunchHealFraction.Value;
            if (fraction <= 0f || !NetworkServer.active) return;

            HealthComponent health = characterBody.healthComponent;
            if (health != null && health.alive) health.HealFraction(fraction, default(ProcChainMask));
        }

        private void PlaySwingAnimation()
        {
            // Alternate hands each swing, the way Loader's fists do.
            string state = swingIndex % 2 == 0 ? FloweryAnimations.PunchLeft
                                               : FloweryAnimations.PunchRight;
            // Both the clip and the blend into it are scaled by attack speed, so the swing
            // fills the state at any speed instead of being cut off mid-extension. The two
            // punch clips already end on the other hand's opening pose, so a held primary
            // has almost nothing to blend across.
            PlayPose(state, 0.05f / Mathf.Max(attackSpeedStat, 0.1f));
            SetPoseSpeed(attackSpeedStat);
        }

        public override void OnExit()
        {
            // Not PlayFly(): the swing runs on the "Weapon" machine and a charge or a dash on
            // "Body" does not end it, so a swing that finishes on its own must not yank the
            // pose back from whatever is still running.
            ReleasePose();
            base.OnExit();
        }
    }
}
