using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace FloweryMod.Components
{
    /// <summary>
    /// Starts Flowery's pose and keeps his renderers - the cape above all - the way the pose
    /// wants them.
    ///
    /// This began as the enforcer of a model swap: Loader's skin kept pointing CharacterModel at
    /// Loader's meshes and switching them back on at spawn, and this undid it. Loader's rig and
    /// skins are gone now (FloweryRig.StripDonor, FlowerySkin), so there is nothing of his left to
    /// fight. What remains is what was always Flowery's: nothing else starts his animator, and
    /// the game still switches his own renderers back on when it refreshes the model, which the
    /// cape cannot tolerate.
    ///
    /// Skin application happens in Start with no ordering guarantee against this component,
    /// and can run again later (on a skin change), so this re-checks for a few frames and
    /// then keeps a cheap watch rather than assuming it won.
    /// </summary>
    public class FloweryModelEnforcer : MonoBehaviour
    {
        /// <summary>
        /// The animator state this model idles in. Nothing else drives Flowery's animator, so
        /// whatever is set here is what he does when no skill is running: flight on a body,
        /// the relaxed standing pose on the character-select mannequin.
        /// </summary>
        public string startPose = SkillStates.FloweryAnimations.Fly;

        /// <summary>
        /// What to play if <see cref="startPose"/> is not a state this bundle's controller has.
        /// Null means there is nothing to fall back to and the wanted pose is played regardless.
        ///
        /// This exists for exactly one failure, and it is a silent one: Animator.Play on a state
        /// the controller does not contain does nothing and logs nothing, leaving the animator in
        /// its DEFAULT state - which for Flowery is Fly. So an asset bundle built before a pose
        /// existed does not fall back to the previous pose, it falls back to hovering. The
        /// character-select entrance is the first pose to be added since bundles started
        /// shipping, so it is the first to need this.
        /// </summary>
        public string fallbackPose;

        private CharacterModel characterModel;
        private Transform custom;

        /// <summary>
        /// Whether the cape is currently off. The renderer sweep in <see cref="Apply"/> treats a
        /// disabled renderer of Flowery's as a mistake and switches it back on, so it has to be
        /// told the cape is off on purpose.
        ///
        /// This is the settled answer, not the request - see <see cref="WantCape"/>.
        /// </summary>
        internal bool capeHidden;

        /// <summary>What the pose currently on the model asks for. Set through WantCape.</summary>
        private bool capeWanted = true;

        /// <summary>How long <see cref="capeWanted"/> has been true without interruption.</summary>
        private float capeWantedFor = CapeShowDelay;

        /// <summary>
        /// How long a cape-wearing pose must hold before the cape actually comes back. Hiding is
        /// instant; only showing waits. See <see cref="UpdateCape"/> for why.
        /// </summary>
        private const float CapeShowDelay = 0.15f;

        private CharacterModel.RendererInfo[] ourInfos;
        private int framesLeft = 10;
        private float recheckTimer;

        private void Start()
        {
            characterModel = GetComponent<CharacterModel>();
            custom = FindCustom(transform);

            // The item display rules are still Loader's, with offsets authored for Loader's bones
            // (see FloweryBody.ConfigureItemDisplays). Clearing the rule set when the prefab is
            // built has not always survived spawn, so it is re-asserted here.
            if (characterModel != null && !Modules.FloweryConfig.ShowItemDisplays.Value &&
                characterModel.itemDisplayRuleSet != null)
            {
                characterModel.itemDisplayRuleSet = null;
                Modules.Log.Info("Item displays cleared on the live model.");
            }

            if (custom == null)
            {
                Modules.Log.Warning("FloweryModelEnforcer: no '" +
                    SkillStates.FloweryAnimations.ModelObject + "' under the model; nothing to enforce.");
                enabled = false;
                return;
            }

            ourInfos = BuildInfos(custom.gameObject);
            RefreshCapeRenderers();

            var anim = custom.GetComponentInChildren<Animator>(true);
            if (anim == null)
            {
                Modules.Log.Error("FloweryModelEnforcer: the custom model has NO Animator - " +
                                  "it can only ever show its bind pose.");
            }
            else
            {
                Modules.Log.Info("Flowery animator: enabled=" + anim.enabled +
                                 " controller=" + (anim.runtimeAnimatorController != null
                                     ? anim.runtimeAnimatorController.name : "NULL") +
                                 " avatar=" + (anim.avatar != null ? anim.avatar.name : "NULL") +
                                 " valid=" + (anim.avatar != null && anim.avatar.isValid) +
                                 " layers=" + anim.layerCount +
                                 " culling=" + anim.cullingMode);
                if (anim.runtimeAnimatorController != null)
                {
                    var names = new List<string>();
                    foreach (var c in anim.runtimeAnimatorController.animationClips)
                    {
                        if (c != null && !names.Contains(c.name)) names.Add(c.name);
                    }
                    Modules.Log.Info("Flowery clips: " + string.Join(", ", names.ToArray()));
                }
                // Nothing else drives this animator, so start it on this model's idle pose.
                anim.enabled = true;
                anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                anim.applyRootMotion = false;
                anim.Play(ResolvePose(anim), 0);
            }

            Apply();
        }

        /// <summary>
        /// The pose to actually start on: <see cref="startPose"/> if this controller has it,
        /// <see cref="fallbackPose"/> if it does not. See fallbackPose for why this is checked
        /// rather than assumed.
        /// </summary>
        private string ResolvePose(Animator anim)
        {
            if (string.IsNullOrEmpty(fallbackPose) || fallbackPose == startPose) return startPose;

            int layer = anim.GetLayerIndex(SkillStates.FloweryAnimations.BaseLayer);
            if (layer < 0) layer = 0;
            if (anim.HasState(layer, Animator.StringToHash(startPose))) return startPose;

            // Reported through the animator's own once-per-pose reporter rather than logged
            // straight: the select screen rebuilds this mannequin every time the highlighted
            // survivor changes, so a raw warning here is one line per click on Flowery.
            SkillStates.FloweryAnimations.ReportOnce(startPose,
                "no pose '" + startPose + "' - wearing '" + fallbackPose + "' instead. Re-export " +
                "the FBX and re-run Flowery/Set Up Low-Poly Flowery to get it.");
            return fallbackPose;
        }

        /// <summary>
        /// Asks for the cape, on behalf of whatever pose is being played. Cheap and idempotent -
        /// it is called on every pose change, not every frame.
        /// </summary>
        internal void WantCape(bool visible)
        {
            if (capeWanted == visible) return;
            capeWanted = visible;
            capeWantedFor = 0f;
            // Hiding is not allowed to wait a frame: a swing must never start with the cape on.
            if (!visible) ApplyCape(true);
        }

        /// <summary>
        /// Brings the cape back once a pose that wears it has held for <see cref="CapeShowDelay"/>.
        ///
        /// The delay exists for exactly one case: holding the primary. A swing ends by handing the
        /// machine back to its main state, which plays the flight pose, and the next swing only
        /// begins a frame or two later when the held button re-executes the skill. The pose itself
        /// barely moves across that gap - the crossfade is 120ms and never gets near the flight
        /// pose - but the cape is a renderer toggle with no crossfade to hide behind, so it popped
        /// fully on and off again once per swing. That was the flicker.
        ///
        /// Waiting to show costs nothing anywhere else: 150ms is shorter than the blend back to
        /// the flight pose, so on the last swing of a combo the cape reappears as he settles.
        ///
        /// The result is then re-asserted EVERY frame, not just when the pose changes. This
        /// component exists because Flowery's renderers do not stay how they are put - the cape
        /// is registered in CharacterModel.baseRendererInfos like every other part of him, and
        /// the game switches those back on whenever it refreshes the model. The sweep in
        /// <see cref="Apply"/> already fights that, but only once a second, so anything that
        /// re-enabled the cape got up to a full second of visibility: that is why the cape came
        /// back on a punch that CONNECTED and not on one that swung through air. Landing a hit
        /// is what dirties the model. Asserting at frame rate closes the window without having
        /// to know which refresh did it.
        /// </summary>
        private void UpdateCape()
        {
            if (custom == null) return;

            if (!capeWanted)
            {
                capeWantedFor = 0f;
            }
            else if (capeWantedFor < CapeShowDelay)
            {
                capeWantedFor += Time.deltaTime;
            }

            ApplyCape(!capeWanted || capeWantedFor < CapeShowDelay);
        }

        /// <summary>
        /// Switches the cape renderers now and records it for the sweep in Apply. Runs every
        /// frame, so it works off a cached list rather than searching the hierarchy.
        /// </summary>
        private void ApplyCape(bool hidden)
        {
            capeHidden = hidden;
            if (capeRenderers == null) return;

            bool on = !hidden && !Invisible;
            for (int i = 0; i < capeRenderers.Length; i++)
            {
                Renderer r = capeRenderers[i];
                if (r == null) { RefreshCapeRenderers(); return; }   // model was rebuilt under us
                if (r.enabled != on) r.enabled = on;
            }
        }

        /// <summary>
        /// Whether the game has hidden him outright - the stage-entry delay before the teleporter
        /// effect prints him in (FlowerySpawnState), and anything else that raises the count.
        /// CharacterModel switches renderers off for that and only switches them back on when the
        /// count drops, so both sweeps here must not turn anything on while it is up: they run
        /// every frame at first, and would have him standing visible through the whole delay.
        /// </summary>
        private bool Invisible => characterModel != null && characterModel.invisibilityCount > 0;

        /// <summary>The cape renderers, cached so the per-frame assert costs nothing.</summary>
        private Renderer[] capeRenderers;

        private void RefreshCapeRenderers()
        {
            if (custom == null) { capeRenderers = null; return; }

            var found = new List<Renderer>();
            foreach (Renderer r in custom.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null || r is ParticleSystemRenderer || !IsCape(r)) continue;
                found.Add(r);
            }
            capeRenderers = found.ToArray();
        }

        private void Update()
        {
            UpdateCape();

            // The skin can be applied after us, so keep asserting for a few frames...
            if (framesLeft > 0)
            {
                framesLeft--;
                Apply();
                return;
            }

            // ...then settle into an occasional check, which also covers a later skin change.
            recheckTimer -= Time.deltaTime;
            if (recheckTimer > 0f) return;
            recheckTimer = 1f;
            Apply();
        }

        private void Apply()
        {
            if (custom == null) return;

            bool invisible = Invisible;
            foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
            {
                if (r == null || r is ParticleSystemRenderer) continue;

                bool isOurs = r.transform.IsChildOf(custom);
                bool want = isOurs && !invisible && !(capeHidden && IsCape(r));
                if (r.enabled != want) r.enabled = want;
            }

            // Keep his renderers in the CharacterModel so overlays, elite materials and cloaking
            // act on them. His skin sets the same renderers, so this is a formality now - but a
            // cheap one, and it keeps a later skin that forgets one from losing it silently.
            if (characterModel != null && ourInfos != null &&
                characterModel.baseRendererInfos != ourInfos)
            {
                characterModel.baseRendererInfos = ourInfos;
            }
        }

        private static bool IsCape(Renderer renderer)
        {
            return renderer.name.StartsWith(SkillStates.FloweryAnimations.CapeMesh);
        }

        /// <summary>
        /// Flowery's model instance under a model object: normally its direct child (mdlFlowery),
        /// searched for more widely only as a fallback.
        /// </summary>
        internal static Transform FindCustom(Transform modelTransform)
        {
            string want = SkillStates.FloweryAnimations.ModelObject;
            Transform direct = modelTransform.Find(want);
            if (direct != null) return direct;

            Transform parent = modelTransform.parent;
            if (parent != null)
            {
                Transform sibling = parent.Find(want);
                if (sibling != null) return sibling;
            }

            foreach (Transform t in modelTransform.root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == want) return t;
            }
            return null;
        }

        private static CharacterModel.RendererInfo[] BuildInfos(GameObject model)
        {
            var infos = new List<CharacterModel.RendererInfo>();
            foreach (Renderer r in model.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer) continue;
                if (r.sharedMaterials != null && r.sharedMaterials.Length > 1)
                {
                    Modules.Log.Warning("Renderer '" + r.name + "' has " + r.sharedMaterials.Length +
                        " materials; CharacterModel will drop all but the first. Split the mesh " +
                        "by material in export_fbx.py.");
                }
                infos.Add(new CharacterModel.RendererInfo
                {
                    renderer = r,
                    defaultMaterial = r.sharedMaterial,
                    defaultShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On,
                    ignoreOverlays = false,
                    hideOnDeath = false,
                });
            }
            return infos.ToArray();
        }
    }
}
