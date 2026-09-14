using BepInEx;
using FloweryMod.Content;
using FloweryMod.Modules;
using R2API.Utils;

namespace FloweryMod
{
    /// <summary>
    /// Adds Flowery, leader of the Flower Kingdom, as a playable survivor.
    /// </summary>
    [BepInDependency("com.bepis.r2api", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.bepis.r2api.content_management", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.bepis.r2api.prefab", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.bepis.r2api.language", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.bepis.r2api.recalculatestats", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.bepis.r2api.networking", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.bepis.r2api.skills", BepInDependency.DependencyFlags.SoftDependency)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    [BepInPlugin(Guid, ModName, Version)]
    public class FloweryPlugin : BaseUnityPlugin
    {
        public const string Author = "deltarune";
        public const string ModName = "FloweryMod";
        public const string Guid = "com." + Author + "." + ModName;
        public const string Version = "1.0.1";

        public static FloweryPlugin Instance { get; private set; }

        private void Awake()
        {
            Instance = this;

            Log.Init(Logger);
            Log.Info("Howdy! Flowery is waking up.");

            FloweryConfig.Init(Config);

            // Order matters: icons come from Assets, buffs use those icons, skills reference both,
            // and the body wires the finished skills onto the prefab.
            FloweryAssets.Init();
            Sounds.Init();
            Tokens.Init();
            Buffs.Init();
            FlowerySkills.RegisterEntityStates();
            FloweryBody.Init();
            FloweryDiagnostics.Init();
            Components.FloweryMenuSounds.Spawn();

            Log.Info("Flowery is ready. Six petals, one very large plan.");
        }
    }
}
