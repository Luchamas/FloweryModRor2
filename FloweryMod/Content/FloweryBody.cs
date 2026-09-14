using System.Collections.Generic;
using System.Reflection;
using FloweryMod.Components;
using FloweryMod.Modules;
using R2API;
using RoR2;
using UnityEngine;

namespace FloweryMod.Content
{
    /// <summary>
    /// Builds the Flowery body, master and survivor entry.
    ///
    /// The body is cloned from Loader, the vanilla survivor whose rig already carries melee
    /// punch animations for Flower Punches to drive. Cloning also inherits every piece of survivor
    /// plumbing RoR2 expects - motor, hurtboxes, camera params, drop pod, ragdoll, item displays -
    /// already wired correctly. Only the parts that make Flowery himself are replaced.
    /// </summary>
    internal static class FloweryBody
    {
        internal const string BodyName = "FloweryBody";
        internal const string MasterName = "FloweryMonsterMaster";

        internal static GameObject BodyPrefab;
        internal static GameObject DisplayPrefab;
        internal static SurvivorDef Survivor;

        private static readonly FieldInfo CrosshairField =
            typeof(CharacterBody).GetField("_defaultCrosshairPrefab",
                                           BindingFlags.Instance | BindingFlags.NonPublic);

        internal static void Init()
        {
            var sourceBody = FloweryAssets.Load<GameObject>("RoR2/Base/Loader/LoaderBody.prefab");
            if (sourceBody == null)
            {
                Log.Error("Could not load LoaderBody - Flowery cannot be created.");
                return;
            }

            BodyPrefab = PrefabAPI.InstantiateClone(sourceBody, BodyName, true);

            ConfigureBody();
            ConfigureModel();
            FlowerySkills.Build(BodyPrefab);
            AttachComponents();

            ContentAddition.AddBody(BodyPrefab);

            CreateMaster();
            CreateDisplayPrefab();
            CreateSurvivorDef();
        }

        private static void ConfigureBody()
        {
            var body = BodyPrefab.GetComponent<CharacterBody>();
            if (body == null)
            {
                Log.Error("Cloned body has no CharacterBody component.");
                return;
            }

            body.baseNameToken = Tokens.BodyName;
            body.subtitleNameToken = Tokens.BodySubtitle;
            body.bodyColor = FloweryAssets.Gold;

            if (FloweryAssets.PortraitTexture != null) body.portraitIcon = FloweryAssets.PortraitTexture;

            body.baseMaxHealth = FloweryConfig.BaseHealth.Value;
            body.levelMaxHealth = Mathf.Round(FloweryConfig.BaseHealth.Value * 0.3f);
            body.baseRegen = 1.5f;
            body.levelRegen = 0.2f;
            body.baseMaxShield = 0f;
            body.levelMaxShield = 0f;

            body.baseDamage = FloweryConfig.BaseDamage.Value;
            body.levelDamage = FloweryConfig.BaseDamage.Value * 0.2f;
            body.baseAttackSpeed = 1f;
            body.levelAttackSpeed = 0f;
            body.baseCrit = 1f;
            body.levelCrit = 0f;

            body.baseArmor = FloweryConfig.BaseArmor.Value;
            body.levelArmor = 0f;

            body.baseMoveSpeed = FloweryConfig.BaseMoveSpeed.Value;
            body.levelMoveSpeed = 0f;
            body.baseAcceleration = 80f;
            body.baseJumpPower = 15f;
            body.levelJumpPower = 0f;
            body.baseJumpCount = 1;
            body.sprintingSpeedMultiplier = 1.45f;

            body.autoCalculateLevelStats = false;
            body.hullClassification = HullClassification.Human;
            body.isChampion = false;

            ReplaceSpawnState(body);

            if (CrosshairField != null && FloweryAssets.Crosshair != null)
            {
                CrosshairField.SetValue(body, FloweryAssets.Crosshair);
            }
        }

        /// <summary>
        /// Everywhere Loader's body names vanilla's SpawnTeleporterState, Flowery's body names
        /// FlowerySpawnState instead. Vanilla's looks for the CharacterModel through an Animator
        /// mdlFlowery does not have, and froze him for four seconds at every stage start.
        ///
        /// Two fields can name it, on two different paths, and the one that matters is the state
        /// machine's. A body starts in its Body machine's initialStateType the moment it spawns,
        /// which is how every stage after the first begins. CharacterBody.preferredInitialStateType
        /// is only read by Run.HandlePlayerFirstEntryAnimation, and only for a body with no drop
        /// pod - Loader has one. The first fix set only that field and changed nothing in game.
        /// </summary>
        private static void ReplaceSpawnState(CharacterBody body)
        {
            var vanilla = typeof(EntityStates.SpawnTeleporterState);
            var ours = new EntityStates.SerializableEntityStateType(typeof(SkillStates.FlowerySpawnState));
            var replaced = new List<string>();

            foreach (EntityStateMachine machine in BodyPrefab.GetComponents<EntityStateMachine>())
            {
                if (machine.initialStateType.stateType != vanilla) continue;
                machine.initialStateType = ours;
                replaced.Add("the " + machine.customName + " machine's initial state");
            }

            if (body.preferredInitialStateType.stateType == vanilla)
            {
                body.preferredInitialStateType = ours;
                replaced.Add("the body's preferred initial state");
            }

            if (replaced.Count > 0)
            {
                Log.Info("Spawn: FlowerySpawnState replaces SpawnTeleporterState as " +
                         string.Join(" and ", replaced.ToArray()) + ".");
            }
            else
            {
                Log.Warning("Spawn: nothing on the body names SpawnTeleporterState, so FlowerySpawnState " +
                            "is not used. If Flowery freezes at stage start, find what does.");
            }
        }

        private static void ConfigureModel()
        {
            var modelLocator = BodyPrefab.GetComponent<ModelLocator>();
            if (modelLocator == null || modelLocator.modelBaseTransform == null) return;

            GameObject customModel = FloweryAssets.FromBundle<GameObject>("mdlFlowery");

            // Before the swap, not after: swapping strips Loader's model object and resets its
            // scale, and the hitbox has to already be there to be carried through that unmoved.
            BuildMeleeHitbox(modelLocator);

            if (customModel != null)
            {
                SwapInCustomModel(modelLocator, customModel);
            }
            else
            {
                Log.Error("No mdlFlowery in the asset bundle - Flowery is wearing Loader's model.");
            }

            ConfigureItemDisplays(modelLocator.modelTransform);
        }

        /// <summary>
        /// What Loader's model object is called once it is Flowery's. The skin's renderer paths
        /// are relative to it, so the body and the mannequin must agree on what is inside it -
        /// not on this name, but keeping one name keeps that obvious.
        /// </summary>
        private const string HolderName = "mdlFlowery";

        /// <summary>
        /// Watches for the ragdoll waking, to stop Flowery's animator - see
        /// <see cref="Components.FloweryRagdollWatcher"/>.
        ///
        /// Without a ragdoll of his own there is no corpse to fall any more: the fallback used to
        /// hang him on Loader's, and Loader's skeleton is gone. He then holds his last pose until
        /// the body is cleaned up, which is what bodies with no ragdoll do in vanilla too.
        /// </summary>
        private static void WatchRagdoll(Transform rig, Transform custom, bool ownRagdoll)
        {
            var ragdoll = rig.GetComponent<RagdollController>();
            if (ragdoll == null || !ownRagdoll)
            {
                Log.Warning("Flowery has no ragdoll of his own (Model Ragdoll is off, or his bones " +
                            "did not match) - his corpse will hold its last pose instead of falling.");
                return;
            }

            var watcher = rig.GetComponent<Components.FloweryRagdollWatcher>();
            if (watcher == null) watcher = rig.gameObject.AddComponent<Components.FloweryRagdollWatcher>();
            watcher.ragdoll = ragdoll;
            // His bones are the physics, so his animator must let go of them. This does not rely
            // on RagdollController doing it: two writers on one transform is what stretched the
            // corpse, and it is too important to leave to a private field.
            watcher.animatorToStop = custom.GetComponentInChildren<Animator>(true);
        }

        /// <summary>
        /// Item displays are positioned by Loader's ItemDisplayRuleSet. Its rules name bones through
        /// the ChildLocator - which now points at Flowery's bones - but every offset, rotation and
        /// scale in them was authored against Loader's bones, so items attach to the right bone at
        /// the wrong place. Until he has a rule set of his own, this at least makes hiding them a
        /// config flip rather than a code change.
        /// </summary>
        private static void ConfigureItemDisplays(Transform rig)
        {
            if (rig == null || FloweryConfig.ShowItemDisplays.Value) return;

            var characterModel = rig.GetComponent<CharacterModel>();
            if (characterModel == null) return;

            characterModel.itemDisplayRuleSet = null;
            Log.Info("Item displays hidden: Flowery has no display rules of his own yet, and " +
                     "Loader's are positioned against Loader's skeleton.");
        }

        /// <summary>
        /// Flower Punches needs a HitBoxGroup to swing through. Loader's own fist boxes are sized
        /// for punches, and a custom model is not expected to ship any, so Flowery gets his own
        /// built here against whatever model ended up on the body.
        /// </summary>
        private static void BuildMeleeHitbox(ModelLocator modelLocator)
        {
            Transform model = modelLocator.modelTransform;
            if (model == null) return;

            if (HitBoxGroup.FindByGroupName(model.gameObject, SkillStates.FlowerPunches.HitBoxGroupName) != null)
            {
                return;
            }

            float reach = Mathf.Max(1f, FloweryConfig.PunchRange.Value);

            var hitboxObject = new GameObject("FloweryPunchHitbox");
            hitboxObject.transform.SetParent(model, false);
            // Centred half a reach ahead of the body, so the box spans from chest to full reach.
            hitboxObject.transform.localPosition = new Vector3(0f, 1.1f, reach * 0.5f);
            hitboxObject.transform.localRotation = Quaternion.identity;
            // OverlapAttack reads the transform's scale as the box dimensions.
            hitboxObject.transform.localScale = new Vector3(3.6f, 3.2f, reach);

            var hitBox = hitboxObject.AddComponent<HitBox>();

            var group = model.gameObject.AddComponent<HitBoxGroup>();
            group.groupName = SkillStates.FlowerPunches.HitBoxGroupName;
            group.hitBoxes = new[] { hitBox };
        }

        /// <summary>
        /// Turns Loader's model object into Flowery's.
        ///
        /// That object carries far more than a mesh: HurtBoxGroup (without it Flowery is
        /// unhittable and untargetable), CharacterModel, RagdollController and ChildLocator. So
        /// it is kept as the holder, and in order:
        /// <list type="number">
        /// <item>his model goes in as a child, and CharacterModel is handed his renderers;</item>
        /// <item>the ragdoll is built on his bones;</item>
        /// <item>the hurtboxes and ChildLocator are moved onto his bones (FloweryRig.MoveOntoModel);</item>
        /// <item>what is left of Loader - skeleton, meshes, animator, skins - is deleted
        /// (FloweryRig.StripDonor);</item>
        /// <item>and he is given a skin of his own in place of Loader's.</item>
        /// </list>
        /// </summary>
        private static void SwapInCustomModel(ModelLocator modelLocator, GameObject customModel)
        {
            Transform rig = modelLocator.modelTransform;
            if (rig == null)
            {
                Log.Error("No model transform to attach the custom model to.");
                return;
            }

            GameObject instance = AttachCustomModel(rig, customModel, SkillStates.FloweryAnimations.Fly);

            bool ownRagdoll = FloweryConfig.ModelRagdoll.Value &&
                              FloweryRagdoll.Build(rig, instance.transform);
            FloweryRig.MoveOntoModel(BodyPrefab.transform, rig, instance.transform);
            FloweryRig.StripDonor(BodyPrefab.transform, rig, instance.transform, HolderName);
            WatchRagdoll(rig, instance.transform, ownRagdoll);

            var characterModel = rig.GetComponent<CharacterModel>();
            if (characterModel != null)
            {
                FlowerySkin.Wear(rig.gameObject, FlowerySkin.Get(rig.gameObject, characterModel.baseRendererInfos));
            }

            Log.Info("Custom model is the body's model now (scale " + FloweryConfig.ModelScale.Value +
                     "); nothing of Loader's rig is left on it.");
        }

        /// <summary>
        /// Puts the custom model inside a vanilla model object and hands that object's
        /// CharacterModel over to it. Shared by the body and by the character-select mannequin.
        /// <paramref name="startPose"/> is the animator state the model idles in - flight in a
        /// run, the relaxed standing pose on the menu.
        /// </summary>
        private static GameObject AttachCustomModel(Transform rig, GameObject customModel,
                                                    string startPose, float scaleMultiplier = 1f,
                                                    string fallbackPose = null)
        {
            // Stay INSIDE the model object. Everything that matters hangs off it - CharacterModel
            // drives visibility, overlays and cloaking from its own hierarchy, and moving Flowery
            // out made him invisible. Cancel out the object's own rotation and scale so the
            // config values are relative to plain body space; that inherited transform is what
            // made him giant and upside-down. (StripDonor later resets the object to identity,
            // holding him where he is, so this ends up as his plain local transform.)
            GameObject instance = Object.Instantiate(customModel, rig);
            instance.name = SkillStates.FloweryAnimations.ModelObject;

            Vector3 rigScale = rig.localScale;
            float wanted = Mathf.Max(0.001f, FloweryConfig.ModelScale.Value) * scaleMultiplier;

            instance.transform.localPosition = FloweryConfig.ModelOffset;
            instance.transform.localRotation = Quaternion.Inverse(rig.localRotation) *
                                               Quaternion.Euler(FloweryConfig.ModelRotation);
            instance.transform.localScale = new Vector3(
                wanted / Mathf.Max(0.0001f, rigScale.x),
                wanted / Mathf.Max(0.0001f, rigScale.y),
                wanted / Mathf.Max(0.0001f, rigScale.z));

            Log.Info("Rig '" + rig.name + "' localScale=" + rigScale +
                     " localRotation=" + rig.localRotation.eulerAngles +
                     " -> compensated child scale " + instance.transform.localScale);

            // Register his renderers so overlays, cloaking and elite materials find something to
            // work on. Loader's own meshes are not hidden here: StripDonor deletes them.
            var characterModel = rig.GetComponent<CharacterModel>();
            if (characterModel != null) characterModel.baseRendererInfos = BuildRendererInfos(instance);

            // Starts his pose and keeps the cape in step with it.
            var enforcer = rig.GetComponent<Components.FloweryModelEnforcer>();
            if (enforcer == null) enforcer = rig.gameObject.AddComponent<Components.FloweryModelEnforcer>();
            enforcer.startPose = startPose;
            enforcer.fallbackPose = fallbackPose;

            return instance;
        }

        private static CharacterModel.RendererInfo[] BuildRendererInfos(GameObject model)
        {
            var infos = new List<CharacterModel.RendererInfo>();

            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer is ParticleSystemRenderer) continue;

                if (renderer.sharedMaterials != null && renderer.sharedMaterials.Length > 1)
                {
                    // CharacterModel rebuilds this renderer's whole material array, sized to one
                    // entry plus overlays, so every submesh past the first would stop rendering.
                    // Split the mesh by material in export_fbx.py instead.
                    Log.Warning("Renderer '" + renderer.name + "' has " +
                                renderer.sharedMaterials.Length + " materials; only the first " +
                                "will render in game. It needs to be one material per object.");
                }

                infos.Add(new CharacterModel.RendererInfo
                {
                    renderer = renderer,
                    defaultMaterial = renderer.sharedMaterial,
                    defaultShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On,
                    ignoreOverlays = false,
                    hideOnDeath = false,
                });
            }

            return infos.ToArray();
        }

        private static void AttachComponents()
        {
            if (BodyPrefab.GetComponent<TensionController>() == null)
            {
                BodyPrefab.AddComponent<TensionController>();
            }

            if (BodyPrefab.GetComponent<OmegaFormController>() == null)
            {
                BodyPrefab.AddComponent<OmegaFormController>();
            }

            if (BodyPrefab.GetComponent<TensionHud>() == null)
            {
                BodyPrefab.AddComponent<TensionHud>();
            }

            if (BodyPrefab.GetComponent<FloweryLocomotion>() == null)
            {
                BodyPrefab.AddComponent<FloweryLocomotion>();
            }
        }

        private static void CreateMaster()
        {
            var sourceMaster = FloweryAssets.Load<GameObject>("RoR2/Base/Loader/LoaderMonsterMaster.prefab");
            if (sourceMaster == null)
            {
                Log.Warning("Could not load LoaderMonsterMaster - Flowery will have no AI master.");
                return;
            }

            GameObject master = PrefabAPI.InstantiateClone(sourceMaster, MasterName, true);
            var characterMaster = master.GetComponent<CharacterMaster>();
            if (characterMaster != null) characterMaster.bodyPrefab = BodyPrefab;

            ContentAddition.AddMaster(master);
        }

        /// <summary>
        /// The character-select mannequin.
        ///
        /// It has to be built on Loader's display prefab rather than on the bare model: the menu
        /// needs a CharacterModel and a ModelSkinController to render and light the mannequin at
        /// all, and ModelPanelParameters to frame it in the panel. Handing it the raw model left
        /// the survivor with an empty slot on the select screen and two warnings in the log
        /// saying exactly which components were missing.
        /// </summary>
        private static void CreateDisplayPrefab()
        {
            var sourceDisplay = FloweryAssets.Load<GameObject>("RoR2/Base/Loader/LoaderDisplay.prefab");
            if (sourceDisplay == null)
            {
                Log.Warning("Could not load LoaderDisplay - Flowery will have no select-screen model.");
                return;
            }

            DisplayPrefab = PrefabAPI.InstantiateClone(sourceDisplay, "FloweryDisplay", false);

            var bodyModel = FloweryAssets.FromBundle<GameObject>("mdlFlowery");
            if (bodyModel == null) return;

            // The mannequin's rig is the object carrying the CharacterModel, the same
            // arrangement the body uses; the menu should show him introducing himself, not
            // mid-flight. The entrance runs once and the controller drops him into the
            // looping held pose after it - see FloweryAnimations.SelectIntro.
            var displayModel = DisplayPrefab.GetComponentInChildren<CharacterModel>();
            Transform rig = displayModel != null ? displayModel.transform : DisplayPrefab.transform;

            GameObject instance = AttachCustomModel(rig, bodyModel,
                SkillStates.FloweryAnimations.SelectIntro, FloweryConfig.DisplayModelScale.Value,
                SkillStates.FloweryAnimations.Select);

            // The same conversion as the body. Loader's display rig used to be held down at
            // runtime instead - its animator kept playing the charge punch and its effect
            // objects kept throwing lights and camera shake - and deleting it ends that.
            FloweryRig.MoveOntoModel(DisplayPrefab.transform, rig, instance.transform);
            FloweryRig.StripDonor(DisplayPrefab.transform, rig, instance.transform, HolderName);
            ConfigureItemDisplays(rig);

            // Not optional here: the select screen applies the loadout's skin to the
            // mannequin without checking it has a ModelSkinController to apply it with.
            if (FlowerySkin.Default != null)
            {
                FlowerySkin.Wear(rig.gameObject, FlowerySkin.Default);
            }
            else
            {
                Log.Error("No Flowery skin to put on the character-select mannequin - the select " +
                          "screen will fail to apply a skin to it.");
            }

            Log.Info("Character-select mannequin built on Loader's display prefab at " +
                     FloweryConfig.DisplayModelScale.Value + "x scale.");
        }

        private static void CreateSurvivorDef()
        {
            Survivor = ScriptableObject.CreateInstance<SurvivorDef>();
            ((ScriptableObject)Survivor).name = "FlowerySurvivorDef";
            Survivor.cachedName = "FLOWERY";
            Survivor.bodyPrefab = BodyPrefab;
            Survivor.displayPrefab = DisplayPrefab != null ? DisplayPrefab : BodyPrefab;
            Survivor.primaryColor = FloweryAssets.Gold;
            Survivor.displayNameToken = Tokens.BodyName;
            Survivor.descriptionToken = Tokens.BodyDescription;
            Survivor.outroFlavorToken = Tokens.BodyOutro;
            Survivor.mainEndingEscapeFailureFlavorToken = Tokens.BodyFailure;
            Survivor.desiredSortPosition = 24f;
            Survivor.hidden = false;
            Survivor.unlockableDef = null;

            ContentAddition.AddSurvivorDef(Survivor);
        }
    }
}
