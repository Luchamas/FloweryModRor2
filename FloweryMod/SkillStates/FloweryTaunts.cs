using EntityStates;
using FloweryMod.Modules;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace FloweryMod.SkillStates
{
    /// <summary>
    /// A taunt: a pose and a voice line, started by a key rather than a skill (see
    /// Components.FloweryTauntInput) and let go the moment the player does anything else.
    ///
    /// Taunts run on the "Body" machine, in place of GenericCharacterMain. That is what keeps him
    /// standing still for one - nothing is turning movement input into motion - but it also means
    /// nothing is turning skill input into skills, because GenericCharacterMain is what does that
    /// too, for every slot. So the taunt watches the inputs itself and steps aside on any of them.
    /// Stepping aside is enough: a press only counts as used once a skill claims it, and the
    /// claim survives for as long as the button stays down, so the Jarona that cancels a taunt
    /// still goes off on the next tick instead of needing a second press.
    /// </summary>
    public abstract class BaseFloweryTaunt : BaseFloweryState
    {
        /// <summary>
        /// The least the taunt lasts, in seconds, whatever its voice line does: its own animation
        /// has to finish. Past that it lasts exactly as long as the line is heard - a pose held on
        /// after the last word is a pose held too long.
        /// </summary>
        protected abstract float MinimumDuration { get; }

        /// <summary>How long to hold when there is no voice line to time it by - no Sounds folder.</summary>
        protected virtual float SilentDuration => MinimumDuration;

        /// <summary>Voice clip folder to speak on the way in.</summary>
        protected abstract string VoiceCategory { get; }

        /// <summary>Which way he faced when the key went down, flat, held for the whole taunt.</summary>
        protected Vector3 facing;

        /// <summary>
        /// Which line of <see cref="VoiceCategory"/> this taunt says, or -1 for none.
        ///
        /// Picked on the owner's machine and networked, because the taunt's length comes from it:
        /// the owner decides when the taunt ends, so a line picked per client would have everyone
        /// else hearing a different one cut short or trailing on. See Sounds.PickIndex.
        /// </summary>
        private int voice = -1;

        /// <summary>
        /// The line as it plays on this machine. It belongs to the taunt: walk out of the taunt
        /// and he stops talking, rather than finishing the sentence over whatever comes next.
        /// </summary>
        private Sounds.Voice spoken;

        private float duration;

        public override void OnEnter()
        {
            // Ahead of everything that reads it. A client that did not start the taunt has
            // already had it filled in by OnDeserialize.
            if (isAuthority) voice = Sounds.PickIndex(VoiceCategory);

            base.OnEnter();

            // Held for the whole taunt, including in front of the camera: the one thing a player
            // does during a taunt is swing the camera round to watch it, and an aim timer left
            // over from a punch would turn him to follow.
            facing = characterDirection != null ? characterDirection.forward : transform.forward;
            facing.y = 0f;
            if (facing.sqrMagnitude < 0.001f) facing = transform.forward;
            facing.Normalize();
            LockFacing(facing);

            float heard = Sounds.AudibleLength(VoiceCategory, voice);
            duration = Mathf.Max(MinimumDuration, heard > 0f ? heard : SilentDuration);

            spoken = Sounds.PlayAt(VoiceCategory, voice, gameObject);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (!isAuthority) return;
            if (fixedAge >= duration || PlayerIsActing(inputBank)) outer.SetNextStateToMain();
        }

        public override void OnSerialize(NetworkWriter writer)
        {
            base.OnSerialize(writer);
            writer.Write((byte)(voice < 0 ? byte.MaxValue : voice));
        }

        public override void OnDeserialize(NetworkReader reader)
        {
            base.OnDeserialize(reader);
            byte b = reader.ReadByte();
            voice = b == byte.MaxValue ? -1 : b;
        }

        public override void OnExit()
        {
            // Every way out, not only a cancel. A taunt that runs its course ends where the line
            // goes quiet (see duration), so all this can cut from a finished one is silence -
            // and every client runs it, since the end of the state is networked like its start.
            Sounds.Stop(spoken);
            spoken = null;

            ReleasePose();
            base.OnExit();
        }

        /// <summary>Anything may cut a taunt short - a skill, a stun, a freeze.</summary>
        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Any;

        /// <summary>
        /// Whether the player is asking for something a taunt would be in the way of: moving,
        /// jumping, or any of the four skills.
        ///
        /// Also asked before a taunt starts, not only while one runs. Starting one mid-stride
        /// would end it on the very next tick - a voice line cut off with nothing to show for it.
        /// </summary>
        internal static bool PlayerIsActing(InputBankTest input)
        {
            if (input == null) return false;
            return input.moveVector.sqrMagnitude > 0.01f
                || input.jump.down
                || input.skill1.down || input.skill2.down
                || input.skill3.down || input.skill4.down;
        }
    }

    /// <summary>
    /// Taunt 1, Ctrl+1: Flowery throws his hair back and offers you his palm, and the fringe
    /// falls back down. The flip always plays out in full; a longer line holds the end of it.
    /// </summary>
    public class HairFlipTaunt : BaseFloweryTaunt
    {
        /// <summary>
        /// The clip's length: posetool.TAUNT1_SECONDS, which the export prints. The taunt may not
        /// end before the fringe has landed, or he walks off with it still in the air.
        /// </summary>
        public static float flipLength = 1.05f;

        /// <summary>Held that long past the landing, so it reads before the crossfade home.</summary>
        public static float landedHold = 0.1f;

        protected override float MinimumDuration => flipLength + landedHold;
        protected override string VoiceCategory => Sounds.TauntHairFlip;

        public override void OnEnter()
        {
            base.OnEnter();

            // Select is the select screen's hand-on-hair pose, which every bundle since the
            // character-select flourish has carried - the nearest thing to a hair flip that an
            // older bundle can show.
            PlayPose(FloweryAnimations.Taunt1, 0.15f, FloweryAnimations.Select);
        }
    }

    /// <summary>
    /// Taunt 2, Ctrl+2: the HERE I COME SAN FRANDISCO pose, held exactly as long as the line.
    /// The same pose the utility charges in, so the cape stays on for it (FloweryAnimations.WearsCape).
    /// </summary>
    public class FrandiscoTaunt : BaseFloweryTaunt
    {
        // Nothing to finish - it is a single pose - so only a floor under a very short line.
        protected override float MinimumDuration => 0.5f;
        protected override float SilentDuration => 1.5f;
        protected override string VoiceCategory => Sounds.TauntFrandisco;

        public override void OnEnter()
        {
            base.OnEnter();
            PlayPose(FloweryAnimations.SanFrandisco, 0.12f);
        }
    }

    /// <summary>
    /// Taunt 3, Ctrl+3: the JaronaCharge wind-up for half a second, then the Jarona 2 shrug, and
    /// a short drift backwards while he keeps facing the way he was - he does not turn his back on
    /// you to leave. The shrug holds until the line ends.
    ///
    /// It drives the motor for the whole taunt, which is what <see cref="IFloweryMotorState"/>
    /// says: during OMEGA the flight steering would otherwise overwrite the drift every frame.
    /// Gravity is off for the same stretch, so he hangs in the wind-up wherever he started it
    /// and the drift is the same flat line on the ground, in the air and in flight - the way
    /// San Frandisco's channel hangs him.
    /// </summary>
    public class FeintTaunt : BaseFloweryTaunt, IFloweryMotorState
    {
        /// <summary>
        /// How long the wind-up is held before the shrug. The voice line's first half ends
        /// here and its second starts a tenth of a second later, so the pose changes in the gap.
        /// </summary>
        public static float chargeDuration = 0.5f;

        // The drift: fast off the mark and easing to a stop, (1 - t)^2 over its length. Distance
        // is speed x duration / 3, so this is about three metres - a step back, not a dash.
        public static float driftSpeed = 22f;
        public static float driftDuration = 0.4f;

        private bool shrugged;
        private bool gravityDisabled;

        // The wind-up and the drift both have to happen, and the shrug has to be seen landing.
        protected override float MinimumDuration => chargeDuration + driftDuration + 0.1f;
        protected override string VoiceCategory => Sounds.TauntFeint;

        public override void OnEnter()
        {
            base.OnEnter();

            // ChargeLastJarona is LAST JARONA's own wind-up, in every bundle since OMEGA, and the
            // same idea: fists drawn before a Jarona that is coming.
            PlayPose(FloweryAnimations.JaronaCharge, 0.1f, FloweryAnimations.ChargeLastJarona);

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

            if (!shrugged && fixedAge >= chargeDuration)
            {
                shrugged = true;
                // The spun Jarona2 is the fallback for a bundle without the held one: it turns
                // him once and then holds the same shrug, so an old bundle costs a spin, not a pose.
                PlayPose(FloweryAnimations.Taunt3, 0.06f, FloweryAnimations.Jarona2);
            }

            if (characterMotor != null) characterMotor.velocity = -facing * DriftSpeedAt(fixedAge - chargeDuration);
        }

        public override void OnExit()
        {
            if (gravityDisabled)
            {
                Gravity.Restore(characterMotor);
                gravityDisabled = false;
            }
            base.OnExit();
        }

        /// <summary>Backward speed <paramref name="t"/> seconds into the shrug; zero before and after.</summary>
        private static float DriftSpeedAt(float t)
        {
            if (t < 0f || t >= driftDuration) return 0f;
            float left = 1f - t / driftDuration;
            return driftSpeed * left * left;
        }
    }
}
