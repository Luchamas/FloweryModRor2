using FloweryMod.Content;
using FloweryMod.Modules;
using RoR2;
using UnityEngine;

namespace FloweryMod.Components
{
    /// <summary>
    /// Everything OMEGA FLOWERY does after the eruption: free flight for as long as the buff
    /// lasts, and swapping the special slot over to LAST JARONA.
    ///
    /// Runs on every client. Flight only steers on the authority - remote clients get the
    /// resulting movement through the normal motor sync - while the skill override is local
    /// state that each client applies for itself off the (networked) buff.
    /// </summary>
    public class OmegaFormController : MonoBehaviour
    {
        private CharacterBody body;
        private CharacterMotor motor;
        private InputBankTest inputBank;
        private SkillLocator skillLocator;
        private EntityStateMachine bodyStateMachine;

        private bool omegaActive;
        private bool skillOverridden;

        private void Awake()
        {
            body = GetComponent<CharacterBody>();
            motor = GetComponent<CharacterMotor>();
            inputBank = GetComponent<InputBankTest>();
            skillLocator = GetComponent<SkillLocator>();
            bodyStateMachine = EntityStateMachine.FindByCustomName(gameObject, "Body");
        }

        private void OnDisable() => Deactivate();

        private void OnDestroy() => Deactivate();

        private void FixedUpdate()
        {
            if (body == null) return;

            bool shouldBeActive = Buffs.Omega != null && body.HasBuff(Buffs.Omega);
            if (shouldBeActive != omegaActive)
            {
                if (shouldBeActive) Activate();
                else Deactivate();
            }

            if (omegaActive) Fly();
        }

        private void Activate()
        {
            SetRainbow(true);
            omegaActive = true;

            if (motor != null)
            {
                Gravity.Suspend(motor);
                // Leave the ground immediately, otherwise the character controller keeps him
                // snapped to it and the first moments of flight feel stuck.
                if (motor.Motor != null) motor.Motor.ForceUnground();
            }

            ApplySkillOverride();
        }

        private void Deactivate()
        {
            SetRainbow(false);
            if (omegaActive) Gravity.Restore(motor);
            omegaActive = false;

            RemoveSkillOverride();
        }

        /// <summary>
        /// OMEGA colours him. The component tints instanced materials rather than swapping
        /// shaders, so RoR2's elite overlays and cloaking keep working, and it restores the
        /// originals on disable - an interrupted transformation cannot strand him rainbow.
        /// </summary>
        private void SetRainbow(bool on)
        {
            var locator = GetComponent<RoR2.ModelLocator>();
            Transform model = locator != null ? locator.modelTransform : null;
            if (model == null) return;

            var rainbow = model.GetComponent<OmegaRainbow>();
            if (rainbow == null)
            {
                if (!on) return;
                rainbow = model.gameObject.AddComponent<OmegaRainbow>();
            }
            rainbow.enabled = on;
        }

        /// <summary>
        /// Free flight in the direction you are looking.
        ///
        /// Forward input flies along the aim ray, so looking up and pushing forward climbs;
        /// strafing stays horizontal so you can circle a target without drifting vertically.
        /// With no input he simply hangs there - the flight is unconditional for the whole form,
        /// with no sinking to fight against.
        /// </summary>
        private void Fly()
        {
            if (motor == null || body == null || !body.hasEffectiveAuthority) return;

            // A dash owns the motor while it runs; steering here too would flatten its arc.
            if (IsMotorStateRunning()) return;

            float speed = body.moveSpeed * FloweryConfig.OmegaFlySpeedMultiplier.Value;
            Vector3 velocity = Vector3.zero;

            if (inputBank != null)
            {
                Vector3 aim = inputBank.aimDirection.normalized;

                // moveVector is already world-space and camera-relative, so its components along
                // the camera's flat forward and right axes recover the raw stick/WASD input.
                Vector3 flatAim = new Vector3(aim.x, 0f, aim.z);
                if (flatAim.sqrMagnitude < 0.0001f) flatAim = transform.forward;
                flatAim.Normalize();

                Vector3 right = Vector3.Cross(Vector3.up, flatAim).normalized;
                Vector3 move = inputBank.moveVector;

                float forwardInput = Vector3.Dot(move, flatAim);
                float strafeInput = Vector3.Dot(move, right);

                velocity = aim * forwardInput * speed + right * strafeInput * speed;
            }

            // Zero the motor's own input contribution as well as setting velocity. The main
            // character state runs in the same FixedUpdate and Unity does not guarantee which
            // component goes first; with moveDirection cleared, only this velocity survives
            // either way.
            motor.moveDirection = Vector3.zero;
            motor.velocity = velocity;
        }

        private bool IsMotorStateRunning()
        {
            if (bodyStateMachine == null) return false;
            return bodyStateMachine.state is SkillStates.IFloweryMotorState;
        }

        private void ApplySkillOverride()
        {
            if (skillOverridden) return;

            GenericSkill special = skillLocator != null ? skillLocator.special : null;
            if (special == null || FlowerySkills.LastJaronaSkill == null) return;

            special.SetSkillOverride(this, FlowerySkills.LastJaronaSkill,
                                     GenericSkill.SkillOverridePriority.Contextual);
            skillOverridden = true;
        }

        private void RemoveSkillOverride()
        {
            if (!skillOverridden) return;

            GenericSkill special = skillLocator != null ? skillLocator.special : null;
            if (special != null && FlowerySkills.LastJaronaSkill != null)
            {
                special.UnsetSkillOverride(this, FlowerySkills.LastJaronaSkill,
                                           GenericSkill.SkillOverridePriority.Contextual);
            }

            skillOverridden = false;
        }
    }
}
