using BepInEx.Configuration;
using UnityEngine;

namespace FloweryMod.Modules
{
    /// <summary>
    /// Every number a player might reasonably want to tweak lives here so balance passes
    /// do not require a recompile.
    /// </summary>
    internal static class FloweryConfig
    {
        internal static ConfigEntry<float> ModelScale;
        internal static ConfigEntry<string> ModelOffsetText;
        internal static ConfigEntry<string> ModelRotationText;
        internal static ConfigEntry<float> DisplayModelScale;
        internal static ConfigEntry<bool> ModelRagdoll;
        internal static ConfigEntry<bool> ShowItemDisplays;

        internal static ConfigEntry<float> BaseHealth;
        internal static ConfigEntry<float> BaseDamage;
        internal static ConfigEntry<float> BaseMoveSpeed;
        internal static ConfigEntry<float> BaseArmor;

        internal static ConfigEntry<float> TensionMax;
        internal static ConfigEntry<float> TensionGrazeRadius;
        internal static ConfigEntry<float> TensionGrazePerSecond;
        internal static ConfigEntry<float> TensionPerHit;

        internal static ConfigEntry<float> PunchDamage;
        internal static ConfigEntry<float> PunchDuration;
        internal static ConfigEntry<float> PunchRange;
        internal static ConfigEntry<float> PunchHealFraction;

        internal static ConfigEntry<float> TpBarLeftMargin;
        internal static ConfigEntry<float> TpBarHeight;

        internal static ConfigEntry<float> JaronaDamage;
        internal static ConfigEntry<float> JaronaCooldown;
        internal static ConfigEntry<int> JaronaCharges;

        internal static ConfigEntry<float> SanFranciscoDamage;
        internal static ConfigEntry<float> SanFranciscoCooldown;
        internal static ConfigEntry<float> SanFranciscoChannelDuration;

        internal static ConfigEntry<float> SoundVolume;

        internal static ConfigEntry<float> OmegaMinTension;
        internal static ConfigEntry<float> OmegaDrainPerSecond;
        internal static ConfigEntry<float> OmegaBlastDamage;
        internal static ConfigEntry<float> OmegaCooldown;

        internal static ConfigEntry<float> OmegaFlySpeedMultiplier;
        internal static ConfigEntry<float> OmegaRiseSpeed;
        internal static ConfigEntry<float> OmegaChargeDuration;

        internal static ConfigEntry<float> LastJaronaImpactDamage;
        internal static ConfigEntry<float> LastJaronaBlastDamage;
        internal static ConfigEntry<float> LastJaronaCooldown;
        internal static ConfigEntry<float> LastJaronaChargeDuration;

        /// <summary>Parsed from the config string, so a bad value degrades to zero rather than throwing.</summary>
        internal static Vector3 ModelOffset => ParseVector(ModelOffsetText);

        internal static Vector3 ModelRotation => ParseVector(ModelRotationText);

        private static Vector3 ParseVector(ConfigEntry<string> entry)
        {
            if (entry == null) return Vector3.zero;

            string[] parts = entry.Value.Split(',');
            if (parts.Length != 3) return Vector3.zero;

            float x, y, z;
            if (!float.TryParse(parts[0].Trim(), System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture, out x)) return Vector3.zero;
            if (!float.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture, out y)) return Vector3.zero;
            if (!float.TryParse(parts[2].Trim(), System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture, out z)) return Vector3.zero;

            return new Vector3(x, y, z);
        }

        internal static void Init(ConfigFile config)
        {
            // An imported model almost never lands at the right size or facing on the first try,
            // so these are config rather than constants - dial them in without a rebuild.
            ModelScale = config.Bind("00 - General", "Model Scale", 0.2f,
                "Uniform scale applied to a custom model from the asset bundle.");
            ModelOffsetText = config.Bind("00 - General", "Model Offset", "0, 0, 0",
                "Local position of a custom model, as \"x, y, z\". Raise y if it sinks into the floor.");
            ModelRotationText = config.Bind("00 - General", "Model Rotation", "0, 0, 0",
                "Local euler rotation of a custom model, as \"x, y, z\". Use y = 180 if it faces backwards.");

            DisplayModelScale = config.Bind("00 - General", "Character Select Model Scale", 0.8f,
                "Extra scale for the character-select mannequin only, multiplied on top of Model " +
                "Scale. The select panel is framed for Loader's proportions, so Flowery needs to " +
                "come down a little to sit in it properly. In-game size is unaffected.");

            ModelRagdoll = config.Bind("00 - General", "Model Ragdoll", true,
                "Build a real ragdoll on Flowery's own skeleton, so his corpse goes limp. Set " +
                "false and his corpse simply holds its last pose - there is no borrowed corpse " +
                "to fall back on any more.");

            ShowItemDisplays = config.Bind("00 - General", "Show Item Displays", false,
                "Draw picked-up items on Flowery's body. Off by default: the only placements " +
                "available are Loader's, positioned against Loader's skeleton and animated by " +
                "Loader's clips, so items sit wrong on Flowery and drift against his pose. Turn " +
                "this back on once he has item display rules of his own. Cosmetic only - items " +
                "always work either way.");

            BaseHealth = config.Bind("01 - Stats", "Base Health", 110f, "Health at level 1.");
            BaseDamage = config.Bind("01 - Stats", "Base Damage", 12f, "Damage at level 1.");
            BaseMoveSpeed = config.Bind("01 - Stats", "Base Move Speed", 7f, "Movement speed in m/s.");
            BaseArmor = config.Bind("01 - Stats", "Base Armor", 0f, "Armor at level 1.");

            SoundVolume = config.Bind("00 - General", "Voice Clip Volume", 0.5f,
                "Volume of Flowery's voice clips, 0 to 1. Set to 0 to mute them. Applied on top " +
                "of the game's Master and SFX volume sliders.");

            TpBarLeftMargin = config.Bind("00 - General", "TP Bar Left Margin", 50f,
                "Distance from the left edge of the screen to the TP bar, in HUD units.");
            TpBarHeight = config.Bind("00 - General", "TP Bar Height", 338f,
                "On-screen height of the TP bar, in HUD units. The width follows the artwork's " +
                "aspect ratio, and the bar stays centred vertically, so shrinking it pulls the " +
                "top and bottom in equally.");

            TensionMax = config.Bind("02 - Tension (TP)", "Max TP", 100f, "Maximum stored Tension Points.");
            TensionGrazeRadius = config.Bind("02 - Tension (TP)", "Graze Radius", 14f,
                "Enemies within this radius feed TP, mirroring DELTARUNE's grazing.");
            TensionGrazePerSecond = config.Bind("02 - Tension (TP)", "Graze TP per Second", 4f,
                "TP per second while at least one enemy is inside the graze radius.");
            TensionPerHit = config.Bind("02 - Tension (TP)", "TP per Hit", 1.2f, "TP granted per enemy hit.");

            PunchDamage = config.Bind("03 - Primary: Flower Punches", "Damage", 2.3f,
                "Damage coefficient per punch. 2.3 means 230% of base damage.");
            PunchDuration = config.Bind("03 - Primary: Flower Punches", "Punch Duration", 0.6f,
                "Seconds per punch before attack speed is applied. Higher is slower and weightier.");
            PunchRange = config.Bind("03 - Primary: Flower Punches", "Reach", 4.5f,
                "How far in front of Flowery the punch reaches, in meters.");
            PunchHealFraction = config.Bind("03 - Primary: Flower Punches", "Heal per Punch", 0.01f,
                "Fraction of maximum health restored by a punch that connects. 0.01 is 1%. " +
                "Once per swing, not once per enemy hit. Set to 0 to disable.");

            JaronaDamage = config.Bind("04 - Secondary: Jarona", "Damage", 2f,
                "Damage coefficient dealt to each enemy passed through.");
            JaronaCooldown = config.Bind("04 - Secondary: Jarona", "Cooldown", 2f,
                "Seconds to recharge one use.");
            JaronaCharges = config.Bind("04 - Secondary: Jarona", "Charges", 2,
                "How many dashes can be banked at once.");

            SanFranciscoDamage = config.Bind("05 - Utility: Here I Come San Frandisco", "Damage", 3f,
                "Damage coefficient dealt to each enemy passed through.");
            SanFranciscoCooldown = config.Bind("05 - Utility: Here I Come San Frandisco", "Cooldown", 6f,
                "Cooldown in seconds.");
            SanFranciscoChannelDuration = config.Bind("05 - Utility: Here I Come San Frandisco",
                "Channel Duration", 0.8f,
                "Seconds Flowery hangs in place before launching. Fixed, not hold-to-charge.");

            OmegaMinTension = config.Bind("06 - Special: OMEGA FLOWERY", "Minimum TP", 75f,
                "TP required to go OMEGA.");
            OmegaDrainPerSecond = config.Bind("06 - Special: OMEGA FLOWERY", "TP Drain per Second", 5f,
                "How fast the TP meter empties while OMEGA is active. OMEGA ends when it hits zero, " +
                "so 75 TP at 5/s lasts 15 seconds and a full bar lasts 20.");
            OmegaBlastDamage = config.Bind("06 - Special: OMEGA FLOWERY", "Eruption Damage", 4f,
                "Base damage coefficient of the vine eruption, before the TP bonus.");
            OmegaCooldown = config.Bind("06 - Special: OMEGA FLOWERY", "Cooldown", 10f, "Cooldown in seconds.");
            OmegaFlySpeedMultiplier = config.Bind("06 - Special: OMEGA FLOWERY", "Flight Speed Multiplier",
                1.6f, "Flight speed as a multiple of ground move speed.");
            OmegaRiseSpeed = config.Bind("06 - Special: OMEGA FLOWERY", "Transform Rise Speed", 13f,
                "How fast Flowery lifts off the ground while transforming.");
            OmegaChargeDuration = config.Bind("06 - Special: OMEGA FLOWERY", "Transform Charge", 0.5f,
                "Seconds spent rising and invulnerable before the eruption goes off.");

            LastJaronaImpactDamage = config.Bind("07 - OMEGA Special: LAST JARONA", "Impact Damage", 6f,
                "Damage coefficient dealt on contact, before the explosion.");
            LastJaronaBlastDamage = config.Bind("07 - OMEGA Special: LAST JARONA", "Explosion Damage", 12f,
                "Damage coefficient of the explosion.");
            LastJaronaCooldown = config.Bind("07 - OMEGA Special: LAST JARONA", "Cooldown", 3f,
                "Cooldown in seconds. Only usable while OMEGA.");
            LastJaronaChargeDuration = config.Bind("07 - OMEGA Special: LAST JARONA", "Charge", 0.8f,
                "Seconds spent winding up before the punch, so it lands on \"...Jarona!\".");
        }
    }
}
