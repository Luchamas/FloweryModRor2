using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.AddressableAssets;
// RoR2 declares its own Path type, which shadows System.IO.Path once RoR2 is imported.
using Path = System.IO.Path;

namespace FloweryMod.Modules
{
    /// <summary>
    /// Central place for every asset the mod needs: Flowery's model from the asset bundle, his
    /// icons and HUD art from loose image files, and the vanilla effects he borrows.
    /// </summary>
    internal static class FloweryAssets
    {
        internal const string BundleName = "flowery";

        internal static AssetBundle Bundle;

        // Palette lifted from Flowery's Dark World design.
        internal static readonly Color Gold = new Color(1f, 0.84f, 0.24f);
        internal static readonly Color SoulBlue = new Color(0.25f, 0.62f, 1f);

        /// <summary>The TP bar fill, rgb(255, 160, 64).</summary>
        internal static readonly Color32 TpFillColor = new Color32(255, 160, 64, 255);

        // Borrowed vanilla assets. Keys verified against the shipped Addressables catalog.
        internal static GameObject LashHitEffect;
        internal static GameObject PelletHitEffect;
        internal static GameObject JaronaChargeEffect;
        internal static GameObject JaronaExplosionEffect;
        internal static GameObject OmegaEruptionEffect;
        internal static GameObject Crosshair;

        /// <summary>The survivor portrait, as a texture - CharacterBody wants a Texture, not a Sprite.</summary>
        internal static Texture PortraitTexture;

        /// <summary>The TP bar frame, the same artwork recoloured into a fill overlay, and the
        /// supplied "%" and "MAX" lettering that sit under the number.</summary>
        internal static Sprite TpBarFrame;
        internal static Sprite TpBarFill;
        internal static Sprite TpPercent;
        internal static Sprite TpMax;

        internal static Sprite IconFlowery;
        internal static Sprite IconPassive;
        internal static Sprite IconFlowerPunches;
        internal static Sprite IconJarona;
        internal static Sprite IconSanFrancisco;
        internal static Sprite IconOmega;
        internal static Sprite IconLastJarona;
        internal static Sprite IconOmegaBuff;

        internal static void Init()
        {
            LoadBundle();
            LoadVanillaAssets();
            BuildIcons();
        }

        private static void LoadBundle()
        {
            try
            {
                var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (string.IsNullOrEmpty(dir)) return;

                // Accept both "Assets/flowery" and a bundle dropped next to the dll.
                var candidates = new[]
                {
                    Path.Combine(Path.Combine(dir, "Assets"), BundleName),
                    Path.Combine(dir, BundleName),
                };

                foreach (var path in candidates)
                {
                    if (!File.Exists(path)) continue;
                    Bundle = AssetBundle.LoadFromFile(path);
                    if (Bundle != null)
                    {
                        Log.Info("Loaded asset bundle from " + path);
                        return;
                    }
                }

                Log.Warning("No Flowery asset bundle found - he will wear Loader's model. " +
                            "Expected it at [plugin folder]/Assets/" + BundleName + ".");
            }
            catch (Exception e)
            {
                Log.Error("Failed to load the Flowery asset bundle: " + e);
            }
        }

        /// <summary>Loads an asset from the mod bundle, returning null (not throwing) when absent.</summary>
        internal static T FromBundle<T>(string name) where T : UnityEngine.Object
        {
            if (Bundle == null || string.IsNullOrEmpty(name)) return null;
            try
            {
                return Bundle.LoadAsset<T>(name);
            }
            catch (Exception e)
            {
                Log.Warning("Bundle asset '" + name + "' failed to load: " + e.Message);
                return null;
            }
        }

        /// <summary>Synchronously resolves a vanilla Addressables key.</summary>
        internal static T Load<T>(string key) where T : UnityEngine.Object
        {
            try
            {
                return Addressables.LoadAssetAsync<T>(key).WaitForCompletion();
            }
            catch (Exception e)
            {
                Log.Warning("Could not load addressable '" + key + "': " + e.Message);
                return null;
            }
        }

        private static void LoadVanillaAssets()
        {
            LashHitEffect = Load<GameObject>("RoR2/Base/Merc/OmniImpactVFXSlashMerc.prefab");

            PelletHitEffect = Load<GameObject>("RoR2/Base/Huntress/OmniImpactVFXHuntress.prefab");

            JaronaChargeEffect = Load<GameObject>("RoR2/Base/Common/VFX/MuzzleflashSmokeRing.prefab");
            JaronaExplosionEffect = Load<GameObject>("RoR2/Base/Croco/CrocoLeapExplosion.prefab");

            OmegaEruptionEffect = Load<GameObject>("RoR2/Base/Treebot/OmniExplosionVFXTreebot.prefab");

            Crosshair = Load<GameObject>("RoR2/Base/UI/SimpleDotCrosshair.prefab");
        }

        private static void BuildIcons()
        {
            LoadImageFolders();

            IconFlowery = RequiredFile("CharSelection");
            IconPassive = RequiredFile("Passive");
            IconFlowerPunches = RequiredFile("Primary");
            IconJarona = RequiredFile("Jarona");
            IconSanFrancisco = RequiredFile("HereICome");
            IconOmega = RequiredFile("OmegaFlowery");
            IconLastJarona = RequiredFile("LastJarona");
            IconOmegaBuff = IconOmega;

            PortraitTexture = IconFlowery != null ? IconFlowery.texture : null;

            // Supplied artwork, so both match the hand-lettered "TP" exactly. MAX arrives
            // already coloured, so it is drawn untinted.
            TpPercent = FromFile("Percentage");
            if (TpPercent == null)
            {
                Log.Warning("No Hud/Percentage image found - the TP bar will show no percent sign.");
            }

            TpMax = FromFile("MAX");
            if (TpMax == null)
            {
                Log.Warning("No Hud/MAX image found - a full TP bar will show nothing in its place.");
            }

            BuildTpBar();
        }

        // ---------------------------------------------------------------------
        // Loose image files. Same idea as the voice clips: drop a PNG in a folder
        // and it is in the game next build, with no Unity round-trip.
        // ---------------------------------------------------------------------

        private static readonly Dictionary<string, Sprite> FileSprites =
            new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

        private static Sprite FromFile(string name)
        {
            return FileSprites.TryGetValue(name, out Sprite sprite) ? sprite : null;
        }

        /// <summary>A skill icon: nothing stands in for a missing one, so say which.</summary>
        private static Sprite RequiredFile(string name)
        {
            Sprite sprite = FromFile(name);
            if (sprite == null) Log.Warning("No Icons/" + name + " image found - that icon will be blank.");
            return sprite;
        }

        private static void LoadImageFolders()
        {
            string root = PluginFolder();
            if (root == null) return;

            foreach (string folder in new[] { "Icons", "Hud" })
            {
                string path = Path.Combine(Path.Combine(root, "Assets"), folder);
                if (!Directory.Exists(path)) continue;

                foreach (string file in Directory.GetFiles(path))
                {
                    string extension = Path.GetExtension(file).ToLowerInvariant();
                    if (extension != ".png" && extension != ".jpg" && extension != ".jpeg") continue;

                    Sprite sprite = LoadSpriteFromFile(file);
                    if (sprite != null) FileSprites[Path.GetFileNameWithoutExtension(file)] = sprite;
                }
            }

            if (FileSprites.Count > 0)
            {
                var names = new List<string>(FileSprites.Keys);
                names.Sort();
                Log.Info("Loaded " + FileSprites.Count + " image(s): " + string.Join(", ", names.ToArray()));
            }
        }

        private static string PluginFolder()
        {
            try
            {
                return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            }
            catch (Exception e)
            {
                Log.Error("Could not resolve the plugin folder: " + e.Message);
                return null;
            }
        }

        private static Sprite LoadSpriteFromFile(string file)
        {
            try
            {
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                };

                if (!texture.LoadImage(System.IO.File.ReadAllBytes(file)))
                {
                    Log.Warning("Could not decode image " + Path.GetFileName(file));
                    return null;
                }

                return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                                     new Vector2(0.5f, 0.5f));
            }
            catch (Exception e)
            {
                Log.Warning("Failed to load image " + Path.GetFileName(file) + ": " + e.Message);
                return null;
            }
        }

        /// <summary>
        /// The TP bar art is an empty frame: black outline, dark red interior, and "TP" already
        /// lettered on the left. The fill overlay is that same artwork with the red interior
        /// recoloured and everything else erased, so the fill follows the frame's slanted shape
        /// exactly instead of being an approximate rectangle laid over it.
        /// </summary>
        private static void BuildTpBar()
        {
            TpBarFrame = FromFile("TP_Bar_Empty");
            if (TpBarFrame == null)
            {
                Log.Warning("No Hud/TP_Bar_Empty image found - there will be no TP bar.");
                return;
            }

            try
            {
                Texture2D source = TpBarFrame.texture;
                Color32[] pixels = source.GetPixels32();
                Color32 fill = TpFillColor;
                var clear = new Color32(0, 0, 0, 0);

                for (int i = 0; i < pixels.Length; i++)
                {
                    Color32 pixel = pixels[i];
                    bool isInterior = pixel.a > 8 && pixel.r > pixel.g + 40 && pixel.r > pixel.b + 40;
                    pixels[i] = isInterior ? fill : clear;
                }

                var texture = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                };
                texture.SetPixels32(pixels);
                texture.Apply(false, false);

                TpBarFill = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                                          new Vector2(0.5f, 0.5f));
            }
            catch (Exception e)
            {
                Log.Warning("Could not build the TP bar fill overlay: " + e.Message);
            }
        }


        // ---------------------------------------------------------------------
        // Pixel digits for the TP readout.
        //
        // The bar artwork letters "TP" and "%" in a chunky pixel face; the HUD's
        // own TMP font next to them looks out of place. Drawing the number as
        // pixels with point filtering keeps it in the same visual language.
        // ---------------------------------------------------------------------

        private const int GlyphWidth = 5;
        private const int GlyphHeight = 7;
        private const int GlyphSpacing = 1;
        private const int GlyphScale = 8;

        /// <summary>
        /// Readouts are drawn into a canvas a fixed number of glyphs wide, centred, whatever the
        /// text. Sprites of differing aspect ratio get scaled differently by the layout - a lone
        /// "5" is tall and narrow, so it fits to height and comes out far larger than "48" - and
        /// a constant canvas keeps the glyphs one size. The number never reaches three digits
        /// because 100 reads as MAX instead.
        /// </summary>
        private const int NumberCanvasGlyphs = 2;

        /// <summary>Row bitmaps, most significant bit leftmost, five columns wide.</summary>
        private static readonly Dictionary<char, byte[]> Glyphs = new Dictionary<char, byte[]>
        {
            { '0', new byte[] { 0x0E, 0x11, 0x13, 0x15, 0x19, 0x11, 0x0E } },
            { '1', new byte[] { 0x04, 0x0C, 0x04, 0x04, 0x04, 0x04, 0x0E } },
            { '2', new byte[] { 0x0E, 0x11, 0x01, 0x02, 0x04, 0x08, 0x1F } },
            { '3', new byte[] { 0x1F, 0x02, 0x04, 0x02, 0x01, 0x11, 0x0E } },
            { '4', new byte[] { 0x02, 0x06, 0x0A, 0x12, 0x1F, 0x02, 0x02 } },
            { '5', new byte[] { 0x1F, 0x10, 0x1E, 0x01, 0x01, 0x11, 0x0E } },
            { '6', new byte[] { 0x06, 0x08, 0x10, 0x1E, 0x11, 0x11, 0x0E } },
            { '7', new byte[] { 0x1F, 0x01, 0x02, 0x04, 0x08, 0x08, 0x08 } },
            { '8', new byte[] { 0x0E, 0x11, 0x11, 0x0E, 0x11, 0x11, 0x0E } },
            { '9', new byte[] { 0x0E, 0x11, 0x11, 0x0F, 0x01, 0x02, 0x0C } },
        };

        private static readonly Dictionary<string, Sprite> TextSprites =
            new Dictionary<string, Sprite>();

        /// <summary>A white pixel-font sprite of <paramref name="value"/>, cached per value.</summary>
        internal static Sprite NumberSprite(int value)
        {
            return TextSprite(Mathf.Clamp(value, 0, 99).ToString(), NumberCanvasGlyphs);
        }

        private static Sprite TextSprite(string text, int canvasGlyphs)
        {
            string key = canvasGlyphs + ":" + text;
            if (TextSprites.TryGetValue(key, out Sprite cached)) return cached;

            Sprite sprite = DrawText(text, canvasGlyphs);
            TextSprites[key] = sprite;
            return sprite;
        }


        private static Sprite DrawText(string text, int canvasGlyphs)
        {
            int columns = canvasGlyphs * GlyphWidth + (canvasGlyphs - 1) * GlyphSpacing;
            int width = columns * GlyphScale;
            int height = GlyphHeight * GlyphScale;

            var pixels = new Color32[width * height];
            var white = new Color32(255, 255, 255, 255);

            // Centre the text in the fixed canvas, rounding left so a two-glyph value sits
            // fractionally left of centre rather than drifting right.
            int used = text.Length * GlyphWidth + (text.Length - 1) * GlyphSpacing;
            int startColumn = (columns - used) / 2;

            for (int i = 0; i < text.Length; i++)
            {
                if (!Glyphs.TryGetValue(text[i], out byte[] glyph)) continue;
                int columnOffset = startColumn + i * (GlyphWidth + GlyphSpacing);

                for (int row = 0; row < GlyphHeight; row++)
                {
                    for (int column = 0; column < GlyphWidth; column++)
                    {
                        // Bit 4 is the leftmost column; rows run top-down but textures are
                        // bottom-up, so the row index is flipped on the way out.
                        if ((glyph[row] & (1 << (GlyphWidth - 1 - column))) == 0) continue;

                        int x0 = (columnOffset + column) * GlyphScale;
                        int y0 = (GlyphHeight - 1 - row) * GlyphScale;

                        for (int y = 0; y < GlyphScale; y++)
                        {
                            int rowStart = (y0 + y) * width + x0;
                            for (int x = 0; x < GlyphScale; x++) pixels[rowStart + x] = white;
                        }
                    }
                }
            }

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                // Point filtering, so scaling the sprite keeps hard pixel edges.
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, false);

            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f));
        }
    }
}
