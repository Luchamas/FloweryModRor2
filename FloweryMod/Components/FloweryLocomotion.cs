using RoR2;
using UnityEngine;

namespace FloweryMod.Components
{
    /// <summary>
    /// Leans Flowery into his own movement.
    ///
    /// His animator has one state for everything that is not a skill - "Fly" - and until now
    /// that state was a single clip, so hanging still and crossing the map at a sprint looked
    /// exactly alike. It is now a blend tree between the hover and a run (see
    /// FloweryLPSetup.BuildFlyState and posetool's run section), and this component is the only
    /// thing that writes its blend parameter: how fast he is actually going, as a fraction of
    /// how fast he can go.
    ///
    /// Being a blend inside one state rather than a second state is what keeps this component
    /// out of everybody's way. It never plays anything, so it can never take a pose off a punch
    /// or a dash and never has to work out whether it currently owns one - a problem that is
    /// genuinely hard here, because Flowery's skills run on two entity state machines that do
    /// not know about each other (see BaseFloweryState.ReleasePose). All it does is change what
    /// Fly looks like, whenever Fly happens to be what is showing.
    ///
    /// It runs on every client and reads only the motor's velocity, which the game already
    /// replicates, so every client leans every Flowery correctly without a byte being sent. That
    /// is the difference between this and Jarona's coin flip, which had to be networked: this
    /// derives its answer from state everyone already has.
    /// </summary>
    public class FloweryLocomotion : MonoBehaviour
    {
        /// <summary>
        /// Speed, as a fraction of the walk speed stat, at which he starts to lean at all.
        ///
        /// Not zero: the motor keeps a few centimetres per second of drift on a body that is
        /// standing still - sliding down a shallow slope, settling after a landing, being pushed
        /// by a teammate - and leaning into that would have him list gently while idling.
        /// </summary>
        private const float LeanFloor = 0.15f;

        /// <summary>
        /// Speed, as a fraction of the walk speed stat, at which the lean is all the way over.
        ///
        /// This is the sprint multiplier the body prefab is built with (FloweryBody sets 1.45),
        /// read off the body rather than repeated, so full lean means full sprint however that
        /// number moves. Walking therefore leans him about two thirds of the way, which is the
        /// walk animation he does not otherwise have.
        /// </summary>
        private float LeanCeiling => Mathf.Max(LeanFloor + 0.1f,
                                               body != null ? body.sprintingSpeedMultiplier : 1.45f);

        /// <summary>
        /// How long the lean takes to catch up with a change of speed, in seconds.
        ///
        /// The parameter feeds a blend tree, which follows it exactly and instantly, so this is
        /// the only smoothing there is. Short enough to answer the stick, long enough that
        /// clipping a wall - which stops the motor dead for a frame or two - does not snap him
        /// upright and back.
        /// </summary>
        private const float LeanSmoothing = 0.14f;

        private CharacterBody body;
        private CharacterMotor motor;
        private ModelLocator modelLocator;

        private Animator animator;
        private int runParameter = -1;
        private float lean;
        private float leanVelocity;

        private void Awake()
        {
            body = GetComponent<CharacterBody>();
            motor = GetComponent<CharacterMotor>();
            modelLocator = GetComponent<ModelLocator>();
        }

        private void Update()
        {
            if (!ResolveAnimator()) return;

            lean = Mathf.SmoothDamp(lean, WantedLean(), ref leanVelocity,
                                    LeanSmoothing, Mathf.Infinity, Time.deltaTime);
            animator.SetFloat(runParameter, lean);
        }

        /// <summary>
        /// How far over he should be, 0 to 1.
        ///
        /// Horizontal speed only. OMEGA flies in three dimensions and a dash can throw him
        /// straight up; climbing is not running, and pitching him forward for it would lean him
        /// into a wall he is going over rather than through.
        /// </summary>
        private float WantedLean()
        {
            if (motor == null || body == null) return 0f;

            Vector3 velocity = motor.velocity;
            float speed = new Vector3(velocity.x, 0f, velocity.z).magnitude;
            float walk = Mathf.Max(0.01f, body.moveSpeed);

            return Mathf.Clamp01(Mathf.InverseLerp(LeanFloor, LeanCeiling, speed / walk));
        }

        /// <summary>
        /// Finds Flowery's own animator and the blend parameter on it, once.
        ///
        /// The model is instantiated at spawn rather than built into the prefab, so this cannot
        /// happen in Awake; and his Animator lives on his model, a child of mdlFlowery, not on
        /// the model object ModelLocator points at (the same reason BaseFloweryState has
        /// GetFloweryAnimator). A body wearing the Loader placeholder has no such animator and no
        /// such parameter, and simply never leans.
        /// </summary>
        private bool ResolveAnimator()
        {
            if (animator != null) return runParameter >= 0;

            Transform model = modelLocator != null ? modelLocator.modelTransform : null;
            if (model == null) return false;

            Transform custom = FloweryModelEnforcer.FindCustom(model);
            if (custom == null) return false;

            animator = custom.GetComponentInChildren<Animator>(true);
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                animator = null;
                return false;
            }

            for (int i = 0; i < animator.parameterCount; i++)
            {
                AnimatorControllerParameter p = animator.GetParameter(i);
                if (p.type != AnimatorControllerParameterType.Float ||
                    p.name != SkillStates.FloweryAnimations.RunParameter) continue;
                runParameter = p.nameHash;
                return true;
            }

            SkillStates.FloweryAnimations.ReportOnce(
                SkillStates.FloweryAnimations.RunParameter,
                "controller has no '" + SkillStates.FloweryAnimations.RunParameter +
                "' parameter - Flowery will hover at every speed instead of leaning into a run");
            enabled = false;
            return false;
        }
    }
}
