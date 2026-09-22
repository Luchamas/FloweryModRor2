using EntityStates;
using FloweryMod.Components;
using UnityEngine;

namespace FloweryMod.SkillStates
{
    /// <summary>
    /// Marks a state that drives the character motor itself. OMEGA flight checks for this and
    /// stops steering while one is running, otherwise it would overwrite the dash's velocity
    /// every frame and pin Jarona to a flat horizontal line.
    /// </summary>
    public interface IFloweryMotorState
    {
    }

    /// <summary>Shared plumbing for Flowery's skills: the TP meter, poses and the cape.</summary>
    public abstract class BaseFloweryState : BaseSkillState
    {
        protected TensionController tension;

        public override void OnEnter()
        {
            base.OnEnter();
            tension = GetComponent<TensionController>();
        }

        public override void OnExit()
        {
            ReleaseFacing();
            base.OnExit();
        }

        /// <summary>True while this state has taken the facing off CharacterDirection.</summary>
        private bool facingLocked;

        /// <summary>Whether CharacterDirection was enabled before we took it, to hand back.</summary>
        private bool directionWasEnabled;

        /// <summary>
        /// Takes the facing away from CharacterDirection, and reports the transform to rotate.
        ///
        /// A skill cannot ask nicely for the model's heading. CharacterDirection eases it round
        /// at the body's turn speed while the motor keeps writing its own heading from movement
        /// input, so asking merely loses slowly - whip the camera during a charge and the model
        /// is still facing the old way when the dash leaves. Winning that race by raising the
        /// turn speed only made the two writers alternate every frame, which read as flickering.
        /// So the component is switched off for the rest of the state and the model base is
        /// rotated here instead: with one writer there is nothing left to fight.
        /// </summary>
        private Transform TakeFacing(Vector3 direction, out Vector3 flat)
        {
            flat = Vector3.zero;
            if (characterDirection == null) return null;

            var wanted = new Vector3(direction.x, 0f, direction.z);
            if (wanted.sqrMagnitude < 0.001f) return null;
            flat = wanted.normalized;

            if (!facingLocked)
            {
                facingLocked = true;
                directionWasEnabled = characterDirection.enabled;
                characterDirection.enabled = false;
            }

            Transform target = characterDirection.targetTransform;
            if (target == null) target = GetModelBaseTransform();
            return target;
        }

        /// <summary>
        /// Points the model flat along <paramref name="direction"/> at once. For a dash, which
        /// has no time to spare and has already committed to where it is going.
        /// </summary>
        protected void LockFacing(Vector3 direction)
        {
            Vector3 flat;
            Transform target = TakeFacing(direction, out flat);
            if (target == null) return;

            target.rotation = Quaternion.LookRotation(flat);
            // Kept in step so handing control back does not spin him round again.
            characterDirection.forward = flat;
        }

        /// <summary>
        /// Eases the model toward <paramref name="direction"/> under our own control. For a
        /// wind-up, where he should visibly track the crosshair rather than snap onto it - but
        /// still as the only writer, so the tracking cannot flicker.
        /// </summary>
        protected void TrackFacing(Vector3 direction, float degreesPerSecond = 720f)
        {
            Vector3 flat;
            Transform target = TakeFacing(direction, out flat);
            if (target == null) return;

            target.rotation = Quaternion.RotateTowards(target.rotation,
                                                       Quaternion.LookRotation(flat),
                                                       degreesPerSecond * Time.fixedDeltaTime);
            characterDirection.forward = flat;
        }

        /// <summary>Hands the facing back to CharacterDirection. Safe to call unconditionally.</summary>
        protected void ReleaseFacing()
        {
            if (!facingLocked) return;
            if (characterDirection != null) characterDirection.enabled = directionWasEnabled;
            facingLocked = false;
        }

        /// <summary>
        /// Crossfades to one of Flowery's poses.
        ///
        /// His controller is a single layer of static poses rather than vanilla's stack of
        /// masked override layers, so there is no layer to grab and nothing to release - a
        /// state simply names the pose it wants and the next state names its own. Playing a
        /// state the animator does not have silently freezes the crossfade, so it is checked.
        ///
        /// The controller comes out of the asset bundle while this comes out of the DLL, and the
        /// two are deployed by different commands - dotnet build and the Flowery menu - so a pose
        /// added on this side can be a rebuild ahead of the bundle that has it.
        /// <paramref name="fallback"/> is the older pose to wear until the bundle catches up;
        /// without one, a missing pose leaves him in whatever he was already holding.
        /// </summary>
        protected void PlayPose(string state, float crossfade = 0.08f, string fallback = null)
        {
            Animator animator = GetFloweryAnimator();
            if (animator == null || string.IsNullOrEmpty(state)) return;

            int layer = animator.GetLayerIndex(FloweryAnimations.BaseLayer);
            if (layer < 0) layer = 0;

            if (!animator.HasState(layer, Animator.StringToHash(state)))
            {
                FloweryAnimations.ReportOnce(state, "animator has no pose '" + state + "'" +
                    (string.IsNullOrEmpty(fallback) ? "" : " - wearing '" + fallback + "' instead"));
                if (string.IsNullOrEmpty(fallback) || fallback == state) return;
                PlayPose(fallback, crossfade);
                return;
            }

            ownedPose = state;
            animator.CrossFadeInFixedTime(state, crossfade, layer);
            ApplyCapeFor(state);
        }

        /// <summary>
        /// The cape belongs to the pose, not to the skill.
        ///
        /// It used to be per-state bookkeeping - hide it in OnEnter, put it back in OnExit - and
        /// that cannot help flickering, because the two calls belong to different states with a
        /// gap between them. Deriving it from the pose instead means there is one rule, applied
        /// at the one place a pose is ever set, and no handshake to get out of step.
        /// </summary>
        private void ApplyCapeFor(string pose)
        {
            Transform model = GetModelTransform();
            if (model == null) return;

            var enforcer = model.GetComponent<Components.FloweryModelEnforcer>();
            if (enforcer != null) enforcer.WantCape(FloweryAnimations.WearsCape(pose));
        }

        /// <summary>The pose this state last asked for, so it can tell whether it still owns it.</summary>
        private string ownedPose;

        /// <summary>
        /// Back to the flight pose - but only if this state's own pose is still the one showing.
        ///
        /// Flowery's skills are split across two entity state machines: Flower Punches runs on
        /// "Weapon" while both dashes, both charges and OMEGA run on "Body". That is deliberate
        /// - it is what lets him dash out of a swing - but it means starting a charge does NOT
        /// end a swing. The two run side by side, and a swing that finished on its own would
        /// call PlayFly() straight into the middle of the charge, dropping him back to the idle
        /// pose for the rest of it.
        ///
        /// Vanilla survivors get away with this because their weapon animations live on a masked
        /// override layer, so the two machines never write the same bones. Flowery's controller
        /// is a single layer of whole-body poses, so the last writer wins - and a state that no
        /// longer owns the pose has to keep its hands off it.
        /// </summary>
        protected void ReleasePose()
        {
            if (string.IsNullOrEmpty(ownedPose)) return;      // never played one: nothing to give back

            Animator animator = GetFloweryAnimator();
            if (animator == null) return;

            int layer = animator.GetLayerIndex(FloweryAnimations.BaseLayer);
            if (layer < 0) layer = 0;

            // Mid-crossfade the destination is what matters, not the pose fading out of view:
            // a skill that took the pose a few frames ago is still the rightful owner.
            AnimatorStateInfo info = animator.IsInTransition(layer)
                ? animator.GetNextAnimatorStateInfo(layer)
                : animator.GetCurrentAnimatorStateInfo(layer);

            if (info.shortNameHash != Animator.StringToHash(ownedPose)) return;

            PlayFly();
        }

        /// <summary>
        /// Scales a pose that is a real animation rather than a held frame.
        ///
        /// Only the punches move: their state runs for PunchDuration / attackSpeedStat while
        /// the clip is authored at the base 0.6s, so a Flowery with attack speed items would
        /// have the swing cut off part-way through the extension - the exact frames the whole
        /// animation exists for. The generated controller drives both punch states off this
        /// parameter (see FloweryLPSetup.SpeedParam) and defaults it to 1, so an older bundle
        /// that has no such parameter simply plays at normal speed.
        /// </summary>
        protected void SetPoseSpeed(float speed)
        {
            Animator animator = GetFloweryAnimator();
            if (animator == null || animator.runtimeAnimatorController == null) return;

            for (int i = 0; i < animator.parameterCount; i++)
            {
                AnimatorControllerParameter p = animator.GetParameter(i);
                if (p.type != AnimatorControllerParameterType.Float ||
                    p.name != FloweryAnimations.SpeedParameter) continue;
                animator.SetFloat(FloweryAnimations.SpeedParameter, speed);
                return;
            }

            FloweryAnimations.ReportOnce(FloweryAnimations.SpeedParameter,
                "controller has no '" + FloweryAnimations.SpeedParameter +
                "' parameter - punch clips will not follow attack speed");
        }

        /// <summary>
        /// The animator that actually drives Flowery.
        ///
        /// ModelLocator.modelTransform is mdlFlowery, the holder that carries the hurtboxes,
        /// ragdoll and ChildLocator, and it deliberately has no Animator (see
        /// FloweryRig.StripDonor). Flowery's own Animator, with his own poses on it, lives on the
        /// model instance inside it, and that is the one to talk to.
        /// </summary>
        protected Animator GetFloweryAnimator()
        {
            if (cachedAnimator != null) return cachedAnimator;

            Transform model = GetModelTransform();
            if (model == null) return null;

            Transform own = Components.FloweryModelEnforcer.FindCustom(model);
            cachedAnimator = own != null ? own.GetComponentInChildren<Animator>(true)
                                         : GetModelAnimator();
            return cachedAnimator;
        }

        private Animator cachedAnimator;

        /// <summary>
        /// Back to the flight pose - his idle, walk and run are all the same pose.
        ///
        /// Prefer <see cref="ReleasePose"/> when ending a state: this takes the pose
        /// unconditionally, including out from under a skill running on the other machine.
        /// </summary>
        protected void PlayFly(float crossfade = 0.12f)
        {
            PlayPose(FloweryAnimations.Fly, crossfade);
        }

    }

    /// <summary>
    /// Flowery's animator state names, kept in one place.
    ///
    /// These are the poses exported from his own rig (see FloweryRig/posetool.py), not vanilla
    /// survivor animations. The controller built by "Flowery ▸ Set Up Low-Poly Flowery" is a
    /// single layer holding exactly these states.
    /// </summary>
    internal static class FloweryAnimations
    {
        /// <summary>The generated controller has one layer, named by Unity's default.</summary>
        internal const string BaseLayer = "Base Layer";

        /// <summary>OMEGA transformation: curls in, then throws an arm skyward.</summary>
        internal const string OmegaTransform = "OmegaTransform";

        /// <summary>Name of Flowery's model instance inside the model object (mdlFlowery).</summary>
        internal const string ModelObject = "FloweryCustomModel";

        /// <summary>
        /// The character-select mannequin's held pose: the author's CharSelectEnd, breathing.
        ///
        /// No longer the author's relaxed standing frame - that is where <see cref="SelectIntro"/>
        /// starts, and CharSelectEnd is where it lands. It loops, and it is the last thing that
        /// plays on the mannequin.
        /// </summary>
        internal const string Select = "Select";

        /// <summary>
        /// The character-select entrance: the standing pose into the flourish, played once.
        ///
        /// The mannequin's animator is started exactly once, by FloweryModelEnforcer, at the
        /// moment the select screen instantiates the display prefab - so this begins when
        /// Flowery is picked. The controller hands it over to <see cref="Select"/> on exit
        /// time; nothing here has to notice it finished.
        ///
        /// A bundle built before this state existed does not have it, and Animator.Play on a
        /// state that is not there does nothing at all, silently, leaving the mannequin in the
        /// controller's default state - which is Fly, i.e. a survivor hovering in his own
        /// select screen. FloweryModelEnforcer checks for the state and falls back to
        /// <see cref="Select"/>.
        /// </summary>
        internal const string SelectIntro = "SelectIntro";

        /// <summary>
        /// The main pose: idle, walking and running all use it.
        ///
        /// One state, but no longer one clip - it is a blend between hanging still and running
        /// flat out, and how far along that blend he is comes from <see cref="RunParameter"/>.
        /// Skills take it and give it back exactly as they always did; only what it looks like
        /// while they are not holding it has changed.
        /// </summary>
        internal const string Fly = "Fly";

        /// <summary>Jarona, the secondary dash: a head-first lunge. Cape hidden.</summary>
        internal const string Jarona = "Jarona";

        /// <summary>
        /// Jarona's second pose: an upright shrug, turned once on the spot across the dash. The
        /// three are rolled between per dash - see JaronaDash. The turn is baked into the clip, not
        /// driven here, so it composes with the facing the dash locks instead of fighting it.
        /// Cape hidden.
        /// </summary>
        internal const string Jarona2 = "Jarona2";

        /// <summary>Jarona's third pose: a flying kick, held. Cape hidden.</summary>
        internal const string Jarona3 = "Jarona3";

        /// <summary>Coiled wind-up, held while charging HERE I COME SAN FRANDISCO.</summary>
        internal const string SanFrandisco = "SanFrandisco";

        /// <summary>LAST JARONA's wind-up: fists drawn in, hanging, before the dive.</summary>
        internal const string ChargeLastJarona = "ChargeLastJarona";

        /// <summary>OMEGA finisher: head-first dive, arms pinned. Cape hidden.</summary>
        internal const string LastJarona = "LastJarona";

        internal const string PunchRight = "PunchR";
        internal const string PunchLeft = "PunchL";

        /// <summary>
        /// Taunt 1, the hair flip: hand on his forehead, dragged back over his crown, palm out,
        /// and the fringe falling back down. A one-shot that ends at rest, which the taunt then
        /// holds - see posetool's Taunt 1 section. Cape hidden: the flip swings the hand it hangs from.
        /// </summary>
        internal const string Taunt1 = "Taunt1";

        /// <summary>Taunt 3's first half: the author's JaronaCharge wind-up, held. Cape hidden.</summary>
        internal const string JaronaCharge = "JaronaCharge";

        /// <summary>
        /// Taunt 3's second half: the Jarona 2 shrug held still, where <see cref="Jarona2"/>
        /// spins him. Taunt 2 has no state of its own - it is <see cref="SanFrandisco"/>. Cape hidden.
        /// </summary>
        internal const string Taunt3 = "Taunt3";

        /// <summary>Float parameter the punch states take their playback rate from.</summary>
        internal const string SpeedParameter = "PunchSpeed";

        /// <summary>
        /// Float parameter leaning <see cref="Fly"/> into a run: 0 hangs still, 1 is flat out.
        /// Written by FloweryLocomotion, and by nothing else.
        /// </summary>
        internal const string RunParameter = "Run";

        /// <summary>Name prefix of the cape renderer inside the model.</summary>
        internal const string CapeMesh = "mesh_coat";

        /// <summary>
        /// Whether Flowery wears his cape in this pose.
        ///
        /// He wears it only when he is hanging still: the flight pose he idles, walks and runs
        /// in, the San Frandisco channel, and the relaxed standing pose on the select screen.
        /// Everything else is a swing or a dash, where it is off.
        ///
        /// The list is not a style choice, it is what the .blend can actually draw. The cape is
        /// rigged under `coatholder`, which hangs off `hand.R`, so it only hangs correctly in a
        /// pose that keys the cape chain to counter whatever that hand is doing - and the author
        /// posed the cape in exactly three actions: Fly, SanFrandisco and Select. The
        /// select-screen flourish moves his LEFT arm only and never the hand the cape hangs from;
        /// its end pose, CharSelectEnd, does not key the cape and borrows Select's at export
        /// (posetool.FILL_FROM). That is why SelectIntro can be here. In the other
        /// five the cape bones are unkeyed, sit at rest, and get dragged around by the right
        /// hand into a rigid slab.
        ///
        /// OMEGA's transformation is therefore NOT in this list even though it should be: it
        /// throws an arm skyward and takes the unposed cape with it. Add it here the moment
        /// TransformOmega keys the cape chain in the .blend.
        ///
        /// Anything not named here hides it, so a pose added later is off until it says
        /// otherwise, which is the safe way round.
        /// </summary>
        internal static bool WearsCape(string pose)
        {
            return pose == Fly || pose == SanFrandisco || pose == Select || pose == SelectIntro;
        }

        private static readonly System.Collections.Generic.HashSet<string> reported =
            new System.Collections.Generic.HashSet<string>();

        /// <summary>Logs a rig complaint once per key, so a missing pose is not spammed.</summary>
        internal static void ReportOnce(string key, string message)
        {
            if (!reported.Add(key)) return;
            Modules.Log.Warning("Flowery animator: " + message);
        }
    }
}
