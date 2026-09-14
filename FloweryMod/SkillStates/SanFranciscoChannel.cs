using EntityStates;
using FloweryMod.Modules;
using RoR2;
using UnityEngine;

namespace FloweryMod.SkillStates
{
    /// <summary>
    /// "Here I come San Frandisco!" - the wind-up half of the utility.
    ///
    /// A fixed channel, not a hold-to-charge: Flowery hangs completely still for the full
    /// duration and then launches at full power regardless of what the player does with the
    /// button. He hovers whether he started on the ground or in mid-air, which is what makes the
    /// move usable as an escape as well as an approach.
    /// </summary>
    public class SanFranciscoChannel : BaseFloweryState, IFloweryMotorState
    {
        // The cape stays on for the whole channel - he hangs in the air and it hangs with him.
        // That is decided by the pose now (FloweryAnimations.WearsCape), not by this state.
        private bool gravityDisabled;

        public override void OnEnter()
        {
            base.OnEnter();

            StartAimMode(4f, false);
            PlayPose(FloweryAnimations.SanFrandisco);

            // On the button press, not on the launch, so the shout leads the move.
            Sounds.PlayAt(Sounds.Utility, gameObject);

            if (characterMotor != null)
            {
                gravityDisabled = true;
                Gravity.Suspend(characterMotor);
                characterMotor.velocity = Vector3.zero;
            }

            if (FloweryAssets.JaronaChargeEffect != null)
            {
                FloweryEffects.Spawn(FloweryAssets.JaronaChargeEffect, new EffectData
                {
                    origin = transform.position + Vector3.up,
                    scale = 1.5f,
                    color = FloweryAssets.SoulBlue,
                }, false);
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            // Hold position for the whole channel, but keep turning to face the aim: the launch
            // direction is taken fresh when the dash begins, so the model has to track it or he
            // hangs there facing one way and then leaves in another.
            if (characterMotor != null) characterMotor.velocity = Vector3.zero;
            TrackFacing(GetAimRay().direction);

            if (isAuthority && fixedAge >= FloweryConfig.SanFranciscoChannelDuration.Value)
            {
                outer.SetNextState(new SanFranciscoDash());
            }
        }

        public override void OnExit()
        {
            RestoreGravity();
            base.OnExit();
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.PrioritySkill;

        /// <summary>
        /// Always hand gravity back, including when a stun or a death yanks us out of the state.
        /// The dash re-suspends it immediately, so the one-frame gap is not visible.
        /// </summary>
        private void RestoreGravity()
        {
            if (!gravityDisabled) return;
            Gravity.Restore(characterMotor);
            gravityDisabled = false;
        }
    }
}
