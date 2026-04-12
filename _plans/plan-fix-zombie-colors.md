# Plan: Fix Zombie Solid Color Patch

## Bug Summary
The Harmony Postfix on `EModelBase.Init()` fires correctly and detects zombies, but material changes don't persist visually. Zombies still show original textures.

## Root Cause (CONFIRMED)
The game re-applies materials AFTER `EModelBase.Init()` — via SetSkinTexture(), AltMaterial system, or during the first LateUpdate frames. Our one-shot material changes get overwritten.

**Evidence:**
- dev-modder: Found `SetSkinTexture()`, `AltMaterial` field, and `LateUpdate()` in EModelBase that can all re-apply materials post-Init
- dev-modder-codex: SphereII ReplaceMaterial uses `onSelfFirstSpawn` (fires AFTER full spawn), confirming Init() is too early for material work

## APPROVED Design (architect validated 2026-04-11)

### Architecture: MonoBehaviour + Delayed Coroutine (SphereII pattern)

**Hook:** Keep `EModelBase.Init()` Postfix — but ONLY to attach a MonoBehaviour. Zero material work in the Postfix.

**New class: `ZombieColorScript : MonoBehaviour`**
```
Attached to: GetModelTransform().gameObject via GetOrAddComponent<>()

Start():
  StartCoroutine(ApplyColorDelayed())

IEnumerator ApplyColorDelayed():
  yield return null  // frame 1
  yield return null  // frame 2
  yield return null  // frame 3 — game material setup complete
  ApplyColor()
  _applied = true

LateUpdate():
  if (!_applied) return
  if (_safetyFrames > 0):
    _safetyFrames--
    ApplyColor()  // re-apply in case game overwrites
  else:
    enabled = false  // stop LateUpdate (performance)

ApplyColor():
  // Same proven logic: replace _MainTex, set _Color, null secondary maps, enable _EMISSION
  // Uses renderer.materials (direct modification) — NOT MaterialPropertyBlock
  // MPB rejected: can't replace textures, can't null maps, can't toggle keywords
```

### Key Decisions (FINAL)

| Decision | Choice | Rationale |
|---|---|---|
| Hook point | `EModelBase.Init()` Postfix | Known to fire (confirmed by logs). Only attaches MonoBehaviour, no material work. |
| Material approach | Direct material modification | SphereII uses same approach. MPB can't handle our use case. |
| Persistence | Coroutine delay + LateUpdate safety (5 frames) | Coroutine waits for game to finalize materials. LateUpdate catches late overwrites. Self-disables for perf. |
| Script attachment | `GetOrAddComponent<ZombieColorScript>()` | 7DTD extension method, idempotent. SphereII proven pattern. |

### Files to Modify
- `Mods/7DaysForKids/Scripts/ZombieSolidColorPatch.cs` — rewrite: split into Harmony Postfix (attach only) + ZombieColorScript MonoBehaviour

### Architect Observations (non-blocker)
- Material memory pressure with 50+ zombies during Blood Moon — monitor FPS
- Verify SurfaceCategory inheritance covers all zombie types
- Check if Config/loadingscreen.xml is redundant with LoadingScreenPatch.cs

## Pre-Ship Checklist (from architect review)

### Blockers
- [ ] Compilation: `dotnet build -c Release` clean
- [ ] Mod loads: "[7DaysForKids] All Harmony patches applied OK" in logs
- [ ] zombieArlene spawns as solid blue (#4A90D9)
- [ ] zombieArleneFeral: blue with higher saturation
- [ ] zombieArleneRadiated: blue with green tint
- [ ] Player NOT colored (first/third person)
- [ ] NPC Trader NOT colored
- [ ] SurfaceCategory: sparks on hit, not blood

### Important (pre-release, not pre-merge)
- [ ] 5+ distinct zombie types verified
- [ ] Blood Moon complete without crash, stable FPS
- [ ] Loading screens are black
