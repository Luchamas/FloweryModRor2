using System.Collections.Generic;
using UnityEngine;

namespace FloweryMod.Components
{
    /// <summary>
    /// Cycles Flowery's materials through the spectrum while OMEGA is active.
    ///
    /// It tints instanced copies of the materials rather than swapping in a custom shader:
    /// RoR2's own shaders vary per renderer, and replacing them would lose elite overlays,
    /// cloaking and the dissolve effect. Tinting works on whatever shader is already there.
    /// The originals are restored on disable, so a mid-run interruption cannot leave him
    /// permanently pink.
    /// </summary>
    public class OmegaRainbow : MonoBehaviour
    {
        /// <summary>Full spectrum sweeps per second.</summary>
        public float cyclesPerSecond = 0.55f;

        /// <summary>How far toward the rainbow colour the base texture is pushed, 0-1.</summary>
        public float strength = 0.75f;

        /// <summary>Extra emission so he glows rather than just changing hue.</summary>
        public float emission = 0.35f;

        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionId = Shader.PropertyToID("_EmColor");
        private static readonly int EmissionPowerId = Shader.PropertyToID("_EmPower");

        /// <summary>
        /// A renderer and everything about it we are going to overwrite. Kept together rather
        /// than in parallel lists so a part cannot get restored from another part's colours.
        /// </summary>
        private struct Tinted
        {
            public Renderer renderer;
            public Color color;
            public Color emissionColor;
            public float emissionPower;
        }

        private readonly List<Tinted> tinted = new List<Tinted>();
        private float phase;

        private void OnEnable()
        {
            tinted.Clear();

            foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
            {
                if (r == null || r.sharedMaterial == null) continue;

                Material m = r.material;
                if (m == null) continue;

                tinted.Add(new Tinted
                {
                    renderer = r,
                    color = m.HasProperty(ColorId) ? m.GetColor(ColorId) : Color.white,
                    emissionColor = m.HasProperty(EmissionId) ? m.GetColor(EmissionId) : Color.black,
                    emissionPower = m.HasProperty(EmissionPowerId) ? m.GetFloat(EmissionPowerId) : 0f,
                });
            }
            phase = 0f;
        }

        private void Update()
        {
            phase += Time.deltaTime * cyclesPerSecond;
            if (phase > 1f) phase -= 1f;

            // One hue for the whole model, every frame. Flowery reaches the game as six
            // renderers rather than one - export_fbx.py splits each mesh so that every Unity
            // renderer owns a single material, which is the only shape CharacterModel's
            // material array will render - so his body, coat, hair, eyes, mouth and shoes are
            // separate objects for a reason that has nothing to do with how he should look.
            // Offsetting the hue per renderer therefore never read as a sweep down the body:
            // GetComponentsInChildren returns them in sibling order, which is arbitrary, so it
            // just painted his face a different colour from his head.
            Color rainbow = Color.HSVToRGB(phase, 0.85f, 1f);

            for (int i = 0; i < tinted.Count; i++)
            {
                Tinted t = tinted[i];
                if (t.renderer == null) continue;

                Material m = t.renderer.material;
                if (m == null) continue;

                if (m.HasProperty(ColorId))
                {
                    m.SetColor(ColorId, Color.Lerp(t.color, rainbow, strength));
                }
                if (m.HasProperty(EmissionId))
                {
                    m.SetColor(EmissionId, rainbow);
                    if (m.HasProperty(EmissionPowerId)) m.SetFloat(EmissionPowerId, emission);
                }
            }
        }

        private void OnDisable()
        {
            for (int i = 0; i < tinted.Count; i++)
            {
                Tinted t = tinted[i];
                if (t.renderer == null) continue;

                Material m = t.renderer.material;
                if (m == null) continue;

                if (m.HasProperty(ColorId)) m.SetColor(ColorId, t.color);

                // Put emission back where it was rather than zeroing it. The parts that glow on
                // their own - his eyes above all - are lit by _EmPower, so forcing it to 0 here
                // left him with dead eyes for the rest of the run once OMEGA ended.
                if (m.HasProperty(EmissionId)) m.SetColor(EmissionId, t.emissionColor);
                if (m.HasProperty(EmissionPowerId)) m.SetFloat(EmissionPowerId, t.emissionPower);
            }
            tinted.Clear();
        }
    }
}
