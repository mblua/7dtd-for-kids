# Plan Fase 2 — Harmony Patch: Zombies de Color Sólido

**Fecha:** 2026-04-11
**Autor:** 7dtd-architect
**Estado:** Draft — pendiente revisión del tech-lead y grinch
**Prerequisito:** Fase 1 (XML puro) completada e instalada

---

## Objetivo

Interceptar la inicialización del modelo 3D de cada entidad zombie y reemplazar TODOS sus materiales por un color sólido. Cada tipo de zombie tiene un color asignado (ver tabla al final). Las variantes (Feral, Radiated, Charged, Infernal) usan el mismo color base con modificadores de brillo/saturación.

**Bonus:** Suprimir partículas de sangre al golpear zombies ("blood-on-hit").

---

## 1. Clases del Juego — Verificadas via Reflection

### Jerarquía de Entidades Zombie

```
MonoBehaviour (Unity)
  └── Entity
        ├── emodel: EModelBase          ← referencia al modelo 3D
        ├── entityClass: int            ← ID de la clase (mapea a entityclasses.xml)
        ├── ModelTransform: Transform   ← transform raíz del modelo
        ├── PostInit()                  ← virtual, llamado post-inicialización
        └── EntityAlive
              └── EntityEnemy
                    ├── PostInit()       ← override virtual
                    └── EntityHuman
                          └── EntityZombie       ← CLASE TARGET
                                └── EntityZombieCop  ← subclase especial
```

**CONFIRMADO:** `EntityZombie` existe y extiende `EntityHuman -> EntityEnemy -> EntityAlive -> Entity`.

**EntityZombie es mínima** — solo declara una propiedad `AimingGun` (Boolean). Toda la lógica está en los padres.

**Nota:** `EntityZombieDog` NO extiende `EntityZombie`. Extiende `EntityEnemyAnimal -> EntityEnemy`. Para colorear perros zombie también, habría que manejar EntityEnemyAnimal por separado (fuera del scope actual).

### Modelo 3D — EModelBase / EModelStandard

```
MonoBehaviour (Unity)
  └── EModelBase
        ├── entity: Entity              ← REFERENCIA AL ENTITY (campo público)
        ├── meshTransform: Transform    ← transform del mesh
        ├── modelTransform: Transform   ← transform del modelo
        ├── AltMaterial: Material       ← material alternativo
        ├── matPropBlock: MaterialPropertyBlock  ← para overrides eficientes
        ├── Init(World, Entity)         ← recibe el Entity, se almacena en this.entity
        ├── createModel(World, EntityClass) ← crea el modelo 3D desde prefab
        ├── PostInit()                  ← llamado DESPUÉS de crear el modelo
        └── EModelStandard              ← MODELO USADO POR ZOMBIES
              └── PostInit()            ← override, hook point recomendado
```

**CLAVE:** `EModelBase.entity` es un campo público que referencia al Entity dueño. Disponible desde Init() en adelante.

### Acceso al Nombre del Zombie

```csharp
// EntityClass.list es DictionarySave<int, EntityClass>
// EntityClass.entityClassName es String (ej: "zombieArlene", "zombieArleneFeral")
EntityClass ec = EntityClass.list[entity.entityClass];
string zombieName = ec.entityClassName; // "zombieArlene"
```

### Campos Relevantes de EntityClass (config XML)

| Campo | Tipo | Uso |
|---|---|---|
| `entityClassName` | String | Nombre del zombie (ej: "zombieArlene") |
| `AltMatNames` | String[] | Nombres de materiales alternativos |
| `MatSwap` | String[] | Material swap definitions del XML |
| `censorMode` | int | Modo de censura (0-3) |
| `censorType` | int | Tipo de censura |
| `meshPath` | String | Path al prefab del mesh |
| `DismemberMultiplierHead/Arms/Legs` | float | Multiplicadores de dismemberment |

---

## 2. Diseño del Harmony Patch — Colores Sólidos

### Hook Point: `EModelStandard.PostInit()` como Postfix

**¿Por qué este método?**
1. Se llama DESPUÉS de que `createModel()` haya instanciado el prefab → renderers disponibles
2. `this.entity` ya está poblado (se setea en `Init()` que corre antes)
3. Es específico de `EModelStandard` (el modelo de zombies) — no se dispara para players (`EModelSDCS`) ni supply crates (`EModelSupplyCrate`)
4. Es virtual, patcheable con Harmony sin problemas

**¿Por qué no Entity.PostInit()?**
- Se llama para TODAS las entidades (players, animales, vehículos, items)
- El modelo podría no estar completamente cargado aún (PostInit del Entity puede correr antes que PostInit del EModel)

**¿Por qué no EntityEnemy.PostInit()?**
- Más amplio que necesario (incluye animales enemigos, bandits)
- Mismo riesgo de timing con el modelo

### Pseudo-código del Patch

```csharp
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;

[HarmonyPatch(typeof(EModelStandard), "PostInit")]
public class ZombieSolidColorPatch
{
    // Mapa de nombre base del zombie → color RGB
    private static readonly Dictionary<string, Color> ZombieColorMap = new Dictionary<string, Color>
    {
        { "zombieArlene",         HexToColor("4A90D9") },  // Azul
        { "zombieMarlene",        HexToColor("D94AD9") },  // Magenta
        { "zombiePartyGirl",      HexToColor("FF00FF") },  // Fucsia
        { "zombieNurse",          HexToColor("FFB6C1") },  // Rosa claro
        { "zombieJoe",            HexToColor("8B5E3C") },  // Marrón
        { "zombieSteve",          HexToColor("DAA520") },  // Dorado
        // ... (35 entradas totales, ver tabla completa al final)
    };

    // Modificadores de color por variante
    private static readonly Dictionary<string, float> VariantBrightness = new Dictionary<string, float>
    {
        { "Feral",    1.0f },   // Sin cambio (ya son más agresivos visualmente)
        { "Radiated", 1.0f },   // Tinte verdoso se agrega por separado
        { "Charged",  1.3f },   // Más brillante
        { "Infernal", 0.7f },   // Más oscuro
    };

    static void Postfix(EModelStandard __instance)
    {
        // 1. Obtener el entity
        Entity entity = __instance.entity;
        if (entity == null) return;

        // 2. Solo procesar zombies
        if (!(entity is EntityZombie)) return;

        // 3. Obtener el nombre de clase del zombie
        EntityClass ec = EntityClass.list[entity.entityClass];
        if (ec == null) return;
        string className = ec.entityClassName; // ej: "zombieArleneFeral"

        // 4. Parsear nombre base y variante
        string baseName = GetBaseName(className);     // "zombieArlene"
        string variant = GetVariant(className);        // "Feral" o ""

        // 5. Buscar color base
        if (!ZombieColorMap.TryGetValue(baseName, out Color baseColor))
            return; // Zombie no mapeado, no modificar

        // 6. Aplicar modificador de variante
        Color finalColor = ApplyVariantModifier(baseColor, variant);

        // 7. Aplicar color sólido a todos los renderers
        ApplySolidColor(entity, finalColor);
    }

    private static void ApplySolidColor(Entity entity, Color color)
    {
        // Obtener todos los renderers del modelo (excluyendo particles)
        Renderer[] renderers = entity.GetComponentsInChildren<Renderer>(true);
        
        foreach (Renderer renderer in renderers)
        {
            // Excluir ParticleSystemRenderers (efectos de partículas)
            if (renderer is ParticleSystemRenderer) continue;
            
            // Excluir renderers de armas/items held
            // Los renderers del body están bajo el mesh transform
            // Los held items están en otro subárbol — verificar con testing
            
            foreach (Material mat in renderer.materials)
            {
                // Opción 1: Cambiar color y remover textura
                mat.color = color;
                if (mat.HasProperty("_MainTex"))
                    mat.SetTexture("_MainTex", Texture2D.whiteTexture);
                
                // Remover normal map, emission, etc. para look sólido
                if (mat.HasProperty("_BumpMap"))
                    mat.SetTexture("_BumpMap", null);
                if (mat.HasProperty("_MetallicGlossMap"))
                    mat.SetTexture("_MetallicGlossMap", null);
                if (mat.HasProperty("_OcclusionMap"))
                    mat.SetTexture("_OcclusionMap", null);
                
                // Ajustar propiedades para look mate/sólido
                if (mat.HasProperty("_Metallic"))
                    mat.SetFloat("_Metallic", 0f);
                if (mat.HasProperty("_Glossiness"))
                    mat.SetFloat("_Glossiness", 0.2f);
            }
        }
    }

    private static string GetBaseName(string fullName)
    {
        // Remover sufijos de variante
        string[] variants = { "Infernal", "Charged", "Radiated", "Feral" };
        foreach (var v in variants)
        {
            if (fullName.EndsWith(v))
                return fullName.Substring(0, fullName.Length - v.Length);
        }
        return fullName;
    }

    private static string GetVariant(string fullName)
    {
        string[] variants = { "Infernal", "Charged", "Radiated", "Feral" };
        foreach (var v in variants)
        {
            if (fullName.EndsWith(v)) return v;
        }
        return ""; // Base variant
    }

    private static Color ApplyVariantModifier(Color baseColor, string variant)
    {
        switch (variant)
        {
            case "Charged":
                // Más brillante
                return new Color(
                    Mathf.Min(baseColor.r * 1.3f, 1f),
                    Mathf.Min(baseColor.g * 1.3f, 1f),
                    Mathf.Min(baseColor.b * 1.3f, 1f));
            case "Feral":
                // Más saturado (aumentar canal dominante)
                float max = Mathf.Max(baseColor.r, baseColor.g, baseColor.b);
                if (max == 0) return baseColor;
                return new Color(
                    baseColor.r + (baseColor.r / max - 0.5f) * 0.3f,
                    baseColor.g + (baseColor.g / max - 0.5f) * 0.3f,
                    baseColor.b + (baseColor.b / max - 0.5f) * 0.3f);
            case "Infernal":
                // Más oscuro
                return new Color(baseColor.r * 0.7f, baseColor.g * 0.7f, baseColor.b * 0.7f);
            case "Radiated":
                // Tinte verdoso
                return new Color(
                    baseColor.r * 0.7f,
                    Mathf.Min(baseColor.g * 1.2f, 1f),
                    baseColor.b * 0.7f);
            default:
                return baseColor; // Base — sin modificación
        }
    }

    private static Color HexToColor(string hex)
    {
        byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
        byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
        byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
        return new Color(r / 255f, g / 255f, b / 255f);
    }
}
```

### Consideraciones Técnicas

#### a) Timing del Hook

`EModelStandard.PostInit()` se llama desde la cadena:
```
Entity.Init() → InitEModel() → EModelBase.Init() → createModel() → PostInit()
```

En `PostInit()`, el prefab ya fue instanciado y los `Renderer` components existen. **Confirmado** por la existencia de `meshTransform` y `modelTransform` en EModelBase, que se setean durante `createModel()`.

#### b) Materiales vs MaterialPropertyBlock

**Opción 1: Modificar `renderer.materials` directamente**
- **Pro:** Simple, cambios persistentes
- **Con:** Crea NUEVAS instancias de material por cada zombie (memory leak potencial)
- **Con:** Si el juego re-aplica materiales (ej: cuando alterna AltMats), nuestros cambios se pierden

**Opción 2: Usar `MaterialPropertyBlock` (campo `matPropBlock` en EModelBase)**
- **Pro:** Eficiente, no crea nuevos materiales
- **Pro:** Override non-destructivo
- **Con:** No puede cambiar texturas (solo propiedades numéricas/color)
- **Con:** No puede cambiar el shader

**Opción 3: Reemplazar shader con `Shader.Find("Unlit/Color")`**
- **Pro:** Garantiza color sólido perfecto sin texturas, lighting, etc.
- **Con:** Pierde toda la iluminación (zombies se ven "flat")
- **Con:** El shader "Unlit/Color" podría no existir en el build

**Recomendación:** Usar **Opción 1** (modificar materials) con las siguientes precauciones:
1. Cachear si el zombie ya fue coloreado para no re-procesar
2. Setear `_MainTex = Texture2D.whiteTexture` (textura blanca 1x1) + `_Color = nuestro color`
3. Remover normal maps y secondary textures
4. Mantener el Standard shader para que responda a iluminación (se vean 3D, no flat)

Si Opción 1 causa memory issues, migrar a Opción 3 (unlit shader).

#### c) Excluir Renderers que No Son del Body

`GetComponentsInChildren<Renderer>()` podría incluir:
- **ParticleSystemRenderer** → efectos de partículas → EXCLUIR (ya filtrado)
- **Held item renderers** → arma del zombie → verificar si están bajo el mismo transform tree
- **Accessory renderers** → gafas, sombreros → probablemente queremos colorear
- **Dismemberment gib renderers** → ya removidos por Fase 1

**Estrategia:** Excluir `ParticleSystemRenderer`. Incluir todo lo demás. Si en testing los held items se colorean, agregar un filtro por nombre de GameObject (ej: excluir nombres que contengan "weapon" o "hand").

#### d) Re-aplicación de Color

El juego podría re-setear materiales en ciertos eventos:
- Cambio de AltMats (variación visual aleatoria al spawn)
- Censorship toggle
- Damage state changes

**Mitigación:** Si se detecta que los colores se pierden, agregar un segundo hook en `EntityAlive.OnUpdateLive()` o similar para re-aplicar periódicamente. Esto es un fallback, no el approach principal.

#### e) EntityZombieCop (caso especial)

`EntityZombieCop` extiende `EntityZombie`. Nuestro check `entity is EntityZombie` lo captura automáticamente. No requiere tratamiento especial.

---

## 3. Bonus: Blood-on-Hit Particle Suppression

### Problema

La propiedad `SurfaceCategory="organic"` en el template zombie causa que al golpear un zombie se genere un efecto de partícula `blood_impact`. Esto fue identificado como limitación de Fase 1 (el XML no puede interceptar runtime particle spawning).

### Approach A — XML Override de SurfaceCategory (¡SIN Harmony!)

```xml
<!-- En entityclasses.xml del mod -->
<set xpath="/entity_classes/entity_class[@name='zombieTemplateMale']/property[@name='SurfaceCategory']/@value">metal</set>
```

**Efecto:** Cambiar SurfaceCategory de "organic" a "metal" hace que al golpear zombies:
- Se reproduzcan partículas de impacto metálico (chispas) en vez de sangre
- Se reproduzcan sonidos metálicos en vez de carne

**Pro:** Cero código C#. Puede ir en Fase 1.
**Con:** El sonido metálico al golpear un zombie podría sonar raro. Alternativas: probar "stone", "wood", o buscar una categoría sin partículas visibles.

**Recomendación:** Probar "metal" y "stone" en juego. Si suena aceptable, este approach es preferible al Harmony patch.

### Approach B — Harmony Hook en CollisionParticleController

```csharp
[HarmonyPatch(typeof(CollisionParticleController), "CheckCollision")]
public class SuppressBloodParticlePatch
{
    static bool Prefix(CollisionParticleController __instance, ref Int32 originEntityId)
    {
        // Si el entity asociado a este controller es un zombie,
        // y el particleEffectName contiene "blood", suprimir
        if (__instance.particleEffectName != null &&
            __instance.particleEffectName.Contains("blood"))
        {
            // Verificar si el entity es un zombie
            var entity = GameManager.Instance.World.GetEntity(__instance.entityId);
            if (entity is EntityZombie)
                return false; // Skip original method — no blood particle
        }
        return true; // Run original
    }
}
```

**Pro:** Granular — solo suprime blood en zombies, no en otros entities
**Con:** Requiere verificar que `GameManager.Instance.World.GetEntity()` funcione en este contexto

### Approach C — Harmony Hook en ParticleEffect.SpawnParticleEffect (nuclear)

```csharp
[HarmonyPatch(typeof(ParticleEffect), "SpawnParticleEffect")]
public class SuppressBloodParticleGlobalPatch
{
    static bool Prefix(ParticleEffect _pe, Int32 _entityThatCausedIt)
    {
        // Si el nombre del efecto contiene "blood", suprimir globalmente
        // Nota: esto suprime TODA la sangre, no solo de zombies
        if (_pe != null && _pe.GetType().GetField("...")... )
            return false;
        return true;
    }
}
```

**Con:** Suprime sangre EVERYWHERE (players, animales). Podría ser deseable para un mod kid-friendly, pero es un cambio amplio.

### Recomendación

1. **Primero probar Approach A (XML)** — cambiar `SurfaceCategory` a "stone" o "metal"
2. Si el sonido es inaceptable, ir a **Approach B** (CollisionParticleController hook)
3. Approach C solo si se quiere suprimir TODA la sangre del juego

---

## 4. Estructura del Proyecto C#

### Archivos

```
Mods/7DaysForKids/
├── ModInfo.xml                          (ya existe de Fase 1)
├── Config/                              (ya existe de Fase 1)
│   ├── entityclasses.xml
│   ├── loadingscreen.xml
│   └── Localization.txt
├── Scripts/                             (NUEVO en Fase 2)
│   ├── 7DaysForKids.csproj             (proyecto C#)
│   ├── ZombieSolidColorPatch.cs        (patch principal)
│   ├── BloodParticlePatch.cs           (patch de partículas, si Approach B)
│   └── ZombieColorMap.cs               (mapa de colores, separado para claridad)
└── 7DaysForKids.dll                     (output compilado — va en raíz del mod)
```

**Nota:** 7DTD carga DLLs directamente desde la carpeta raíz del mod (NO desde Scripts/). El directorio `Scripts/` es para el código fuente. El `.dll` compilado debe estar en `Mods/7DaysForKids/`.

### Proyecto .csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net48</TargetFramework>
    <AssemblyName>7DaysForKids</AssemblyName>
    <LangVersion>9.0</LangVersion>
    <OutputPath>../</OutputPath>
    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="Assembly-CSharp">
      <HintPath>C:\Program Files (x86)\Steam\steamapps\common\7 Days To Die\7DaysToDie_Data\Managed\Assembly-CSharp.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="0Harmony">
      <HintPath>C:\Program Files (x86)\Steam\steamapps\common\7 Days To Die\Mods\0_TFP_Harmony\0Harmony.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="UnityEngine">
      <HintPath>C:\Program Files (x86)\Steam\steamapps\common\7 Days To Die\7DaysToDie_Data\Managed\UnityEngine.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="UnityEngine.CoreModule">
      <HintPath>C:\Program Files (x86)\Steam\steamapps\common\7 Days To Die\7DaysToDie_Data\Managed\UnityEngine.CoreModule.dll</HintPath>
      <Private>false</Private>
    </Reference>
  </ItemGroup>
</Project>
```

**IMPORTANTE:**
- Target Framework: `net48` (Unity Mono runtime, NOT .NET 6+)
- `Private=false` para todas las references (no copiar DLLs de referencia al output)
- Output directamente a la raíz del mod (`../` desde Scripts/)

### Compilación

```bash
cd Mods/7DaysForKids/Scripts/
dotnet build -c Release
# Output: Mods/7DaysForKids/7DaysForKids.dll
```

### Mod Init (Harmony auto-patching)

7DTD + TFP Harmony automáticamente aplica `[HarmonyPatch]` attributes de DLLs en carpetas de mods. **No se necesita** código de inicialización manual ni `Harmony.PatchAll()` — el framework `0_TFP_Harmony` lo hace.

**Verificar:** El dev-modder debe confirmar si TFP_Harmony auto-patchea, o si necesita una clase `IModApi.InitMod()`:

```csharp
public class ModInit : IModApi
{
    public void InitMod(Mod _modInstance)
    {
        var harmony = new HarmonyLib.Harmony("com.7daysforkids.mod");
        harmony.PatchAll();
    }
}
```

---

## 5. Dependencias de Compilación

| Requisito | Path | Status |
|---|---|---|
| .NET SDK 6+ | `C:\Program Files\dotnet\dotnet.exe` | ✓ Instalado (v6.0.428) |
| Assembly-CSharp.dll | `7DaysToDie_Data\Managed\Assembly-CSharp.dll` | ✓ Disponible |
| 0Harmony.dll | `Mods\0_TFP_Harmony\0Harmony.dll` | ✓ Disponible |
| UnityEngine.dll | `7DaysToDie_Data\Managed\UnityEngine.dll` | ✓ Disponible |
| UnityEngine.CoreModule.dll | `7DaysToDie_Data\Managed\UnityEngine.CoreModule.dll` | ✓ Disponible |

**Target framework note:** El proyecto targeta `net48` para compatibilidad con Unity Mono. `dotnet build` con .NET 6 SDK puede compilar targets net48 si tiene el targeting pack, o usar `<TargetFramework>netstandard2.0</TargetFramework>` como alternativa compatible.

---

## 6. Tabla Completa de Colores (35 tipos base)

| Zombie Base | Color | Hex | Nombre kid-friendly |
|---|---|---|---|
| zombieArlene | Azul | #4A90D9 | Arlene the Blue |
| zombieBiker | Rojo | #D94A4A | Biker the Red |
| zombieBoe | Verde | #4AD94A | Boe the Green |
| zombieBowler | Naranja | #D9A04A | Bowler the Orange |
| zombieBurnt | Gris oscuro | #666666 | Burnt the Grey |
| zombieBusinessMan | Violeta | #9B4AD9 | Business the Violet |
| zombieChuck | Celeste | #4AD9D9 | Chuck the Cyan |
| zombieDarlene | Rosa | #D94A90 | Darlene the Pink |
| zombieDemolition | Amarillo | #D9D94A | Demo the Yellow |
| zombieFatCop | Azul marino | #2A4A8A | Cop the Navy |
| zombieFatHawaiian | Turquesa | #40B5AD | Hawaiian the Turquoise |
| zombieFemaleFat | Lila | #B58AD9 | Lily the Lilac |
| zombieFrostclaw | Blanco hielo | #D9EAF0 | Frost the White |
| zombieInmate | Naranja oscuro | #CC6600 | Inmate the Orange |
| zombieJanitor | Verde oliva | #6B8E23 | Janitor the Olive |
| zombieJoe | Marrón | #8B5E3C | Joe the Brown |
| zombieLab | Blanco | #FFFFFF | Lab the White |
| zombieLumberjack | Verde bosque | #228B22 | Lumberjack the Green |
| zombieMaleHazmat | Amarillo fluo | #CCFF00 | Hazmat the Neon |
| zombieMarlene | Magenta | #D94AD9 | Marlene the Magenta |
| zombieMoe | Bordó | #8B0000 | Moe the Maroon |
| zombieMutated | Lima | #32CD32 | Mutated the Lime |
| zombieNurse | Rosa claro | #FFB6C1 | Nurse the Rose |
| zombiePartyGirl | Fucsia | #FF00FF | Party the Fuchsia |
| zombiePlagueSpitter | Verde tóxico | #7FFF00 | Toxic the Green |
| zombieRancher | Beige | #C8AD7F | Rancher the Beige |
| zombieScreamer | Plateado | #C0C0C0 | Silver |
| zombieSkateboarder | Cyan | #00CED1 | Skater the Cyan |
| zombieSoldier | Caqui | #BDB76B | Soldier the Khaki |
| zombieSpider | Negro | #333333 | Spider the Black |
| zombieSteve | Dorado | #DAA520 | Steve the Gold |
| zombieSteveCrawler | Bronce | #CD7F32 | Crawler the Bronze |
| zombieTomClark | Salmón | #FA8072 | Tom the Salmon |
| zombieUtilityWorker | Ámbar | #FFBF00 | Utility the Amber |
| zombieWight | Índigo | #4B0082 | Wight the Indigo |
| zombieYo | Coral | #FF7F50 | Yo the Coral |

**Nota sobre zombieWight:** No tiene variante base en el juego (empieza en Feral). El color se aplica a WightFeral como si fuera base.

**Nota sobre colores similares:** Chuck (Celeste #4AD9D9) y Skateboarder (Cyan #00CED1) son muy similares. Considerar cambiar Skateboarder a Turquesa oscuro (#008B8B) para diferenciarlos. Lo mismo para Lumberjack (Verde bosque) vs Boe (Verde) vs PlagueSpitter (Verde tóxico) — 3 verdes que podrían confundirse.

---

## 7. Riesgos y Mitigaciones

| Riesgo | Impacto | Mitigación |
|---|---|---|
| Model load es async y renderers no existen en PostInit | Colores no se aplican | Agregar fallback: hook en `EntityAlive.OnUpdateLive()` con check one-time |
| Game re-aplica materiales post-init (AltMats, censor toggle) | Colores se pierden | Agregar re-aplicación periódica o hook en el método que cambia materiales |
| Held items (armas) del zombie también se colorean | Arma se ve del mismo color que el zombie | Filtrar por transform hierarchy — excluir hijos de joints de mano |
| `mat.color` no funciona con el shader del juego | Color no se aplica | Probar `mat.SetColor("_Color", color)` o reemplazar shader por `Unlit/Color` |
| .NET 48 targeting pack no disponible | No compila | Usar `netstandard2.0` como target alternativo |
| Texture2D.whiteTexture no disponible en el contexto | Textura no se asigna | Crear una textura 1x1 blanca en runtime: `new Texture2D(1,1); tex.SetPixel(0,0,Color.white); tex.Apply()` |
| TFP_Harmony no auto-patchea | Patches no se aplican | Implementar `IModApi.InitMod()` con `harmony.PatchAll()` |
| EntityZombieCop tiene comportamiento visual especial | Colores incompletos en cops | Verificar que el check `is EntityZombie` cubre EntityZombieCop (sí, por herencia) |

---

## 8. Orden de Implementación

1. **Setup proyecto C#** — crear `.csproj` con references
2. **ZombieColorMap.cs** — diccionario de colores con helper methods
3. **ZombieSolidColorPatch.cs** — hook principal en EModelStandard.PostInit()
4. **ModInit.cs** — IModApi init (si TFP no auto-patchea)
5. **Compilar** → verificar que genera DLL sin errores
6. **Test en juego** — spawn de cada tipo de zombie, verificar colores
7. **Ajustes** — filtrar held items si es necesario, ajustar colores
8. **BloodParticlePatch.cs** — si SurfaceCategory XML no es suficiente
9. **Test completo** — blood moon, wandering hordes, POIs con sleepers

---

## 9. Testing Checklist

- [ ] Compilación exitosa del DLL
- [ ] Mod se carga sin errores en el log del juego
- [ ] Spawnar `zombieArlene` con `F6` menu → verificar color azul sólido
- [ ] Spawnar `zombieArleneFeral` → azul con modificador Bold
- [ ] Spawnar `zombieArleneRadiated` → azul con tinte verdoso
- [ ] Verificar al menos 5 tipos distintos de zombie
- [ ] Verificar EntityZombieCop (Fat Cop) funciona
- [ ] Verificar que el player NO se colorea
- [ ] Verificar que animales NO se colorean
- [ ] Verificar que el NPC trader NO se colorea
- [ ] Verificar que held items del zombie no se colorean (o que es aceptable)
- [ ] Blood moon night → verificar que no crashea con muchos zombies
- [ ] Blood-on-hit: golpear zombie → verificar que no hay sangre (si se implementó)
- [ ] Performance: verificar FPS con 20+ zombies en pantalla

---

## 10. Verificaciones Pendientes para el Dev

El dev-modder DEBE verificar los siguientes puntos con ILSpy/dnSpy antes de implementar:

1. **¿EModelStandard.PostInit() llama base.PostInit()?** — Si no, el Postfix podría correr en un estado incompleto
2. **¿createModel() es sincrónico?** — Si usa async/coroutine, PostInit() podría correr antes de que el mesh esté listo
3. **¿Qué shaders usan los materiales de zombie?** — Inspeccionar los .mat files referenciados o leer `renderer.material.shader.name` en runtime
4. **¿TFP_Harmony auto-patchea?** — Verificar con un print simple en un [HarmonyPatch] Postfix
5. **¿`renderer.materials` crea copias?** — En Unity, acceder a `renderer.materials` (no `sharedMaterials`) crea instancias nuevas. Verificar si esto causa issues de memoria con muchos zombies
6. **¿El target framework `net48` compila con .NET SDK 6?** — Si no, probar `netstandard2.0`
