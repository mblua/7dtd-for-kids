# Plan v1 — 7 Days to Die "For Kids" Mod

**Fecha:** 2026-04-11
**Estado:** Scope aprobado por el usuario — en implementación (Fase 1)
**Game version:** 7 Days to Die (Steam, instalado en `C:\Program Files (x86)\Steam\steamapps\common\7 Days To Die`)

---

## Objetivo

Transformar la experiencia visual de 7 Days to Die para hacerla apta para chicos, sin alterar la mecánica de juego. El juego se juega exactamente igual (misma dificultad, mismos spawns, mismas recetas), pero los zombies se ven como figuras de colores sólidos, las pantallas de carga no muestran imágenes feas, y los nombres son amigables.

---

## Scope

### Lo que hacemos
1. **Zombies de color sólido** — Harmony patch (C#) que fuerza un color de material por tipo de zombie
2. **Nombres kid-friendly** — Localization.txt con el color asignado en el nombre
3. **Loading screens limpias** — Reemplazo de las 4 pantallas de carga por pantallas sólidas

### Lo que NO tocamos
- Stats de zombies (HP, daño, velocidad, AI)
- Spawning, gamestages, biomes
- Items, loot, recetas y crafting
- Bloques de gore
- Progresión y perks
- Sonidos
- Dificultad

---

## Estructura del Mod

```
Mods/7DaysForKids/
├── ModInfo.xml
├── Config/
│   ├── entityclasses.xml       # Remover partículas de muerte y dismemberment
│   ├── loadingscreen.xml       # Reemplazar texturas de loading screen
│   └── Localization.txt        # Nombres kid-friendly para zombies
└── Scripts/
    └── 7DaysForKids.dll        # Harmony patch: zombies de color sólido
```

### Distribución

| Paquete | Contenido | Instalación |
|---|---|---|
| **Server** | Config/ solamente (XMLs + Localization) | Se instala en el server, se pushea automático a clientes |
| **Client** | Config/ + Scripts/ (todo) | Se instala en cada PC. Necesario para los cambios visuales |
| **Single-player** | Client (incluye todo) | Se instala en la PC local |

---

## Cambio 1: Zombies de Color Sólido

### Investigación realizada

**138 zombie entities** encontradas en `entityclasses.xml`. Se organizan así:

- **27 tipos base** (zombieArlene, zombieBiker, zombieBoe, zombieBowler, zombieBurnt, zombieBusinessMan, zombieChuck, zombieDarlene, zombieDemolition, zombieFatCop, zombieFatHawaiian, zombieFemaleFat, zombieFrostclaw, zombieInmate, zombieJanitor, zombieJoe, zombieLab, zombieLumberjack, zombieMaleHazmat, zombieMarlene, zombieMoe, zombieMutated, zombieNurse, zombiePartyGirl, zombiePlagueSpitter, zombieRancher, zombieScreamer, zombieSkateboarder, zombieSoldier, zombieSpider, zombieSteve, zombieTomClark, zombieUtilityWorker, zombieWight, zombieYo)
- **5 variantes** por tipo: Base, Charged, Feral, Infernal, Radiated

**Propiedades visuales clave:**
- `Mesh` — referencia al prefab 3D (ej: `@:Entities/Zombies/Arlene/ZArlene.prefab`)
- `AltMats` — materiales alternativos para variación visual
- `ParticleOnDeath` — efecto de partículas al morir: `blood_death`
- `DismemberTag_L_*Gore` — prefabs de desmembramiento (gore de corte/golpe)

### Implementación

**Mecanismo:** Harmony patch en C# que intercepta la inicialización de entidades zombie y reemplaza el color del material por un color sólido.

```csharp
// Concepto — el código final lo escribe el dev-modder
[HarmonyPatch(typeof(EntityZombie), "PostInit")]  // Clase exacta a verificar
class ZombieSolidColorPatch
{
    static void Postfix(EntityZombie __instance)
    {
        Color color = GetColorForZombieType(__instance);
        var renderers = __instance.GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            foreach (var mat in r.materials)
            {
                mat.color = color;
                mat.mainTexture = null; // Fuerza color sólido sin textura
            }
        }
    }
}
```

### Asignación de colores

Hay ~27 tipos base. Propuesta de asignación (1 color por tipo):

| Zombie Base | Color propuesto | Hex | Nombre kid-friendly |
|---|---|---|---|
| zombieArlene | Azul | #4A90D9 | Arlene la Azul |
| zombieBiker | Rojo | #D94A4A | Biker el Rojo |
| zombieBoe | Verde | #4AD94A | Boe el Verde |
| zombieBowler | Naranja | #D9A04A | Bowler el Naranja |
| zombieBurnt | Gris oscuro | #666666 | Burnt el Gris |
| zombieBusinessMan | Violeta | #9B4AD9 | Business el Violeta |
| zombieChuck | Celeste | #4AD9D9 | Chuck el Celeste |
| zombieDarlene | Rosa | #D94A90 | Darlene la Rosa |
| zombieDemolition | Amarillo | #D9D94A | Demo el Amarillo |
| zombieFatCop | Azul marino | #2A4A8A | Cop el Azul Marino |
| zombieFatHawaiian | Turquesa | #40B5AD | Hawaiian el Turquesa |
| zombieFemaleFat | Lila | #B58AD9 | Lila |
| zombieFrostclaw | Blanco hielo | #D9EAF0 | Frost el Blanco |
| zombieInmate | Naranja oscuro | #CC6600 | Inmate el Naranja |
| zombieJanitor | Verde oliva | #6B8E23 | Janitor el Oliva |
| zombieJoe | Marrón | #8B5E3C | Joe el Marrón |
| zombieLab | Blanco | #FFFFFF | Lab el Blanco |
| zombieLumberjack | Verde bosque | #228B22 | Lumberjack el Verde |
| zombieMaleHazmat | Amarillo fluo | #CCFF00 | Hazmat el Fluo |
| zombieMarlene | Magenta | #D94AD9 | Marlene la Magenta |
| zombieMoe | Bordó | #8B0000 | Moe el Bordó |
| zombieMutated | Lima | #32CD32 | Mutated el Lima |
| zombieNurse | Rosa claro | #FFB6C1 | Nurse la Rosa |
| zombiePartyGirl | Fucsia | #FF00FF | Party la Fucsia |
| zombiePlagueSpitter | Verde tóxico | #7FFF00 | Verde Tóxico |
| zombieRancher | Beige | #C8AD7F | Rancher el Beige |
| zombieScreamer | Plateado | #C0C0C0 | Plateada |
| zombieSkateboarder | Cyan | #00CED1 | Skater el Cyan |
| zombieSoldier | Caqui | #BDB76B | Soldier el Caqui |
| zombieSpider | Negro | #333333 | Spider el Negro |
| zombieSteve | Dorado | #DAA520 | Steve el Dorado |
| zombieTomClark | Salmón | #FA8072 | Tom el Salmón |
| zombieUtilityWorker | Ámbar | #FFBF00 | Utility el Ámbar |
| zombieWight | Índigo | #4B0082 | Wight el Índigo |
| zombieYo | Coral | #FF7F50 | Yo el Coral |

**Variantes** (Charged, Feral, Infernal, Radiated) usarían el mismo color base pero con modificador de brillo/saturación para distinguirlas:
- Charged → color base más brillante
- Feral → color base más saturado
- Infernal → color base más oscuro
- Radiated → color base con tinte verdoso

### Compilación del C#

**Hallazgo importante:** 7DTD NO compila archivos .cs en runtime. Carga DLLs pre-compilados. Necesitamos:

1. **Harmony** ya viene incluido en el juego: `Mods/0_TFP_Harmony/0Harmony.dll`
2. **Assembly-CSharp.dll** (10.5 MB) en `7DaysToDie_Data/Managed/` — contiene todas las clases del juego
3. **Compilación:** Se necesita `dotnet build` o equivalente, referenciando esos DLLs
4. **Output:** `7DaysForKids.dll` que va en la carpeta del mod

**Setup de compilación (una sola vez):**
```
Scripts/
├── 7DaysForKids.csproj          # Proyecto C# con references a game DLLs
├── ZombieSolidColorPatch.cs     # Harmony patch
└── LoadingScreenPatch.cs        # Harmony patch (si aplica)
```

**Dependencia:** Necesitamos .NET SDK instalado para compilar. Verificar si ya está disponible en la máquina.

### XML complementario — Remover gore visual de entidades

Además del Harmony patch, vía XML removemos partículas y dismemberment:

```xml
<configs>
  <!-- Remover partículas de sangre al morir — solo blood_death (no tocar supply_crate_gib_Prefab) -->
  <set xpath="/entity_classes/entity_class/property[@name='ParticleOnDeath'][@value='blood_death']/@value"></set>

  <!-- Remover tags de dismemberment — TODOS los entity classes -->
  <!-- No alcanza con solo los templates: ~80% de zombies overridean DismemberTag con paths propios -->
  <!-- Hay ~323 DismemberTag lines en entityclasses.xml -->
  <remove xpath="/entity_classes/entity_class/property[starts-with(@name,'DismemberTag')]"/>

  <!-- DismemberMultiplier a 0 — wildcard (templates + overrides Feral/Radiated = ~83 entries) -->
  <set xpath="/entity_classes/entity_class/property[starts-with(@name,'DismemberMultiplier')]/@value">0</set>
</configs>
```

**Notas técnicas (dev-modder review):**
- ParticleOnDeath usa double-predicate `[@value='blood_death']` para no romper `supply_crate_gib_Prefab` en supplyPlane/supply crates
- DismemberMultiplier usa wildcard porque Feral/Radiated overridean el template con valores propios (~80 entries adicionales)
- DismemberTag wildcard es seguro: afecta zombies + animales hostiles (boar), NO afecta playerMale
- **Limitación Fase 1:** SurfaceCategory=organic causa blood splash on HIT (no solo muerte). Requiere Harmony patch (Fase 2)

---

## Cambio 2: Nombres Kid-Friendly

### Implementación

Archivo: `Config/Localization.txt`

```csv
Key,File,Type,UsedInMainMenu,NoTranslate,english,Context / Tooltip
zombieArlene,entityclasses,EntityClass,,,Arlene the Blue,
zombieArleneCharged,entityclasses,EntityClass,,,Arlene the Blue (Bright),
zombieArleneFeral,entityclasses,EntityClass,,,Arlene the Blue (Bold),
zombieArleneInfernal,entityclasses,EntityClass,,,Arlene the Blue (Dark),
zombieArleneRadiated,entityclasses,EntityClass,,,Arlene the Blue (Glow),
...
```

Patrón: `{Nombre} the {Color} ({Variante})` — variantes serían Bright/Bold/Dark/Glow para Charged/Feral/Infernal/Radiated.

Se generarían las ~138 entradas (27 base × 5 variantes + extras como zombieDemolition y zombieSteveCrawler).

---

## Cambio 3: Loading Screens Limpias

### Investigación realizada

**Configuración:** `Data/Config/loadingscreen.xml` define 4 texturas de fondo:
- `GUI/loading_screen_1`
- `GUI/loading_screen_2`
- `GUI/loading_screen_3`
- `GUI/loading_screen_4`

**Problema:** Las imágenes están dentro de un **Unity AssetBundle** (`Data/Addressables/.../ui.bundle`, 17 MB). NO son archivos PNG sueltos que podamos reemplazar.

### Opciones

**Opción A — Override XML + textura custom (preferida):**
1. Crear una textura sólida negra/blanca como asset del mod
2. Overridear `loadingscreen.xml` para apuntar a nuestra textura:
```xml
<configs>
  <!-- La estructura real es: <doc><backgrounds><tex file="..."/></backgrounds></doc> -->
  <set xpath="/doc/backgrounds/tex[1]/@file">@:Mods/7DaysForKids/Resources/blank_loading.png</set>
  <set xpath="/doc/backgrounds/tex[2]/@file">@:Mods/7DaysForKids/Resources/blank_loading.png</set>
  <set xpath="/doc/backgrounds/tex[3]/@file">@:Mods/7DaysForKids/Resources/blank_loading.png</set>
  <set xpath="/doc/backgrounds/tex[4]/@file">@:Mods/7DaysForKids/Resources/blank_loading.png</set>
</configs>
```
**Nota:** XPath verificado contra la estructura real de loadingscreen.xml (root es `<doc>`, texturas son `<tex file="..."/>` dentro de `<backgrounds>`).

**Opción B — Harmony patch:**
Interceptar el código que carga las texturas de loading screen y reemplazarlas por una textura generada en runtime (1x1 pixel negro). Más robusto pero más complejo.

**Opción C — UI override:**
Modificar el XUi (UI system de 7DTD) para que el panel de loading screen sea opaco negro, tapando la imagen de fondo. El loading screen UI ya tiene un `pnlBlack` — podríamos hacer que cubra toda la pantalla.

### Recomendación

Probar **Opción A** primero (es XML puro si funciona). Si el path de textura custom no funciona, ir a **Opción C** (UI override). **Opción B** como último recurso.

**Loading tips:** Las 51 tips definidas en `loadingscreen.xml` también deberían ser revisadas. Algunas pueden tener contenido inapropiado. Se overridean vía Localization.txt si es necesario.

---

## Dependencias y Requisitos

### Para compilar el Harmony patch (C#)

| Requisito | Estado | Acción |
|---|---|---|
| .NET SDK | Por verificar | Verificar con `dotnet --version`. Si no está, instalar .NET 6+ SDK |
| 0Harmony.dll | Disponible | En `Mods/0_TFP_Harmony/0Harmony.dll` |
| Assembly-CSharp.dll | Disponible | En `7DaysToDie_Data/Managed/Assembly-CSharp.dll` |
| UnityEngine.dll | Disponible | En `7DaysToDie_Data/Managed/UnityEngine*.dll` |

### Para testear

- El juego instalado y funcional
- Crear un mundo nuevo (o usar uno existente) para verificar los cambios
- EAC (Easy Anti-Cheat) deshabilitado (necesario para mods con C#)

---

## Orden de Implementación

### Fase 1 — XML puro (sin compilación)
1. `ModInfo.xml` — metadata del mod
2. `entityclasses.xml` — remover ParticleOnDeath y DismemberTag
3. `Localization.txt` — nombres kid-friendly para zombies
4. `loadingscreen.xml` — intentar override de texturas (Opción A)

**Testeable sin compilar nada.** El usuario puede instalar el mod con solo la carpeta Config/ y ver los cambios de nombres y partículas removidas.

### Fase 2 — Harmony patch (requiere .NET SDK)
1. Setup del proyecto C# con references a game DLLs
2. Implementar `ZombieSolidColorPatch.cs`
3. Implementar `LoadingScreenPatch.cs` (si Opción A no funciona)
4. Compilar → `7DaysForKids.dll`
5. Test completo en juego

### Fase 3 — Polish
1. Verificar que todos los loading tips sean kid-appropriate
2. Ajustar colores de zombies según feedback visual en juego
3. Documentar instrucciones de instalación

---

## Riesgos y Mitigaciones

| Riesgo | Impacto | Mitigación |
|---|---|---|
| XPath de loading screen no funciona | Loading screens quedan con imágenes feas | Fallback a Opción C (UI override) o Opción B (Harmony) |
| Color sólido del Harmony patch no cubre todo el modelo | Partes del zombie siguen con textura original | Iterar sobre todos los sub-renderers y child objects |
| .NET SDK no disponible | No se puede compilar el Harmony patch | Fase 1 funciona sin C#. Instalar SDK cuando sea necesario |

---

## Grinch Review — FAIL (parcialmente aplicable)

**Reviewed:** 2026-04-11
**Verdict original:** FAIL — 25 findings (6 CRITICAL, 10 HIGH, 6 MEDIUM, 3 LOW)

**Tech-lead triage:** La review se hizo contra una versión que todavía tenía Cambio 4 (Limpieza de Loot/Items Gore). Ese cambio fue **removido del scope** antes de la review. Muchos findings son sobre items, loot, recetas y gore blocks que **NO se tocan**.

**Findings APLICADOS al plan (in-scope):**
- C1: loadingscreen.xml XPath corregido → `/doc/backgrounds/tex[N]/@file` ✅
- C2: DismemberTag wildcard XPath → apunta a TODOS los entity classes ✅
- C5: ParticleOnDeath blankeado en TODOS los entity classes (player, animales incluidos) ✅
- H1: "Gorda la Lila" → "Lila" ✅
- H2: "Screamer la Plateada" → "Plateada" ✅
- H3: "Spitter el Verde" → "Verde Tóxico" ✅
- M1, M2, M6: Conteo de entidades y variantes necesita reconciliación — pendiente verificación del architect/dev
- M3: Loading tips audit — pendiente
- L1: Sonidos como limitación conocida — pendiente documentar

**Findings DESCARTADOS (fuera de scope — no hay Cambio 4):**
- C3 (foodRottingFlesh/farm plot), C4 (scope contradicts Cambio 4), C6 (foodCanSham recipe)
- H4-H10 (drugFortBites, KillerInstinct books/perks, zombie vomit ammo, cntBathTubGore)
- M4 (passive_effect tags), M5 (foodHoboStew recipe), L2 (corpseHanging)

La review original se mantiene abajo como referencia, pero los findings descartados NO requieren acción.

---

**Review original del grinch (referencia):**

The plan has good intentions and a solid structure, but contains multiple XPath bugs that would produce a non-functional mod, misses significant inappropriate content, and has an internal scope contradiction.

---

### CRITICAL — Will break the mod or is fundamentally wrong

**C1. loadingscreen.xml XPath is completely wrong**
- **What:** The plan writes `/loadingscreen/@texture_1` but the actual XML root is `<doc>`, not `<loadingscreen>`, and textures are `<tex file="..."/>` elements inside `<backgrounds>`, not attributes.
- **Why:** This XPath matches nothing. Loading screens will remain unchanged — kids see the scary vanilla images.
- **Fix:** Use `/doc/backgrounds/tex[1]/@file`, `/doc/backgrounds/tex[2]/@file`, etc. Or `<set xpath="/doc/backgrounds/tex/@file" replace_all="true">` if the mod framework supports it.

**C2. DismemberTag removal only targets templates — misses ~80% of entries**
- **What:** The plan removes DismemberTag from only 3 templates (`zombieTemplateMale`, `zombieTemplateShort`, `zombieTemplateSlimFemale`). But individual zombies (Arlene, Marlene, PartyGirl, Nurse, Joe, Steve, TomClark, BusinessMan, Burnt, Rancher, Chuck, Spider, Boe, etc.) ALL override DismemberTag with their own zombie-specific dismemberment paths. There are ~323 DismemberTag lines across the file.
- **Why:** Template removal has NO effect on entities that override the property. Dismemberment (arms, legs, heads coming off) will still occur for most zombies. A child slicing a zombie's head off with a machete sees the gore.
- **Fix:** Use a wildcard XPath: `<remove xpath="/entity_classes/entity_class/property[starts-with(@name,'DismemberTag')]"/>` to target ALL entity classes at once.

**C3. Removing foodRottingFlesh breaks the FARM PLOT recipe**
- **What:** `farmPlotBlockVariantHelper` (the farm plot — a core gameplay item) uses `foodRottingFlesh` as an ingredient (10 units). The plan says "Remove de items + loot + recipes" without specifying which recipes or what to replace with.
- **Why:** Farming becomes impossible. This is a core survival mechanic.
- **Fix:** Either (a) keep foodRottingFlesh but rename it to something kid-friendly (e.g., "Compost" / "Mulch"), or (b) replace the ingredient in ALL recipes that use it. foodRottingFlesh appears as an ingredient in: drugFortBites (×2 variants), farmPlotBlockVariantHelper, foodCanSham, foodHoboStew. Plus 3 passive_effect tags that reference it.

**C4. Scope section directly contradicts Cambio 4**
- **What:** The "Lo que NO tocamos" section lists: "Items, loot, recetas y crafting", "Bloques de gore", "Progresión y perks". But Cambio 4 removes gore blocks, removes/renames items, modifies loot tables, modifies recipes, and renames perks.
- **Why:** Anyone reading the scope will think these systems are untouched. The dev-modder or tech-lead may skip implementing Cambio 4 because the scope says not to touch these things.
- **Fix:** Update the scope to reflect ALL 4 changes, or split the "What we do" section into phases: "Visual changes" (Cambios 1-3) and "Content cleanup" (Cambio 4).

**C5. ParticleOnDeath only removed from zombie templates — player and animals still bleed**
- **What:** The plan removes `ParticleOnDeath` from `zombieTemplateMale` only. But `blood_death` particles also exist on: `playerMale` (line 210), animal template (stag, line 4570), another animal (doe, line 4642), and hostile animal template (line 4855).
- **Why:** When the child's character dies, they see a blood splatter. When they kill a deer or boar, blood splatter. Only zombie deaths are "clean."
- **Fix:** Remove ParticleOnDeath from ALL entity classes: `<set xpath="/entity_classes/entity_class/property[@name='ParticleOnDeath']/@value"></set>` (blanks ALL instances).

**C6. foodCanSham recipe uses BOTH foodRottingFlesh AND resourceBone**
- **What:** If both items are removed (plan removes foodRottingFlesh and renames resourceBone), the recipe `foodCanSham` loses 2 of 3 ingredients. Combined with the farm plot issue, this is a cascading recipe break.
- **Why:** Sham (canned food) becomes uncraftable with the wrong ingredients.
- **Fix:** Specify replacement ingredients for every recipe that references removed/renamed items. Don't just say "clean recipes" — enumerate them.

---

### HIGH — Content inappropriate for kids that the plan missed

**H1. "Gorda la Lila" is body-shaming**
- **What:** Proposed kid-friendly name for `zombieFemaleFat` is "Gorda la Lila" (Fat Woman the Lilac).
- **Why:** Body-shaming language is inappropriate for a children's mod. "Gorda" is a common insult in Spanish-speaking contexts.
- **Fix:** Use a neutral color-only name: "Lila" or "Lilac."

**H2. "Screamer la Plateada" is frightening**
- **What:** Keeps "Screamer" in the kid-friendly name. 
- **Why:** The word "Screamer" evokes horror. A 6-year-old reads "Screamer" and gets scared before the zombie even appears.
- **Fix:** Use "Silver" or "Plateada" only.

**H3. "Spitter el Verde" references disgusting behavior**
- **What:** Keeps "Spitter" (one who spits) in the kid-friendly name.
- **Why:** References vomiting/spitting — the zombiePlagueSpitter literally vomits on players.
- **Fix:** Use "Toxic Green" or "Verde Tóxico" — descriptive of color, not behavior.

**H4. drugFortBites not flagged**
- **What:** A drug item using medicalBloodBag + drinkJarBeer + foodRottingFlesh as ingredients. Not in the plan's removal/rename list.
- **Why:** "Fort Bites" is a drug. Its recipe contains 3 items the plan flags individually but the drug itself slipped through.
- **Fix:** Add to rename list (or remove if not essential to gameplay).

**H5. bookBarBrawling3KillerInstinct not flagged**
- **What:** A skill book named "Killer Instinct." 
- **Why:** Teaches children that "killer instinct" is a skill to develop.
- **Fix:** Add to rename list → "Quick Instinct" or "Fast Reflexes."

**H6. bookSpearHunter4KillMove not flagged**
- **What:** A skill book named with "Kill Move."
- **Why:** Explicit killing reference in a skill name.
- **Fix:** Add to rename list → "Power Move" or "Finishing Move."

**H7. perkBarBrawling3KillerInstinct not flagged**
- **What:** Perk named "Killer Instinct" with descriptions that reward "killing blows with stamina."
- **Why:** The full Localization entry says "Gain 5% damage with each kill, up to 15%." Teaches that killing = rewards.
- **Fix:** Add to rename list + override Localization descriptions.

**H8. perkSkullCrusher rank descriptions not addressed**
- **What:** The plan flags `perkSkullCrusher` for renaming but doesn't mention the Localization descriptions. Rank 5 says "If anyone pisses you off, you have the means to crush their skull." Contains profanity AND graphic violence.
- **Why:** Renaming the perk without overriding descriptions means the violent text still appears in skill tree tooltips.
- **Fix:** Override ALL rank descriptions for perkSkullCrusher in Localization.txt.

**H9. ammoProjectileZombieVomit not flagged**
- **What:** `ammoProjectileZombieVomit`, `ammoProjectileZombieVomitFeral`, `ammoProjectileZombieVomitRadiated` — zombie vomit projectiles.
- **Why:** "Vomit" is disgusting for children and appears in the item name (visible in some UIs).
- **Fix:** Rename via Localization or add to items to clean up.

**H10. cntBathTubGore block not flagged**
- **What:** A gore bathtub block (`cntBathTubGore`) with `blood_impact` destroy effect. Its child `cntBathTubsRandomLootHelper` extends it.
- **Why:** Gore bathtub appears in POIs. Child opens/destroys it, sees blood splash effect.
- **Fix:** Add to gore blocks removal list, OR override the DowngradeFX to remove `blood_impact`. Must handle the child block that extends it.

---

### MEDIUM — Technical issues that will cause problems

**M1. zombieWight has NO base variant**
- **What:** The entity list starts at `zombieWightFeral` — there is no `zombieWight` entity. But the plan's color table lists `zombieWight` and the Localization plan creates an entry for it.
- **Why:** Localization entry for `zombieWight` will be dead (no matching entity). The Harmony patch color map will have an unused entry. Not a crash, but indicates the plan author didn't verify entity names.
- **Fix:** Remove `zombieWight` base from color table and Localization. Start at `zombieWightFeral`.

**M2. Variant counts are incorrect — "5 variants per type" is wrong**
- **What:** The plan claims "27 tipos base" × "5 variantes (Base, Charged, Feral, Infernal, Radiated)." In reality:
  - 8+ zombies have Infernal COMMENTED OUT (Arlene, Marlene, PartyGirl, Nurse, Joe, Steve, TomClark, Screamer)
  - Some have Charged commented out (FatCop, Soldier, Moe)
  - zombieDemolition has only 1 entity (no variants found)
  - zombiePlagueSpitter/Frostclaw inherit from Rancher/Chuck variants, not their own
  - zombieSteveCrawler exists but is NOT listed anywhere in the plan
- **Why:** The "138 entities" count is wrong. The Localization file will have entries for entities that don't exist, and will be missing entries for entities that do (zombieSteveCrawler and its variants).
- **Fix:** Enumerate the ACTUAL entity list from entityclasses.xml. Generate Localization entries only for entities that actually exist (not commented out).

**M3. Loading tips count and audit are wrong**
- **What:** The plan says "51 tips" but loadingscreen.xml has 40 active tips. The plan says "should be reviewed" but doesn't actually review them.
- **Why:** At least 3 tips have inappropriate content: `loadingTipHarvestingCorpses` (harvesting dead bodies), `loadingTipBladedWeapons` (severing meat/bones, bleeding debuff), `loadingTipSmell` (killing animals).
- **Fix:** Actually audit all 40 tips. Override inappropriate tip texts via Localization.txt. List which specific tips need changes.

**M4. passive_effect tags reference foodRottingFlesh**
- **What:** Lines 1958, 2024, 2123 in recipes.xml have `tags="foodRottingFlesh,..."` in passive_effect elements for crafting perks.
- **Why:** If foodRottingFlesh is removed, these tag references become orphaned. May not crash but the perk effects referencing that ingredient will silently stop working.
- **Fix:** If renaming foodRottingFlesh (recommended — see C3), update the tags OR verify the game handles missing item tags gracefully.

**M5. foodHoboStew uses foodRottingFlesh**
- **What:** Another recipe using foodRottingFlesh as ingredient, not enumerated in the plan.
- **Why:** If foodRottingFlesh is removed, Hobo Stew becomes uncraftable.
- **Fix:** Replace ingredient or document the recipe change.

**M6. Plan's color table lists 35 zombies, not 27**
- **What:** The "27 tipos base" claim is in the text, but the actual color assignment table has 35 rows.
- **Why:** Creates confusion about the actual scope of work. Some of these 35 may not need entries (zombieWight base doesn't exist).
- **Fix:** Reconcile the count with actual entities in the game.

---

### LOW — Polish and documentation issues

**L1. Sounds exclusion should be documented as a known gap**
- **What:** The plan excludes sounds from scope. Zombie death sounds include blood-curdling screams, moaning, and horror sounds.
- **Why:** For a 6-year-old, horrifying sounds may be MORE frightening than visual changes. Not addressing this in v1 is a reasonable scope decision, but it should be explicitly documented as a known limitation, not silently ignored.
- **Fix:** Add a "Known Limitations" section acknowledging that zombie and death sounds remain vanilla.

**L2. corpseHanging downgrade blocks**
- **What:** corpseHangingLog blocks have `DowngradeBlock` properties (e.g., `noCorpseHangingLog1`). When removed, existing POIs with these blocks will show as air.
- **Why:** The plan acknowledges this is acceptable, which is fine. But the downgrade blocks themselves may cause a warning in the log.
- **Fix:** No action required, but document that POIs with hanging corpses will have empty spaces.

**L3. The Harmony patch color example references `EntityZombie` class**
- **What:** The C# code example patches `EntityZombie.PostInit()`. The actual class name in Assembly-CSharp.dll needs verification.
- **Why:** If the class is named differently (e.g., `EntityZombieV2` or namespace differs), the Harmony patch targets nothing.
- **Fix:** Dev-modder must verify the exact class and method name by inspecting Assembly-CSharp.dll with dnSpy or ILSpy before writing the patch.

---

### Summary of Required Actions Before Approval

1. **Fix** the loadingscreen.xml XPath (C1)
2. **Fix** DismemberTag removal to target ALL entity classes (C2)
3. **Decide** whether to rename or remove foodRottingFlesh, and enumerate ALL affected recipes (C3, C5, C6, M4, M5)
4. **Update** the scope section to match actual changes (C4)
5. **Add** ParticleOnDeath removal for player + animals (C5)
6. **Rename** offensive kid-friendly names: Gorda, Screamer, Spitter (H1-H3)
7. **Add** missing items to the flag list: drugFortBites, KillerInstinct books/perks, zombie vomit ammo, cntBathTubGore (H4-H10)
8. **Override** perkSkullCrusher rank descriptions in Localization (H8)
9. **Audit** all 40 loading tips and list which need Localization overrides (M3)
10. **Reconcile** the actual zombie entity list with the color table (M1, M2, M6)

The plan cannot be approved until at least items 1-6 are addressed. The concept is sound, but the implementation details have too many gaps that would result in a mod that either doesn't work or still exposes children to inappropriate content.

---

## Review del Architect — Fase 1

**Fecha:** 2026-04-11
**Reviewer:** 7dtd-architect
**Scope aprobado por Tech Lead:** Solo 3 cambios: (1) Nombres kid-friendly via Localization.txt, (2) Loading screens limpias, (3) Remoción de gore visual en entidades. Cambio 4 (items/loot/gore blocks) REMOVIDO del scope. Harmony patch (zombies de color) es Fase 2.
**Relación con Grinch Review:** Confirmo hallazgos C1, C2, M1, M2, M6. Los hallazgos C3/C4/C5/C6 y H4-H10 quedan fuera del scope actual. H1-H3 siguen relevantes para Localization.

---

### 1. entityclasses.xml — VERIFICADO CONTRA GAME XML

**Templates — CONFIRMADOS:**
- `zombieTemplateMale` (línea 498) — template raíz con `ParticleOnDeath` y `DismemberTag`
- `zombieTemplateShort` (línea 708) — extiende zombieTemplateMale, sin overrides de gore
- `zombieTemplateSlimFemale` (línea 715) — extiende zombieTemplateShort, sin overrides de gore

**ParticleOnDeath — CONFIRMADO:** Línea 522, valor `blood_death`. Se hereda a todos los zombies, ninguno lo sobreescribe.

**DismemberTag — BUG CONFIRMADO (Grinch C2):** 323 propiedades en dos niveles — 9 en template (genéricas `Prefabs/HeadGore`) + ~314 en zombies individuales (paths específicos como `Arlene/Dismemberment/Blade/Head`). Los overrides individuales hacen que remover solo de templates sea **insuficiente**.

**DismemberMultiplier — DESCUBRIMIENTO ADICIONAL:** Template tiene `DismemberMultiplierHead/Arms/Legs = 1`. Feral sobreescribe a `.7`, Radiated a `.4`. Con DismemberTag removidos son irrelevantes, pero set a 0 como belt-and-suspenders.

**XPath VERIFICADO:**

```xml
<configs>
  <!-- Limpiar partícula de sangre al morir en template zombie -->
  <set xpath="/entity_classes/entity_class[@name='zombieTemplateMale']/property[@name='ParticleOnDeath']/@value"></set>

  <!-- Remover TODOS los DismemberTag de TODAS las entity_classes (323 props) -->
  <remove xpath="/entity_classes/entity_class/property[starts-with(@name,'DismemberTag')]"/>

  <!-- Belt-and-suspenders: dismemberment multipliers a 0 -->
  <set xpath="/entity_classes/entity_class[@name='zombieTemplateMale']/property[@name='DismemberMultiplierHead']/@value">0</set>
  <set xpath="/entity_classes/entity_class[@name='zombieTemplateMale']/property[@name='DismemberMultiplierArms']/@value">0</set>
  <set xpath="/entity_classes/entity_class[@name='zombieTemplateMale']/property[@name='DismemberMultiplierLegs']/@value">0</set>
</configs>
```

---

### 2. loadingscreen.xml — XPATH CORREGIDO (Grinch C1)

**Estructura real:** Root es `<doc>`, backgrounds son `<tex file="..."/>` elements.

**Opción A (intentar primero):**
```xml
<configs>
  <set xpath="/doc/backgrounds/tex[@file='GUI/loading_screen_1']/@file">@modfolder:Resources/blank_loading</set>
  <set xpath="/doc/backgrounds/tex[@file='GUI/loading_screen_2']/@file">@modfolder:Resources/blank_loading</set>
  <set xpath="/doc/backgrounds/tex[@file='GUI/loading_screen_3']/@file">@modfolder:Resources/blank_loading</set>
  <set xpath="/doc/backgrounds/tex[@file='GUI/loading_screen_4']/@file">@modfolder:Resources/blank_loading</set>
</configs>
```

**Riesgo:** Texturas en Unity AssetBundle — `@modfolder:` podría no funcionar si carga antes de init de mods.

**Opción A-bis (fallback):** `<remove xpath="/doc/backgrounds/tex"/>`

**Opción C:** XUi override del panel de loading screen. Dev verificar en `Data/Config/XUi/`.

**Estrategia:** A → A-bis → C → Harmony (último recurso).

---

### 3. Localization.txt — VERIFICADO Y CORREGIDO

**Header — CORRECCIÓN:** `Context / Tooltip` → `Context / Alternate Text`

**Conteo — CORRECCIÓN MAYOR:** 27×5=~138 → **36 tipos, 155 entidades activas**

**Mapa completo verificado:**

| Tipo | Base | Feral | Rad | Chg | Inf | Total |
|---|:---:|:---:|:---:|:---:|:---:|:---:|
| Arlene | ✓ | ✓ | ✓ | ✓ | ✗ | 4 |
| Marlene | ✓ | ✓ | ✓ | ✓ | ✗ | 4 |
| PartyGirl | ✓ | ✓ | ✓ | ✓ | ✗ | 4 |
| Nurse | ✓ | ✓ | ✓ | ✓ | ✗ | 4 |
| Joe | ✓ | ✓ | ✓ | ✓ | ✗ | 4 |
| Steve | ✓ | ✓ | ✓ | ✓ | ✗ | 4 |
| TomClark | ✓ | ✓ | ✓ | ✓ | ✗ | 4 |
| BusinessMan | ✓ | ✓ | ✓ | ✓ | ✓ | 5 |
| Burnt | ✓ | ✓ | ✓ | ✓ | ✓ | 5 |
| Rancher | ✓ | ✓ | ✓ | ✓ | ✓ | 5 |
| PlagueSpitter | ✓ | ✓ | ✓ | ✓ | ✓ | 5 |
| Chuck | ✓ | ✓ | ✓ | ✓ | ✓ | 5 |
| Frostclaw | ✓ | ✓ | ✓ | ✓ | ✓ | 5 |
| Spider | ✓ | ✓ | ✓ | ✓ | ✓ | 5 |
| Boe | ✓ | ✓ | ✓ | ✓ | ✓ | 5 |
| MaleHazmat | ✓ | ✓ | ✓ | ✓ | ✓ | 5 |
| Janitor | ✓ | ✓ | ✓ | ✓ | ✓ | 5 |
| Inmate | ✓ | ✓ | ✓ | ✓ | ✓ | 5 |
| Darlene | ✓ | ✓ | ✓ | ✓ | ✓ | 5 |
| Yo | ✓ | ✓ | ✓ | ✓ | ✓ | 5 |
| UtilityWorker | ✓ | ✓ | ✓ | ✓ | ✓ | 5 |
| Skateboarder | ✓ | ✓ | ✓ | ✓ | ✓ | 5 |
| Mutated | ✓ | ✓ | ✓ | ✓ | ✓ | 5 |
| Moe | ✓ | ✓ | ✓ | ✗ | ✓ | 4 |
| Lab | ✓ | ✓ | ✓ | ✓ | ✗ | 4 |
| Biker | ✓ | ✓ | ✓ | ✗ | ✓ | 4 |
| Lumberjack | ✓ | ✓ | ✓ | ✗ | ✓ | 4 |
| FemaleFat | ✓ | ✓ | ✓ | ✗ | ✓ | 4 |
| FatHawaiian | ✓ | ✓ | ✓ | ✗ | ✓ | 4 |
| Bowler | ✓ | ✓ | ✓ | ✗ | ✓ | 4 |
| FatCop | ✓ | ✓ | ✓ | ✗ | ✓ | 4 |
| Soldier | ✓ | ✓ | ✓ | ✗ | ✓ | 4 |
| Screamer | ✓ | ✓ | ✓ | ✓ | ✗ | 4 |
| Wight | **✗** | ✓ | ✓ | ✓ | ✓ | 4 |
| Demolition | ✓ | ✗ | ✗ | ✗ | ✗ | 1 |
| SteveCrawler | ✓ | ✓ | ✗ | ✗ | ✗ | 2 |
| **TOTAL** | | | | | | **155** |

**Correcciones a nombres (Grinch H1-H3):**
- "Gorda la Lila" (FemaleFat) → **"Lily the Lilac"**
- "Screamer la Plateada" → **"Silver"**
- "Spitter el Verde" → **"Toxic the Green"**

**Tipos faltantes:** zombieWight base NO EXISTE (empieza en Feral). zombieSteveCrawler no estaba en tabla (agregar: Bronce #CD7F32, "Crawler the Bronze").

**Formato:**
```csv
Key,File,Type,UsedInMainMenu,NoTranslate,english,Context / Alternate Text
zombieArlene,entityclasses,EntityClass,,,Arlene the Blue,
zombieArleneFeral,entityclasses,EntityClass,,,Arlene the Blue (Bold),
zombieArleneRadiated,entityclasses,EntityClass,,,Arlene the Blue (Glow),
zombieArleneCharged,entityclasses,EntityClass,,,Arlene the Blue (Bright),
```
Variantes: Base=plain, Feral=(Bold), Radiated=(Glow), Charged=(Bright), Infernal=(Dark). **155 líneas + 1 header.**

---

### 4. ModInfo.xml

```xml
<?xml version="1.0" encoding="UTF-8" ?>
<xml>
    <ModInfo>
        <Name value="7DaysForKids" />
        <DisplayName value="7 Days For Kids" />
        <Description value="Kid-friendly mod: colorful zombie names, clean loading screens, no gore effects" />
        <Author value="mblua" />
        <Version value="1.0.0" />
        <Website value="https://github.com/mblua/7-days-to-die-for-kids" />
    </ModInfo>
</xml>
```

---

### 5. Orden de implementación Fase 1

1. **ModInfo.xml** — metadata
2. **entityclasses.xml** — XPaths verificados arriba
3. **loadingscreen.xml** — Opción A → A-bis → C
4. **Localization.txt** — 155 entradas kid-friendly

```
Mods/7DaysForKids/
├── ModInfo.xml
├── Config/
│   ├── entityclasses.xml
│   ├── loadingscreen.xml
│   └── Localization.txt
└── Resources/
    └── blank_loading.png    (1920×1080 negro, solo si Opción A funciona)
```

---

### 6. Resumen de correcciones al plan

| Item | Plan original | Corrección |
|---|---|---|
| DismemberTag XPath | Solo 3 templates (9 props) | Todas entity_classes (323 props) |
| DismemberMultiplier | No mencionado | Set a 0 (belt-and-suspenders) |
| loadingscreen XPath | `/loadingscreen/@texture_N` | `/doc/backgrounds/tex[@file='...']/@file` |
| Localization header | `Context / Tooltip` | `Context / Alternate Text` |
| Zombie count | 27×5 = ~138 | 36 tipos, 155 entidades |
| zombieWight base | Listado | NO EXISTE |
| zombieSteveCrawler | No mencionado | 2 variantes (Base+Feral) |
| Nombre FemaleFat | "Gorda la Lila" | "Lily the Lilac" |
| Nombre Screamer | "Screamer la Plateada" | "Silver" |
| Nombre PlagueSpitter | "Spitter el Verde" | "Toxic the Green" |
| Cambio 4 scope | Incluido | REMOVIDO |

---

## Dev-Modder Review — Fase 1

**Fecha:** 2026-04-11
**Reviewer:** 7dtd-dev-modder
**Método:** Verificación directa contra archivos del juego en `C:\Program Files (x86)\Steam\steamapps\common\7 Days To Die\Data\Config\`

---

### 1. entityclasses.xml — Verificación Completa

#### DismemberTag wildcard — ¿Afecta entidades no-zombie?

**SÍ, pero es deseable.** El wildcard `<remove xpath="/entity_classes/entity_class/property[starts-with(@name,'DismemberTag')]"/>` afecta:

| Entity | Tipo | DismemberTag entries | Impacto |
|---|---|---|---|
| `zombieTemplateMale` | Zombie template | 9 (genéricos) | Deseado |
| ~30 zombies individuales | Zombies | ~314 (paths específicos) | Deseado |
| `animalTemplateHostile` (línea 4857) | Animal template | 9 (genéricos) | **Colateral pero BUENO** — animales sin gore |
| `animalBoar` (línea 5723) | Animal | 1 (HeadGore override) | **Colateral pero BUENO** |
| `playerMale` | Player | **0 — NO tiene DismemberTag** | Sin impacto |

**Veredicto:** El wildcard es seguro. Los únicos no-zombie afectados son animales hostiles, y remover su gore es consistente con el objetivo kid-friendly. No hay riesgo de romper al player.

#### ParticleOnDeath — ¿Alcanza con blanquear solo el template zombie?

**NO alcanza para un mod kid-friendly completo.** Verificación de TODAS las instancias de `ParticleOnDeath` en el archivo:

| Línea | Entity | Valor | ¿Blanquear? |
|---|---|---|---|
| 210 | `playerMale` | `blood_death` | **SÍ** — el player muere con sangre |
| 522 | `zombieTemplateMale` | `blood_death` | **SÍ** — heredado por todos los zombies |
| 4570 | `animalTemplateTimid` (stag/doe) | `blood_death` | **SÍ** — animales tímidos mueren con sangre |
| 4642 | `animalDoe` (override) | `blood_death` | **SÍ** — doe tiene su propio override |
| 4855 | `animalTemplateHostile` (boar) | `blood_death` | **SÍ** — animales hostiles mueren con sangre |
| 6021 | `supplyPlane` | `supply_crate_gib_Prefab` | **NO** — es la animación de destrucción del crate, no sangre |
| 6042 | `sc_General` (supply crate) | `supply_crate_gib_Prefab` | **NO** — misma razón |
| 6276 | NPC survivor template | `blood_death` | **SÍ** — NPCs mueren con sangre |
| 6481 | `npcTraderTemplate` | `blood_death` | **SÍ** — traders mueren con sangre |

**PROBLEMA con el wildcard del grinch/architect:** `<set xpath="/entity_classes/entity_class/property[@name='ParticleOnDeath']/@value"></set>` blanquea TODAS las instancias, incluyendo `supply_crate_gib_Prefab`. Esto rompería la animación de destrucción de los supply crates (cajas de suministros aéreos).

**XPath RECOMENDADO — con filtro por valor:**
```xml
<set xpath="/entity_classes/entity_class/property[@name='ParticleOnDeath'][@value='blood_death']/@value"></set>
```
Esto blanquea SOLO las partículas de sangre, dejando intacto el `supply_crate_gib_Prefab`. El double-predicate `[@name='X'][@value='Y']` es XPath 1.0 estándar y soportado por 7DTD.

**Confirmación del architect:** Ningún zombie individual sobreescribe ParticleOnDeath — **VERIFICADO CORRECTO**. Solo `zombieTemplateMale` lo define, y todos los zombies heredan de ahí. Pero el template solo cubre zombies. Para player, animales y NPCs necesitamos el wildcard con filtro por valor.

#### DismemberMultiplier — ¿Necesario o redundante?

**Redundante pero recomendado como safety net.** Hallazgos:

- **83 entradas** de DismemberMultiplier en el archivo
- Template (línea 631-633): Head=1, Arms=1, Legs=1
- Feral variants: override a .7
- Radiated variants: override a .4
- `animalZombieBoar` (línea 5744): DismemberMultiplierHead = 0 (ya deshabilitado)

**PROBLEMA con el plan actual:** Solo se setea a 0 en el template zombie (3 entradas). Pero Feral y Radiated variants sobreescriben el template → el set en template NO afecta a Feral/Radiated.

**¿Importa?** Con los DismemberTags removidos, no hay prefabs de gore para spawnear → el multiplier es irrelevante independientemente de su valor. Sin embargo, para belt-and-suspenders completo:

```xml
<!-- Opción 1: Wildcard — setea TODOS los DismemberMultiplier a 0 (83 entradas) -->
<set xpath="/entity_classes/entity_class/property[starts-with(@name,'DismemberMultiplier')]/@value">0</set>
```

**Mi recomendación:** Incluirlo. Es una línea extra de XML, no tiene costo, y es un safety net contra edge cases donde el engine podría intentar dismemberment basándose solo en el multiplier sin verificar tags. Usar el wildcard, no solo el template.

**Nota adicional:** `playerMale` tiene `DismemberChance` como passive_effect (línea 257, valor 0.05 = 5% base chance). Esto controla con qué frecuencia los ataques del player causan dismemberment. Con DismemberTags removidos de los targets, esta chance se calcula pero no produce resultado visual. No requiere acción — dejar como está.

---

### 2. loadingscreen.xml — Path Scheme y Fallbacks

#### ¿`@modfolder:` es el path correcto?

**Incierto — necesita test en juego.** Análisis:

- `@:` — prefijo estándar para recursos de Unity (usado en `Mesh`, `Prefab` values). Resuelve a la raíz de recursos del juego.
- `@modfolder:` — prefijo documentado en la comunidad de modding para referenciar assets locales del mod. Resuelve al directorio raíz del mod.
- **El problema:** Los `<tex file="...">` en `loadingscreen.xml` referencian recursos del Unity Addressable system (`GUI/loading_screen_1` está dentro de un AssetBundle de 17 MB). El código de loading screen podría NO usar el mismo sistema de resolución de paths que `Mesh` o `Prefab`.

**Riesgo:** MEDIO. Si `@modfolder:` no funciona, la textura no carga y vemos la textura original (feas) o un placeholder.

#### Opción A-bis: ¿Remover los tex nodes?

**Viable pero con riesgo.** `<remove xpath="/doc/backgrounds/tex"/>` elimina las 4 texturas.

- **Mejor caso:** El loading screen muestra negro (sin textura de fondo), lo cual es exactamente lo que queremos.
- **Peor caso:** Null reference si el código asume al menos 1 textura → crash durante la carga. Es poco probable en código de calidad, pero posible.
- **Testeable rápidamente:** Si crashea al cargar un mundo, revertir inmediatamente.

#### Opción C: XUi override — VERIFICADO FACTIBLE

**Encontré la estructura exacta en `XUi_Menu/windows.xml` (líneas 2699-2716):**

```xml
<window name="loadingScreen" depth="200" ...>
  <!-- pnlBlack: panel negro a depth=1 (DETRÁS de la imagen) -->
  <panel name="pnlBlack" pos="-3000,3000" width="10000" height="10000" depth="1" ...>
    <sprite depth="0" name="blackback" sprite="menu_empty3px" color="0,0,0,255" .../>
  </panel>
  
  <!-- loading_image: textura de fondo a depth=2 (ENCIMA del panel negro) -->
  <texture depth="2" texture="{background_texture}" name="loading_image" .../>
  
  <!-- backgroundMain: gradiente a depth=4 -->
  <sprite depth="4" name="backgroundMain" .../>
</window>
```

**Override simple:** Cambiar el depth de `pnlBlack` a 3 → el panel negro se dibuja ENCIMA de la loading image (depth=2), tapando las imágenes feas:

```xml
<!-- Config/XUi_Menu/windows.xml -->
<configs>
  <set xpath="/windows/window[@name='loadingScreen']/panel[@name='pnlBlack']/@depth">3</set>
</configs>
```

**Ventajas:** No depende de texturas custom ni de `@modfolder:`. Usa el sistema existente de UI. El gradiente (depth=4) y los tips (depth=6) siguen visibles encima del panel negro.

**Estrategia recomendada:** A → A-bis → C. Las tres son XML puro. Si A falla, A-bis es inmediato. Si A-bis crashea, C es la red de seguridad más robusta.

---

### 3. Localization.txt — Formato Verificado

#### Header correcto

El header real del juego (línea 1 de `Data/Config/Localization.txt`):
```
Key,File,Type,UsedInMainMenu,NoTranslate,english,Context / Alternate Text,german,spanish,french,...
```

Para el mod, solo necesitamos las primeras 7 columnas:
```
Key,File,Type,UsedInMainMenu,NoTranslate,english,Context / Alternate Text
```

El architect ya corrigió esto (Context / Tooltip → Context / Alternate Text). **VERIFICADO CORRECTO.**

#### ¿El juego soporta Localization override por mod?

**SÍ.** 7DTD carga los `Localization.txt` de los mods y los merge sobre las entradas base. Las claves que coincidan con el juego base se sobreescriben. No se necesita ningún approach especial — colocar el archivo en `Config/Localization.txt` del mod es suficiente.

#### ¿Problemas con el formato CSV?

**Ninguno.** El formato propuesto es correcto:
```csv
Key,File,Type,UsedInMainMenu,NoTranslate,english,Context / Alternate Text
zombieArlene,entityclasses,EntityClass,,,Arlene the Blue,
```

- `File` = `entityclasses` (sin extensión .xml)
- `Type` = `EntityClass` (match con el tipo de la entidad)
- `UsedInMainMenu` y `NoTranslate` vacíos (no aplican)
- Las 155 entradas del architect están bien contadas y verificadas

---

### 4. Efectos Secundarios e Interacciones

#### ¿Crashes?

**NO.** 7DTD maneja gracefully:
- DismemberTag ausente → la entidad simplemente no tiene dismemberment
- ParticleOnDeath vacío → no se spawnea partícula al morir
- Algunos zombies ya tienen DismemberMultiplier=0 (zombieDemolition, zombieSteveCrawler, animalZombieBoar) → consistente con nuestro cambio

#### ¿Log spam?

**Improbable.** Remover DismemberTags elimina la referencia — el engine nunca busca los prefabs de gore, así que no hay warnings de "missing prefab". Diferente sería si dejáramos los tags con paths vacíos.

#### ¿Visual glitches?

**Muerte limpia:** Ragdoll physics funciona normalmente. El zombie colapsa sin sangre y sin partes que se separan. Exactamente lo que queremos.

**Gap conocido de Fase 1:** El `SurfaceCategory = "organic"` en entidades causa que los **impactos durante el combate** (no la muerte) reproduzcan partículas de sangre. Esto es un efecto separado del `ParticleOnDeath`. Remover ParticleOnDeath solo afecta la muerte, no los hits. Los hits con sangre necesitan el Harmony patch de Fase 2, o un cambio de SurfaceCategory (que afectaría los sonidos de impacto). **Documentar como limitación conocida de Fase 1.**

---

### 5. XPaths Finales Recomendados para entityclasses.xml

```xml
<configs>
  <!-- 1. Blanquear partícula de sangre al morir — SOLO blood_death, preserva supply_crate_gib -->
  <set xpath="/entity_classes/entity_class/property[@name='ParticleOnDeath'][@value='blood_death']/@value"></set>

  <!-- 2. Remover TODOS los DismemberTag de TODAS las entity_classes (323+ props) -->
  <remove xpath="/entity_classes/entity_class/property[starts-with(@name,'DismemberTag')]"/>

  <!-- 3. Belt-and-suspenders: TODOS los DismemberMultiplier a 0 (83 props, incluyendo Feral/Radiated overrides) -->
  <set xpath="/entity_classes/entity_class/property[starts-with(@name,'DismemberMultiplier')]/@value">0</set>
</configs>
```

**Diferencias vs plan actual del architect:**
1. ParticleOnDeath: filtro `[@value='blood_death']` para no romper supply crates
2. DismemberMultiplier: wildcard en vez de solo template (cubre Feral/Radiated overrides)

---

### 6. Resumen de Hallazgos

| Item | Plan Actual | Mi Hallazgo | Acción |
|---|---|---|---|
| DismemberTag wildcard scope | "TODOS los entity classes" | Incluye animales (template+boar) — NO playerMale | OK — es deseable |
| ParticleOnDeath wildcard | Blanquea TODO | Rompería `supply_crate_gib_Prefab` | **CORREGIR** — agregar filtro `[@value='blood_death']` |
| DismemberMultiplier belt-and-suspenders | Solo template (3 entries) | Feral/Radiated overridean (80 entries ignoradas) | **MEJORAR** — usar wildcard para los 83 entries |
| `@modfolder:` loading screen path | Asumido como válido | Incierto — loading screen puede usar Addressables | **RIESGO MEDIO** — tener A-bis y C listos |
| XUi loading screen override (Opción C) | "Dev verificar en XUi" | **Verificado factible** — cambiar pnlBlack depth de 1→3 | Listo para implementar como fallback |
| Localization formato | Header corregido por architect | Verificado contra juego real | OK |
| Hit particles durante combate | No mencionado | `SurfaceCategory="organic"` causa blood splash on hit | **Limitación Fase 1** — documentar |
| DismemberChance passive_effect | No mencionado | Player tiene 5% base chance (línea 257) | No requiere acción — sin DismemberTags es inocuo |
