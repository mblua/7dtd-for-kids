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
            if (entity == null)
                return;

            Transform modelT = __instance.GetModelTransform();
            if (modelT == null)
                return;

            EntityClass ec = EntityClass.list[entity.entityClass];
            if (ec == null)
                return;

            string className = ec.entityClassName;

            // Guard: EntityZombie (humanoid zombies) OR any entity whose
            // className starts with "animalZombie" (zombie dogs, bears,
            // vultures, boars). Zombie animals use EntityEnemyAnimal,
            // EntityZombieDog, EntityVulture — none extend EntityAnimal,
            // so we check by name instead of C# type.
            if (!(entity is EntityZombie) && !className.StartsWith("animalZombie"))
                return;

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
            script.EntityClassName = className;
            script.OwnerEntity = entity;
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
        public string EntityClassName;
        public Entity OwnerEntity;

        private bool _applied;
        private int _safetyFrames = 5;
        private GameObject _primitiveObj;

        public void ResetAndApply(Color color)
        {
            TargetColor = color;
            StopAllCoroutines();
            _applied = false;
            _safetyFrames = 5;
            _frameCount = 0;
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
            // Zombie animals (EntityEnemyAnimal/EntityZombieDog/EntityVulture — NOT EntityAnimal)
            { "animalZombieBear",     HexColor("8B4513") },  // Saddle brown
            { "animalZombieBoar",     HexColor("556B2F") },  // Dark olive green
            { "animalZombieDog",      HexColor("708090") },  // Slate gray
            { "animalZombieVulture",  HexColor("2F4F4F") },  // Dark slate
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

        private int _frameCount;

        void LateUpdate()
        {
            if (!_applied)
                return;

            _frameCount++;

            if (_safetyFrames > 0)
            {
                // Initial safety: re-apply every frame for 5 frames
                _safetyFrames--;
                ApplyColor();
            }
            else if (_frameCount % 30 == 0)
            {
                // Slow poll: re-disable renderers the game may re-enable
                // (ragdoll/death, LOD swap, model reload).
                // Every ~0.5s at 60fps — negligible performance cost.
                DisableOriginalRenderers();
            }
        }

        /// <summary>
        /// Disables all original renderers on the entity. Searches from
        /// transform.root to catch renderers that are siblings/parents of
        /// the model transform (some body parts live outside the model tree).
        /// </summary>
        private void DisableOriginalRenderers()
        {
            // Search from the Entity's transform to catch all renderers
            // belonging to THIS zombie (siblings of model transform).
            // DO NOT use transform.root — that's the scene root and would
            // disable renderers on ALL entities.
            Transform searchRoot = (OwnerEntity != null) ? OwnerEntity.transform : transform;
            Renderer[] renderers = searchRoot.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (renderer is ParticleSystemRenderer)
                    continue;
                if (_primitiveObj != null && renderer.gameObject == _primitiveObj)
                    continue;
                renderer.enabled = false;
            }
        }

        private void ApplyColor()
        {
            // 1. Disable all original renderers
            DisableOriginalRenderers();

            // 2. If primitive already exists, ensure it's active and return
            if (_primitiveObj != null)
            {
                _primitiveObj.SetActive(true);
                return;
            }

            // 3. Create capsule primitive — scale varies by entity type
            _primitiveObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            _primitiveObj.name = "7DFK_ColorCapsule";
            _primitiveObj.transform.SetParent(transform, false);

            if (EntityClassName != null && EntityClassName.Contains("Bear"))
            {
                _primitiveObj.transform.localPosition = Vector3.up * 0.7f;
                _primitiveObj.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
            }
            else if (EntityClassName != null && EntityClassName.Contains("Boar"))
            {
                _primitiveObj.transform.localPosition = Vector3.up * 0.5f;
                _primitiveObj.transform.localScale = new Vector3(0.7f, 0.6f, 0.9f);
            }
            else if (EntityClassName != null && EntityClassName.Contains("Dog"))
            {
                _primitiveObj.transform.localPosition = Vector3.up * 0.3f;
                _primitiveObj.transform.localScale = new Vector3(0.4f, 0.4f, 0.6f);
            }
            else if (EntityClassName != null && EntityClassName.Contains("Vulture"))
            {
                _primitiveObj.transform.localPosition = Vector3.up * 0.3f;
                _primitiveObj.transform.localScale = new Vector3(0.3f, 0.3f, 0.5f);
            }
            else
            {
                // Default zombie humanoid — wide enough to cover arms
                _primitiveObj.transform.localPosition = Vector3.up * 0.9f;
                _primitiveObj.transform.localScale = new Vector3(0.8f, 0.95f, 0.5f);
            }

            // 4. Remove collider — capsule is purely visual.
            // Zombie's original Entity.physicsCapsuleCollider handles all
            // hit detection and physics. Our collider was blocking arrows
            // without routing damage to the zombie.
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
