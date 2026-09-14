using FloweryMod.Modules;
using UnityEngine;
using UnityEngine.Networking;

namespace FloweryMod.SkillStates
{
    /// <summary>
    /// Jarona - the secondary. A short instant dash on two charges, for repositioning between
    /// punches rather than crossing the arena.
    ///
    /// It has three poses and rolls between them every time, so the skill you press most often is
    /// not the same fifteen metres twice in a row: a head-first lunge, an upright shrug that
    /// spins him once on the spot on his way through, or a flying kick. All are the author's, out
    /// of the .blend (see FloweryRig/posetool.py); the spin is baked into the second clip rather
    /// than driven from here, so it turns the model under a facing this state has already locked
    /// instead of racing it.
    /// </summary>
    public class JaronaDash : BaseFloweryDash
    {
        public static float speed = 34f;

        // Distance is speed x duration, so this is the dial to turn for reach. Held at the same
        // velocity as before, twice as long: roughly 15m, against San Frandisco's 26m.
        //
        // A_Jarona2's turn is timed to land exactly as this runs out, so moving it means moving
        // posetool.JARONA_FRAMES with it or the spin finishes crooked. How fast that turn goes
        // is posetool.JARONA_TURNS, not anything here.
        public static float duration = 0.44f;

        public static float tensionPerEnemy = 5f;

        /// <summary>The poses a dash can come up as, rolled between evenly.</summary>
        private static readonly string[] Poses =
        {
            FloweryAnimations.Jarona,
            FloweryAnimations.Jarona2,
            FloweryAnimations.Jarona3,
        };

        /// <summary>
        /// Which of <see cref="Poses"/> this dash came up as.
        ///
        /// Rolled on the authority and networked rather than rolled per machine: every client
        /// renders this model, so a local roll would have Flowery lunging on one screen and
        /// kicking on another for the same dash. (The voice clips get away with a local roll
        /// because only the owner ever hears them - see Sounds.PlayAt.)
        /// </summary>
        private byte pose;

        protected override float DashSpeed => speed;
        protected override float DashDuration => duration;
        protected override float DamageCoefficient => FloweryConfig.JaronaDamage.Value;
        protected override float TensionPerEnemy => tensionPerEnemy;
        protected override string VoiceCategory => Sounds.Secondary;

        // Clamped rather than trusted: the index arrives off the wire.
        protected override string DashPose => Poses[Mathf.Min(pose, Poses.Length - 1)];

        public override void OnEnter()
        {
            // Ahead of base.OnEnter, which is the call that reads DashPose and plays it.
            if (isAuthority) pose = (byte)Random.Range(0, Poses.Length);
            base.OnEnter();
        }

        public override void OnSerialize(NetworkWriter writer)
        {
            base.OnSerialize(writer);
            writer.Write(pose);
        }

        public override void OnDeserialize(NetworkReader reader)
        {
            base.OnDeserialize(reader);
            pose = reader.ReadByte();
        }
    }
}
