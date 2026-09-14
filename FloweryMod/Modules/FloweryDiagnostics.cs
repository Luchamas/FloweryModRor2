using FloweryMod.Content;
using UnityEngine;
using RoR2;

namespace FloweryMod.Modules
{
    /// <summary>
    /// Reports, once the catalogs are built, whether Flowery actually made it in.
    ///
    /// Content registration failures in RoR2 are usually silent - the survivor simply is not in
    /// the menu - so this turns "he is missing" into a line in the log that says why.
    /// </summary>
    internal static class FloweryDiagnostics
    {
        internal static void Init()
        {
            RoR2Application.onLoad += Report;
        }

        private static void Report()
        {
            if (FloweryBody.BodyPrefab == null)
            {
                Log.Error("Flowery body prefab was never created - the survivor is not registered.");
                return;
            }

            BodyIndex bodyIndex = BodyCatalog.FindBodyIndex(FloweryBody.BodyName);

            if (bodyIndex == BodyIndex.None)
            {
                Log.Error("BodyCatalog has no '" + FloweryBody.BodyName + "' - the body failed to register.");
            }
            else
            {
                Log.Info("Body registered as index " + (int)bodyIndex + ".");
            }

            // Look the survivor up through the body rather than by name: the catalog keys on
            // SurvivorDef.cachedName, which it fills in itself during init.
            SurvivorDef registered = SurvivorCatalog.FindSurvivorDefFromBody(FloweryBody.BodyPrefab);

            if (registered == null)
            {
                Log.Error("SurvivorCatalog has no entry for Flowery - he will not appear in character select.");
            }
            else
            {
                int position = 0;
                bool inOrderedList = false;
                foreach (SurvivorDef def in SurvivorCatalog.orderedSurvivorDefs)
                {
                    if (def == registered) { inOrderedList = true; break; }
                    position++;
                }

                Log.Info("Survivor registered: index " + (int)registered.survivorIndex +
                         ", cachedName '" + registered.cachedName + "', " +
                         (inOrderedList
                             ? "slot " + position + " of " + SurvivorCatalog.survivorCount + " in character select."
                             : "MISSING from the ordered list - he will not show in character select."));

                Log.Info("Display name resolves to: \"" + Language.GetString(registered.displayNameToken) + "\".");

                // Round-trip the index: if this comes back as another survivor, picking Flowery
                // in the menu would hand the player the wrong character.
                SurvivorDef roundTrip = SurvivorCatalog.GetSurvivorDef(registered.survivorIndex);
                if (roundTrip != registered)
                {
                    Log.Error("Survivor index " + (int)registered.survivorIndex + " resolves to '" +
                              (roundTrip != null ? roundTrip.cachedName : "null") +
                              "' instead of Flowery - the catalog entry is inconsistent.");
                }
            }

            var skillLocator = FloweryBody.BodyPrefab.GetComponent<SkillLocator>();
            if (skillLocator != null)
            {
                Log.Info("Skills: " +
                         Describe(skillLocator.primary) + ", " +
                         Describe(skillLocator.secondary) + ", " +
                         Describe(skillLocator.utility) + ", " +
                         Describe(skillLocator.special) + ".");
            }

            if (FlowerySkills.LastJaronaSkill == null)
            {
                Log.Error("LAST JARONA was never built - the OMEGA special override will do nothing.");
            }

            CheckMeleeHitbox();
            CheckBorrowedEffects();
            CheckStateMachines();
            ReportModelComponents();
            ReportSkins(bodyIndex);
            Sounds.ReportAudioState();
        }

        /// <summary>
        /// Says which of the borrowed vanilla VFX are EffectCatalog entries and which are not.
        ///
        /// A prefab that is not in the catalog cannot go through EffectManager: the call draws
        /// nothing and logs a line every single time it is made, so one bad pick is dozens of
        /// errors a fight rather than one at startup. FloweryEffects instantiates those by hand
        /// instead; this is the record of which path each of them ends up taking.
        /// </summary>
        private static void CheckBorrowedEffects()
        {
            var report = new System.Collections.Generic.List<string>();

            DescribeEffect(report, "swing", FloweryAssets.LashSwingEffect);
            DescribeEffect(report, "punch impact", FloweryAssets.LashHitEffect);
            DescribeEffect(report, "dash charge", FloweryAssets.JaronaChargeEffect);
            DescribeEffect(report, "dash impact", FloweryAssets.PelletHitEffect);
            DescribeEffect(report, "LAST JARONA blast", FloweryAssets.JaronaExplosionEffect);
            DescribeEffect(report, "OMEGA eruption", FloweryAssets.OmegaEruptionEffect);

            Log.Info("Borrowed VFX: " + string.Join(", ", report.ToArray()) + ".");
        }

        private static void DescribeEffect(System.Collections.Generic.List<string> into,
                                           string role, GameObject prefab)
        {
            if (prefab == null)
            {
                into.Add(role + " never loaded");
                return;
            }

            bool catalogued = EffectCatalog.FindEffectIndexFromPrefab(prefab) != EffectIndex.Invalid;
            into.Add(role + " '" + prefab.name + "' " +
                     (catalogued ? "networked" : "local-only (not an EffectCatalog entry)"));
        }

        /// <summary>
        /// Skills activate on state machines looked up by name. If the donor body ever
        /// changes and a machine is missing, the skill just silently does nothing.
        /// </summary>
        private static void CheckStateMachines()
        {
            var machines = FloweryBody.BodyPrefab.GetComponents<EntityStateMachine>();
            var names = new System.Collections.Generic.List<string>();
            foreach (var machine in machines) names.Add(machine.customName);

            Log.Info("State machines: " + string.Join(", ", names.ToArray()) + ".");

            foreach (string required in new[] { "Body", "Weapon" })
            {
                if (!names.Contains(required))
                {
                    Log.Error("No EntityStateMachine named '" + required +
                              "' - skills bound to it will not fire.");
                }
            }
        }

        /// <summary>
        /// Flower Punches silently does zero damage if its HitBoxGroup is missing, so it is worth
        /// saying out loud at startup rather than discovering it mid-run.
        /// </summary>
        private static void CheckMeleeHitbox()
        {
            var modelLocator = FloweryBody.BodyPrefab.GetComponent<ModelLocator>();
            Transform model = modelLocator != null ? modelLocator.modelTransform : null;

            if (model == null)
            {
                Log.Error("Flowery has no model transform - Flower Punches cannot find its hitboxes.");
                return;
            }

            var group = HitBoxGroup.FindByGroupName(model.gameObject, SkillStates.FlowerPunches.HitBoxGroupName);
            if (group == null || group.hitBoxes == null || group.hitBoxes.Length == 0)
            {
                Log.Error("HitBoxGroup '" + SkillStates.FlowerPunches.HitBoxGroupName +
                          "' is missing - Flower Punches will not hit anything.");
            }
            else
            {
                Log.Info("Flower Punches hitbox ready (" + group.hitBoxes.Length + " box, reach " +
                         FloweryConfig.PunchRange.Value + "m).");
            }
        }

        /// <summary>
        /// Lists what actually lives on the model transform. Swapping in a custom model replaces
        /// that object, so anything here is something the swap has to carry across - and losing
        /// the hurtboxes silently would make Flowery unhittable rather than throw.
        /// </summary>
        private static void ReportModelComponents()
        {
            var modelLocator = FloweryBody.BodyPrefab.GetComponent<ModelLocator>();
            Transform model = modelLocator != null ? modelLocator.modelTransform : null;
            if (model == null) return;

            var names = new System.Collections.Generic.List<string>();
            foreach (Component component in model.GetComponents<Component>())
            {
                if (component != null) names.Add(component.GetType().Name);
            }

            Log.Info("Model components: " + string.Join(", ", names.ToArray()) + ".");
            Log.Info("Model children of '" + model.name + "': " + ChildNames(model) + ".");

            ReportRig(model);

            var hurtBoxGroup = model.GetComponentInChildren<HurtBoxGroup>(true);
            Log.Info("Hurtboxes under the model: " +
                     (hurtBoxGroup != null
                         ? (hurtBoxGroup.hurtBoxes != null ? hurtBoxGroup.hurtBoxes.Length : 0) +
                           " box(es), bullseyes " + hurtBoxGroup.bullseyeCount +
                           ", on '" + hurtBoxGroup.gameObject.name + "'"
                         : "none - they live outside the model"));
        }

        /// <summary>
        /// Whether SkinCatalog found Flowery's skin and baked it. A skin the catalog missed never
        /// shows in the loadout; one it failed to bake applies nothing.
        /// </summary>
        private static void ReportSkins(BodyIndex bodyIndex)
        {
            if (bodyIndex == BodyIndex.None) return;

            SkinDef[] skins = SkinCatalog.GetBodySkinDefs(bodyIndex);
            if (skins == null || skins.Length == 0)
            {
                Log.Warning("SkinCatalog has no skins for Flowery - the loadout will show no skin row.");
                return;
            }

            var described = new System.Collections.Generic.List<string>();
            foreach (SkinDef skin in skins)
            {
                if (skin == null) { described.Add("null"); continue; }
                described.Add("'" + skin.name + "' (" + (skin.runtimeSkin != null
                    ? skin.runtimeSkin.rendererInfoTemplates.Length + " renderers baked"
                    : "NOT baked") + ")");
            }
            Log.Info("Skins: " + string.Join(", ", described.ToArray()) + ".");
        }

        private static string ChildNames(Transform t)
        {
            var names = new System.Collections.Generic.List<string>();
            foreach (Transform child in t) names.Add(child.name);
            return string.Join(", ", names.ToArray());
        }

        /// <summary>
        /// What animates the model and what skins it.
        ///
        /// The model object must have NO Animator - Flowery's lives on the model inside it - and
        /// exactly his one skin; anything else means some of Loader's rig survived the strip.
        /// </summary>
        private static void ReportRig(Transform model)
        {
            Transform custom = Components.FloweryModelEnforcer.FindCustom(model);
            if (custom == null || custom == model)
            {
                Log.Warning("Rig: no Flowery model on the body - he is wearing Loader's.");
                return;
            }

            var skinController = model.GetComponent<ModelSkinController>();
            Animator own = custom.GetComponentInChildren<Animator>(true);
            Log.Info("Rig: model object '" + model.name + "' has " +
                     (model.GetComponent<Animator>() != null ? "an Animator (Loader's survived!)" : "no Animator") +
                     "; Flowery's animator " +
                     (own != null && own.runtimeAnimatorController != null
                         ? "'" + own.runtimeAnimatorController.name + "'"
                         : "MISSING") +
                     "; skins: " + (skinController != null && skinController.skins != null
                         ? string.Join(", ", System.Array.ConvertAll(skinController.skins,
                               s => s != null ? s.name : "null"))
                         : "none") + ".");
        }

        private static string Describe(GenericSkill slot)
        {
            if (slot == null) return "<missing slot>";
            if (slot.skillFamily == null) return slot.skillName + " <no family>";
            return slot.skillName;
        }
    }
}
