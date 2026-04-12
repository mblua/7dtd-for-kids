# Codex Verification Report: SurfaceCategory Coverage + Loading Screen Redundancy

**Date**: 2026-04-11
**Author**: dev-modder-codex (Track 2)
**Branch**: feature/fase1-xml-kid-friendly

---

## 1. SurfaceCategory Coverage Analysis

### What it does
`SurfaceCategory` determines the particle effect on hit. `organic` = blood impact particles. Our mod changes it to `stone` = spark particles.

### Current mod XPath
```xml
<set xpath="/entity_classes/entity_class[@name='zombieTemplateMale']/property[@name='SurfaceCategory']/@value">stone</set>
```

### Zombie Coverage: 100% COVERED

- `zombieTemplateMale` is the **only root zombie template** (confirmed via grep - no zombie entity_class without `extends` except this one)
- **No individual zombie overrides SurfaceCategory** (grep for `SurfaceCategory` + `zombie` returned zero matches)
- All zombies inherit through one of three chains:
  - `zombieTemplateMale` (direct: zombieJoe, zombieSpider, zombieBoe, zombieChuck, zombieRancher, zombieMoe, etc.)
  - `zombieTemplateShort extends zombieTemplateMale` (zombieArlene, zombieSteve, zombieScreamer, etc.)
  - `zombieTemplateSlimFemale extends zombieTemplateShort` (zombiePartyGirl)
- Special zombies also covered: zombieDemolition (extends zombieSoldier), zombieMutated (extends zombieFatCop), zombieFrostclaw (extends zombieChuck)
- All Feral/Radiated/Charged/Infernal variants inherit from their base and do NOT override SurfaceCategory

**Verdict: Single XPath on zombieTemplateMale covers all ~140+ zombie entity classes. No gaps.**

### Non-Zombie Entities with SurfaceCategory=organic: GAPS EXIST

| Line | Entity | SurfaceCategory | Covered? | Priority |
|------|--------|----------------|----------|----------|
| 206 | `playerMale` | organic | NO | Medium — player bleeds when taking damage |
| 523 | `zombieTemplateMale` | organic | YES | -- |
| ~4569 | `animalTemplateTimid` | organic | NO | Low — deer/stag/rabbit bleed on hit |
| ~4641 | `animalDoe` (has own) | organic | NO | Low — doe bleeds on hit |
| 4854 | `animalTemplateHostile` | organic | NO | Medium — wolf/bear bleed on hit |
| 6274 | `npcSurvivorTemplate` | organic | N/A — **commented out in vanilla XML** |
| 6480 | `npcTraderTemplate` | organic | NO | Very Low — traders rarely attacked |

### Gap Analysis

**Should we fix these gaps for Fase 1?**

- **Animals**: Kids will hunt deer/wolves/bears. Blood particles on hit are visible but less scary than zombie blood (no gore, just particle effects). Could add:
  ```xml
  <set xpath="/entity_classes/entity_class[@name='animalTemplateTimid']/property[@name='SurfaceCategory']/@value">stone</set>
  <set xpath="/entity_classes/entity_class[@name='animalDoe']/property[@name='SurfaceCategory']/@value">stone</set>
  <set xpath="/entity_classes/entity_class[@name='animalTemplateHostile']/property[@name='SurfaceCategory']/@value">stone</set>
  ```
- **Player**: Blood particles when player takes zombie hits. Less prominent than zombie blood. Could add:
  ```xml
  <set xpath="/entity_classes/entity_class[@name='playerMale']/property[@name='SurfaceCategory']/@value">stone</set>
  ```
  **Caveat**: Stone sparks on player when hit by a zombie may look odd. Consider `glass` or `cloth` instead.

- **Traders**: Very low priority. Traders are behind counters and hostile only when attacked.

**Recommendation**: Add animal templates to Fase 1. Player and trader can be Fase 2.

---

## 2. Loading Screen Redundancy Analysis

### Current implementation (two files)

**File 1: `Config/loadingscreen.xml` (Option A-bis)**
```xml
<remove xpath="/doc/backgrounds/tex"/>
```
Removes ALL background texture references from the loading screen system. No vanilla loading images will load.

**File 2: `Config/XUi_Menu/windows.xml` (Option C)**
```xml
<set xpath="/windows/window[@name='loadingScreen']/panel[@name='pnlBlack']/@depth">3</set>
```
Moves the black panel (depth 1 -> 3) above the loading image (depth 2). Black panel covers any scary images. Gradient bar (depth 4) and tips text (depth 6) remain visible above the black panel.

### Redundancy Assessment

This is **intentional defensive redundancy**, NOT waste. Comments in the code document this explicitly:

- Option A-bis prevents images from loading entirely (attack the data)
- Option C covers images even if Option A-bis fails (attack the rendering layer)
- Historical context: Option A (using `@modfolder:` paths) failed at runtime because the loading screen system uses Unity Addressables, not filesystem paths. Option A-bis was the workaround.

### Failure scenarios covered

| Scenario | A-bis alone | C alone | Both |
|----------|-------------|---------|------|
| Normal load | OK (no images) | OK (black covers) | OK |
| Game has fallback texture | FAIL (shows fallback) | OK (black covers) | OK |
| XUi_Menu load order issue | OK (no images) | FAIL (depth not applied yet) | OK |
| Future game update adds images differently | FAIL (new path) | OK (black covers) | OK |
| Unity Addressable caches old images | FAIL (cached) | OK (black covers) | OK |

**Verdict: KEEP BOTH. This is correct belt-and-suspenders design. Cost is 2 simple XPath operations (negligible). Protection against 3+ failure modes.**

### Minor note
The tips text that renders above the black panel is vanilla game tips. If any tips have scary text (e.g., "zombies will eat you"), that would need a separate `Localization.txt` override. This is outside current Fase 1 scope but worth noting for Fase 2.

---

## 3. Summary

| Area | Status | Action Needed |
|------|--------|---------------|
| Zombie SurfaceCategory | 100% covered | None |
| Animal SurfaceCategory | NOT covered | Add 3 XPaths (recommend for Fase 1) |
| Player SurfaceCategory | NOT covered | Defer to Fase 2 (needs UX decision on replacement particle) |
| Trader SurfaceCategory | NOT covered | Very low priority, defer |
| Loading screen Option A-bis | Working | Keep |
| Loading screen Option C | Working (safety net) | Keep |
| Loading screen redundancy | Intentional | No changes needed |
