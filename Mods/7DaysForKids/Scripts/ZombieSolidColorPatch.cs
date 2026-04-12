using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace SevenDaysForKids
{
    // ================================================================
    // Harmony Postfix — attaches ZombieColorScript MonoBehaviour only.
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

            var script = modelT.gameObject.GetOrAddComponent<ZombieColorScript>();
            script.ResetAndApply(finalColor);

            if (!_loggedFirstHit)
            {
                _loggedFirstHit = true;
                Log.Out("[7DaysForKids] Color capsule attached to: " + className + " color=" + finalColor);
            }
        }
    }

    // ================================================================
    // ZombieColorScript — disables original renderers and overlays a
    // solid-color capsule primitive. Material modification doesn't work
    // because the game's rendering pipeline overwrites materials.
    // This approach completely bypasses the game's material system.
    // ================================================================

    public class ZombieColorScript : MonoBehaviour
    {
        public Color TargetColor;

        private bool _applied;
        private int _safetyFrames = 5;
        private GameObject _primitiveObj;

        public void ResetAndApply(Color color)
        {
            TargetColor = color;
            StopAllCoroutines();
            _applied = false;
            _safetyFrames = 5;
            enabled = true;

            // Destroy old primitive if re-initializing
            if (_primitiveObj != null)
            {
                Object.Destroy(_primitiveObj);
                _primitiveObj = null;
            }

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

        void Start()
        {
            StartCoroutine(ApplyColorDelayed());
        }

        IEnumerator ApplyColorDelayed()
        {
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
                enabled = false;
            }
        }

        private void ApplyColor()
        {
            // 1. Disable all original renderers (skip particles and our own primitive)
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (renderer is ParticleSystemRenderer)
                    continue;
                // Skip our own capsule's renderer
                if (_primitiveObj != null && renderer.gameObject == _primitiveObj)
                    continue;
                renderer.enabled = false;
            }

            // 2. If primitive already exists, ensure it's active and return
            if (_primitiveObj != null)
            {
                _primitiveObj.SetActive(true);
                return;
            }

            // 3. Create capsule primitive
            _primitiveObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            _primitiveObj.name = "7DFK_ColorCapsule";
            _primitiveObj.transform.SetParent(transform, false);
            _primitiveObj.transform.localPosition = Vector3.up * 0.9f;
            _primitiveObj.transform.localScale = new Vector3(0.6f, 0.9f, 0.6f);

            // 4. Remove the collider — zombie already has its own
            Collider col = _primitiveObj.GetComponent<Collider>();
            if (col != null)
                Object.Destroy(col);

            // 5. Set up material with solid color
            Renderer capsuleRenderer = _primitiveObj.GetComponent<Renderer>();
            if (capsuleRenderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                if (mat.shader == null || mat.shader.name == "Hidden/InternalErrorShader")
                {
                    // Fallback if Standard shader not available
                    Shader fallback = Shader.Find("Sprites/Default");
                    if (fallback != null)
                        mat = new Material(fallback);
                }

                mat.color = TargetColor;

                if (mat.HasProperty("_Metallic"))
                    mat.SetFloat("_Metallic", 0f);
                if (mat.HasProperty("_Glossiness"))
                    mat.SetFloat("_Glossiness", 0f);

                mat.EnableKeyword("_EMISSION");
                if (mat.HasProperty("_EmissionColor"))
                    mat.SetColor("_EmissionColor", new Color(
                        TargetColor.r * 0.3f,
                        TargetColor.g * 0.3f,
                        TargetColor.b * 0.3f, 1f));

                capsuleRenderer.material = mat;
            }
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
