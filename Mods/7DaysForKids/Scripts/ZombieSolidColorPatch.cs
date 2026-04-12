using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace SevenDaysForKids
{
    /// <summary>
    /// Harmony Postfix on EModelBase.Init().
    /// After the model is initialized, replaces all materials with a solid
    /// color based on zombie type. Uses EModelBase (not EModelStandard)
    /// following the pattern from SphereII production mods.
    /// </summary>
    [HarmonyPatch(typeof(EModelBase), "Init")]
    public static class ZombieSolidColorPatch
    {
        // 36 base zombie types → solid color (keyed by game entityClassName)
        private static readonly Dictionary<string, Color> ColorMap = new Dictionary<string, Color>
        {
            { "zombieArlene",         HexColor("4A90D9") },  // Blue
            { "zombieBiker",          HexColor("D94A4A") },  // Red
            { "zombieBoe",            HexColor("4AD94A") },  // Emerald green
            { "zombieBowler",         HexColor("D9A04A") },  // Orange
            { "zombieBurnt",          HexColor("666666") },  // Gray
            { "zombieBusinessMan",    HexColor("9B4AD9") },  // Violet
            { "zombieChuck",          HexColor("4AD9D9") },  // Teal
            { "zombieDarlene",        HexColor("D94A90") },  // Rose
            { "zombieDemolition",     HexColor("D9D94A") },  // Yellow
            { "zombieFatCop",         HexColor("2A4A8A") },  // Navy
            { "zombieFatHawaiian",    HexColor("40B5AD") },  // Turquoise
            { "zombieFemaleFat",      HexColor("B58AD9") },  // Lilac
            { "zombieFrostclaw",      HexColor("D9EAF0") },  // Ice white
            { "zombieInmate",         HexColor("CC6600") },  // Tangerine
            { "zombieJanitor",        HexColor("6B8E23") },  // Olive
            { "zombieJoe",            HexColor("8B5E3C") },  // Brown
            { "zombieLab",            HexColor("FFFFFF") },  // White
            { "zombieLumberjack",     HexColor("228B22") },  // Forest green
            { "zombieMaleHazmat",     HexColor("CCFF00") },  // Neon yellow
            { "zombieMarlene",        HexColor("D94AD9") },  // Magenta
            { "zombieMoe",            HexColor("8B0000") },  // Maroon
            { "zombieMutated",        HexColor("32CD32") },  // Lime
            { "zombieNurse",          HexColor("FFB6C1") },  // Pink
            { "zombiePartyGirl",      HexColor("FF00FF") },  // Fuchsia
            { "zombiePlagueSpitter",  HexColor("7FFF00") },  // Toxic green
            { "zombieRancher",        HexColor("C8AD7F") },  // Beige
            { "zombieScreamer",       HexColor("C0C0C0") },  // Silver
            { "zombieSkateboarder",   HexColor("00CED1") },  // Cyan
            { "zombieSoldier",        HexColor("BDB76B") },  // Khaki
            { "zombieSpider",         HexColor("333333") },  // Black
            { "zombieSteve",          HexColor("DAA520") },  // Golden
            { "zombieSteveCrawler",   HexColor("CD7F32") },  // Bronze
            { "zombieTomClark",       HexColor("FA8072") },  // Salmon
            { "zombieUtilityWorker",  HexColor("FFBF00") },  // Amber
            { "zombieWight",          HexColor("4B0082") },  // Indigo
            { "zombieYo",             HexColor("FF7F50") },  // Coral
        };

        // Variant suffixes — checked longest-first to avoid "Feral" matching before "Infernal"
        private static readonly string[] Variants = { "Infernal", "Radiated", "Charged", "Feral" };

        private static bool _loggedFirstHit;

        static void Postfix(EModelBase __instance)
        {
            Entity entity = __instance.entity;
            if (entity == null || !(entity is EntityZombie))
                return;

            // Ensure model transform is ready
            if (__instance.GetModelTransform() == null)
                return;

            EntityClass ec = EntityClass.list[entity.entityClass];
            if (ec == null)
                return;

            string className = ec.entityClassName;

            if (!_loggedFirstHit)
            {
                _loggedFirstHit = true;
                Log.Out("[7DaysForKids] Color patch fired for: " + className);
            }

            // Parse base name and variant
            string baseName = className;
            string variant = "";
            foreach (string v in Variants)
            {
                if (className.EndsWith(v))
                {
                    baseName = className.Substring(0, className.Length - v.Length);
                    variant = v;
                    break;
                }
            }

            if (!ColorMap.TryGetValue(baseName, out Color baseColor))
                return;

            Color finalColor = ApplyVariant(baseColor, variant);
            ApplySolidColor(__instance, finalColor);
        }

        // Cache colored textures per base color to avoid creating duplicates
        private static readonly Dictionary<Color, Texture2D> _texCache = new Dictionary<Color, Texture2D>();

        private static Texture2D GetColorTexture(Color color)
        {
            if (_texCache.TryGetValue(color, out Texture2D cached))
                return cached;

            // Create a small solid-color texture (4x4 for filtering)
            Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[16];
            for (int i = 0; i < 16; i++)
                pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply();
            Object.DontDestroyOnLoad(tex);

            _texCache[color] = tex;
            return tex;
        }

        private static void ApplySolidColor(EModelBase model, Color color)
        {
            Texture2D colorTex = GetColorTexture(color);
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in renderers)
            {
                if (renderer is ParticleSystemRenderer)
                    continue;

                Material[] mats = renderer.materials;
                for (int i = 0; i < mats.Length; i++)
                {
                    Material mat = mats[i];

                    // Replace diffuse texture with our solid-color texture
                    if (mat.HasProperty("_MainTex"))
                        mat.SetTexture("_MainTex", colorTex);

                    // Set color property to match
                    if (mat.HasProperty("_Color"))
                        mat.SetColor("_Color", color);

                    // Nuke all secondary textures that could show through
                    string[] texturesToClear = {
                        "_BumpMap", "_MetallicGlossMap", "_OcclusionMap",
                        "_DetailAlbedoMap", "_DetailNormalMap", "_ParallaxMap",
                        "_EmissionMap", "_DetailMask", "_SpecGlossMap"
                    };
                    foreach (string texName in texturesToClear)
                    {
                        if (mat.HasProperty(texName))
                            mat.SetTexture(texName, null);
                    }

                    // Force matte flat finish
                    if (mat.HasProperty("_Metallic"))
                        mat.SetFloat("_Metallic", 0f);
                    if (mat.HasProperty("_Glossiness"))
                        mat.SetFloat("_Glossiness", 0f);

                    // Add emission to reinforce color visibility
                    mat.EnableKeyword("_EMISSION");
                    if (mat.HasProperty("_EmissionColor"))
                        mat.SetColor("_EmissionColor", color * 0.3f);
                }
                renderer.materials = mats;
            }
        }

        private static Color ApplyVariant(Color c, string variant)
        {
            switch (variant)
            {
                case "Charged":
                    return new Color(
                        Mathf.Min(c.r * 1.3f, 1f),
                        Mathf.Min(c.g * 1.3f, 1f),
                        Mathf.Min(c.b * 1.3f, 1f));
                case "Feral":
                    float max = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
                    if (max < 0.01f) return c;
                    return new Color(
                        Mathf.Clamp01(c.r + (c.r / max - 0.5f) * 0.3f),
                        Mathf.Clamp01(c.g + (c.g / max - 0.5f) * 0.3f),
                        Mathf.Clamp01(c.b + (c.b / max - 0.5f) * 0.3f));
                case "Infernal":
                    return new Color(c.r * 0.7f, c.g * 0.7f, c.b * 0.7f);
                case "Radiated":
                    return new Color(
                        c.r * 0.7f,
                        Mathf.Min(c.g * 1.2f, 1f),
                        c.b * 0.7f);
                default:
                    return c;
            }
        }

        private static Color HexColor(string hex)
        {
            byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            return new Color(r / 255f, g / 255f, b / 255f);
        }
    }
}
