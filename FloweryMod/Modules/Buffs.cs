using R2API;
using RoR2;
using UnityEngine;

namespace FloweryMod.Modules
{
    /// <summary>Custom buffs and the stat hook that gives them meaning.</summary>
    internal static class Buffs
    {
        /// <summary>
        /// Applied while Flowery is in his OMEGA form. The flight and the LAST JARONA skill
        /// override both key off this buff - see <see cref="Components.OmegaFormController"/>.
        /// </summary>
        internal static BuffDef Omega;

        internal const float OmegaDamageMultiplier = 0.25f;

        internal static void Init()
        {
            Omega = ScriptableObject.CreateInstance<BuffDef>();
            Omega.name = "bdFloweryOmega";
            Omega.buffColor = FloweryAssets.Gold;
            Omega.canStack = false;
            Omega.isDebuff = false;
            Omega.isHidden = false;
            Omega.iconSprite = FloweryAssets.IconOmegaBuff;
            ContentAddition.AddBuffDef(Omega);

            RecalculateStatsAPI.GetStatCoefficients += ApplyOmegaStats;
        }

        private static void ApplyOmegaStats(CharacterBody body, RecalculateStatsAPI.StatHookEventArgs args)
        {
            if (body == null || Omega == null) return;
            if (!body.HasBuff(Omega)) return;

            args.damageMultAdd += OmegaDamageMultiplier;
        }
    }
}
