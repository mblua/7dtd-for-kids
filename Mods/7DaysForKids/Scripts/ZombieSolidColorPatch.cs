using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace SevenDaysForKids
{
    /// <summary>
    /// Harmony Postfix on EModelStandard.PostInit().
    /// After the zombie model is created and renderers exist,
    /// replaces all materials with a solid color based on zombie type.
    /// </summary>
    [HarmonyPatch(typeof(EModelStandard), "PostInit")]
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

        static void Postfix(EModelStandard __instance)
        {
            Entity entity = __instance.entity;
            if (entity == null || !(entity is EntityZombie))
                return;

            EntityClass ec = EntityClass.list[entity.entityClass];
            if (ec == null)
                return;

            string className = ec.entityClassName;

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
            ApplySolidColor(entity, finalColor);
        }

        private static void ApplySolidColor(Entity entity, Color color)
        {
            Renderer[] renderers = entity.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in renderers)
            {
                if (renderer is ParticleSystemRenderer)
                    continue;

                // Access .materials once — Unity creates per-instance copies on first access.
                // This is intentional: each zombie needs its own color.
                Material[] mats = renderer.materials;
                foreach (Material mat in mats)
                {
                    // Set solid color
                    if (mat.HasProperty("_Color"))
                        mat.SetColor("_Color", color);

                    // Replace diffuse texture with white so color shows through
                    if (mat.HasProperty("_MainTex"))
                        mat.SetTexture("_MainTex", Texture2D.whiteTexture);

                    // Remove detail textures for clean solid look
                    if (mat.HasProperty("_BumpMap"))
                        mat.SetTexture("_BumpMap", null);
                    if (mat.HasProperty("_MetallicGlossMap"))
                        mat.SetTexture("_MetallicGlossMap", null);
                    if (mat.HasProperty("_OcclusionMap"))
                        mat.SetTexture("_OcclusionMap", null);

                    // Matte finish — no metallic sheen, low gloss
                    if (mat.HasProperty("_Metallic"))
                        mat.SetFloat("_Metallic", 0f);
                    if (mat.HasProperty("_Glossiness"))
                        mat.SetFloat("_Glossiness", 0.2f);
                }
                // Write back the modified materials array
                renderer.materials = mats;
            }
        }

        private static Color ApplyVariant(Color c, string variant)
        {
            switch (variant)
            {
                case "Charged":  // Brighter (+30%)
                    return new Color(
                        Mathf.Min(c.r * 1.3f, 1f),
                        Mathf.Min(c.g * 1.3f, 1f),
                        Mathf.Min(c.b * 1.3f, 1f));
                case "Feral":  // More saturated
                    float max = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
                    if (max < 0.01f) return c;
                    return new Color(
                        Mathf.Clamp01(c.r + (c.r / max - 0.5f) * 0.3f),
                        Mathf.Clamp01(c.g + (c.g / max - 0.5f) * 0.3f),
                        Mathf.Clamp01(c.b + (c.b / max - 0.5f) * 0.3f));
                case "Infernal":  // Darker (-30%)
                    return new Color(c.r * 0.7f, c.g * 0.7f, c.b * 0.7f);
                case "Radiated":  // Green tint
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
