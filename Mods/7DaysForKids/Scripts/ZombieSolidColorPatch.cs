using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace SevenDaysForKids
{
    // ================================================================
    // Harmony Postfix — attaches ZombieColorScript MonoBehaviour only.
    // Zero material work here. The MonoBehaviour handles color via
    // coroutine delay (waits for game to finalize materials) + LateUpdate
    // safety re-application. Pattern from SphereII AddScriptToTransform.
    // ================================================================

    [HarmonyPatch(typeof(EModelBase), "Init")]
    public static class ZombieSolidColorPatch
    {
        private static readonly string[] Variants = { "Infernal", "Radiated", "Charged", "Feral" };
        private static bool _loggedFirstHit;

        static void Postfix(EModelBase __instance)
        {
            Entity entity = __instance.entity;
            if (entity == null || !(entity is EntityZombie))
                return;

            Transform modelT = __instance.GetModelTransform();
            if (modelT == null)
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

            if (!ZombieColorScript.ColorMap.TryGetValue(baseName, out Color baseColor))
                return;

            Color finalColor = ZombieColorScript.ApplyVariant(baseColor, variant);

            // Attach or re-initialize the MonoBehaviour.
            // ResetAndApply handles re-init if the script already exists but is disabled
            // (LOD swap, SwitchModelAndView, entity pool recycling).
            var script = modelT.gameObject.GetOrAddComponent<ZombieColorScript>();
            script.ResetAndApply(finalColor);

            if (!_loggedFirstHit)
            {
                _loggedFirstHit = true;
                Log.Out("[7DaysForKids] Color script attached to: " + className + " → " + finalColor);
            }
        }
    }

    // ================================================================
    // ZombieColorScript — MonoBehaviour attached to zombie model transform.
    // Uses coroutine delay (3 frames) to let the game finalize materials,
    // then applies solid color. LateUpdate re-applies for 5 frames as
    // safety net against late material overwrites, then self-disables.
    // ================================================================

    public class ZombieColorScript : MonoBehaviour
    {
        public Color TargetColor;

        private bool _applied;
        private int _safetyFrames = 5;

        /// <summary>
        /// Re-initializes the script for a new color application.
        /// Handles re-init when GetOrAddComponent returns an existing disabled script
        /// (LOD swap, SwitchModelAndView, entity pool recycling).
        /// </summary>
        public void ResetAndApply(Color color)
        {
            TargetColor = color;
            StopAllCoroutines();
            _applied = false;
            _safetyFrames = 5;
            enabled = true;
            StartCoroutine(ApplyColorDelayed());
        }

        // 36 base zombie types → solid color
        public static readonly Dictionary<string, Color> ColorMap = new Dictionary<string, Color>
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
            { "zombieSpider",         HexColor("555577") },  // Dark blue-gray
            { "zombieSteve",          HexColor("DAA520") },  // Golden
            { "zombieSteveCrawler",   HexColor("CD7F32") },  // Bronze
            { "zombieTomClark",       HexColor("FA8072") },  // Salmon
            { "zombieUtilityWorker",  HexColor("FFBF00") },  // Amber
            { "zombieWight",          HexColor("4B0082") },  // Indigo
            { "zombieYo",             HexColor("FF7F50") },  // Coral
        };

        // Texture cache — shared across all zombies, keyed by color
        private static readonly Dictionary<Color, Texture2D> TexCache = new Dictionary<Color, Texture2D>();

        private static readonly string[] TexturesToClear = {
            "_BumpMap", "_MetallicGlossMap", "_OcclusionMap",
            "_DetailAlbedoMap", "_DetailNormalMap", "_ParallaxMap",
            "_EmissionMap", "_DetailMask", "_SpecGlossMap"
        };

        void Start()
        {
            StartCoroutine(ApplyColorDelayed());
        }

        IEnumerator ApplyColorDelayed()
        {
            // Wait 3 frames for the game to finalize model materials,
            // AltMats, skin textures, censor mode, etc.
            yield return null;
            yield return null;
            yield return null;

            ApplyColor();
            _applied = true;
        }

        void LateUpdate()
        {
            if (!_applied)
                return;

            if (_safetyFrames > 0)
            {
                _safetyFrames--;
                ApplyColor();
            }
            else
            {
                // All safety re-applications done — stop for performance
                enabled = false;
            }
        }

        private void ApplyColor()
        {
            Texture2D colorTex = GetColorTexture(TargetColor);
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in renderers)
            {
                if (renderer is ParticleSystemRenderer)
                    continue;

                Material[] mats = renderer.materials;
                for (int i = 0; i < mats.Length; i++)
                {
                    Material mat = mats[i];

                    if (mat.HasProperty("_MainTex"))
                        mat.SetTexture("_MainTex", colorTex);
                    if (mat.HasProperty("_Color"))
                        mat.SetColor("_Color", TargetColor);

                    foreach (string texName in TexturesToClear)
                    {
                        if (mat.HasProperty(texName))
                            mat.SetTexture(texName, null);
                    }

                    if (mat.HasProperty("_Metallic"))
                        mat.SetFloat("_Metallic", 0f);
                    if (mat.HasProperty("_Glossiness"))
                        mat.SetFloat("_Glossiness", 0f);

                    mat.EnableKeyword("_EMISSION");
                    if (mat.HasProperty("_EmissionColor"))
                        mat.SetColor("_EmissionColor", new Color(TargetColor.r * 0.3f, TargetColor.g * 0.3f, TargetColor.b * 0.3f, 1f));
                }
                renderer.materials = mats;
            }
        }

        private static Texture2D GetColorTexture(Color color)
        {
            if (TexCache.TryGetValue(color, out Texture2D cached))
                return cached;

            Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[16];
            for (int i = 0; i < 16; i++)
                pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply();
            Object.DontDestroyOnLoad(tex);

            TexCache[color] = tex;
            return tex;
        }

        public static Color ApplyVariant(Color c, string variant)
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
