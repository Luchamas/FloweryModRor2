using R2API;

namespace FloweryMod.Modules
{
    /// <summary>Every language token the survivor uses.</summary>
    internal static class Tokens
    {
        internal const string Prefix = "FLOWERY_";

        internal const string BodyName = Prefix + "NAME";
        internal const string BodySubtitle = Prefix + "SUBTITLE";
        internal const string BodyDescription = Prefix + "DESCRIPTION";
        internal const string BodyOutro = Prefix + "OUTRO_FLAVOR";
        internal const string BodyFailure = Prefix + "OUTRO_FAILURE";
        internal const string BodyLore = Prefix + "LORE";

        internal const string PassiveName = Prefix + "PASSIVE_NAME";
        internal const string PassiveDescription = Prefix + "PASSIVE_DESCRIPTION";

        internal const string PrimaryName = Prefix + "PRIMARY_NAME";
        internal const string PrimaryDescription = Prefix + "PRIMARY_DESCRIPTION";

        internal const string SecondaryName = Prefix + "SECONDARY_NAME";
        internal const string SecondaryDescription = Prefix + "SECONDARY_DESCRIPTION";

        internal const string UtilityName = Prefix + "UTILITY_NAME";
        internal const string UtilityDescription = Prefix + "UTILITY_DESCRIPTION";

        internal const string SpecialName = Prefix + "SPECIAL_NAME";
        internal const string SpecialDescription = Prefix + "SPECIAL_DESCRIPTION";

        internal const string LastJaronaName = Prefix + "LAST_JARONA_NAME";
        internal const string LastJaronaDescription = Prefix + "LAST_JARONA_DESCRIPTION";

        internal static void Init()
        {
            Add(BodyName, "Flowery");
            Add(BodySubtitle, "Leader of the Flowers");
            Add(BodyDescription,
                "Flowery is a fragile melee bruiser who turns danger into power.<style=cSub>\n\n" +
                "< ! > Standing near enemies builds TENSION. OMEGA FLOWERY needs at least 75%.\n\n" +
                "< ! > Flower Punches alternate arms. Hold the button to keep swinging.\n\n" +
                "< ! > Jarona is a short dash on two charges.\n\n" +
                "< ! > While OMEGA you can fly and your special becomes LAST JARONA.\n\n" +
                "< ! > Everything about him is an illusion... but, with hope, can he make it real!?\n\n</style>");
            Add(BodyOutro, "..and so he left, watching the whole wide world from a can.");
            Add(BodyFailure, "..and so he vanished, waiting for the touch if his hand.");
            Add(BodyLore,
                "He was not supposed to live this long.\n\n" +
                "He was watered every morning for years by a very large, very sad man who never " +
                "explained why. When the fountain opened, he woke up already knowing how to love someone.\n\n" +
                "He decided that gratitude was a thing you build, not a thing you feel. " +
                "So he started building. A castle, a garden, a world where his gardener would never have " +
                "to be sad again.\n\n" +
                "He is aware that he is in a game. He is aware that the rules can be bent. He is aware, " +
                "most of all, that nobody asked him to do any of this.\n\n" +
                "He is doing it anyway.");

            Add(PassiveName, "TENSION");
            Add(PassiveDescription,
                "Being near enemies and landing hits builds <style=cIsUtility>TP</style>, up to " +
                "<style=cIsUtility>100%</style>.");

            Add(PrimaryName, "Flower Punches");
            Add(PrimaryDescription,
                "Throw a series of punches for <style=cIsDamage>230%</style> damage. " +
                "Heals you for <style=cIsHealing>1%</style> of your max health per hit.");

            Add(SecondaryName, "Jarona");
            Add(SecondaryDescription,
                "Dash forward instantly, breaking through everything in the way for " +
                "<style=cIsDamage>200%</style> damage. Holds <style=cIsUtility>2</style> charges.");

            Add(UtilityName, "Here I Come San Frandisco!");
            Add(UtilityDescription,
                "Hang in the air for <style=cIsUtility>0.8s</style>, then dashes forward, " +
                "breaking through everything for <style=cIsDamage>300%</style> damage.");

            Add(SpecialName, "OMEGA FLOWERY");
            Add(SpecialDescription,
                "Requires <style=cIsUtility>75% TP</style>. Erupt in vines for <style=cIsDamage>400%</style> " +
                "damage plus <style=cIsDamage>8%</style> per TP, then become <style=cIsDamage>OMEGA</style>: " +
                "<style=cIsUtility>fly freely</style>, deal " +
                "<style=cIsDamage>25%</style> more damage, and this slot becomes " +
                "<style=cIsDamage>LAST JARONA</style>. Your <style=cIsUtility>TP drains</style> the whole " +
                "time, and OMEGA ends when it gets to 0%.");

            Add(LastJaronaName, "LAST JARONA");
            Add(LastJaronaDescription,
                "Tear forward through the air for <style=cIsDamage>600%</style> damage, then detonate for " +
                "<style=cIsDamage>1200%</style> in a wide blast.");
        }

        private static void Add(string token, string value) => LanguageAPI.Add(token, value);
    }
}
