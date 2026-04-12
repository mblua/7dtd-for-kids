# Grinch Review — ZombieSolidColorPatch.cs Rewrite (commit 6286f97)

**Fecha:** 2026-04-11
**Reviewer:** 7dtd-grinch (adversarial)
**Files reviewed:** `ZombieSolidColorPatch.cs`, `ModInit.cs`, `LoadingScreenPatch.cs`, `7DaysForKids.csproj`
**Plan:** `_plans/plan-fix-zombie-colors.md`
**Architect review:** `_plans/architect-review-zombie-colors.md`

---

## Veredicto: FAIL — 1 bug, 4 non-blocking observations

---

## BUG #1 — Re-Init Kills Color Application (BLOCKER)

**File:** `ZombieSolidColorPatch.cs` L56-57 (Postfix) + L74-79 (MonoBehaviour state)

**What:** If `EModelBase.Init()` fires twice for the same zombie (same model transform), the color is never re-applied.

**Trace:**

First Init() call — works correctly:
1. `GetOrAddComponent<ZombieColorScript>()` → creates NEW component
2. `TargetColor` set
3. `Start()` fires → coroutine waits 3 frames → `ApplyColor()` → `_applied = true`
4. `LateUpdate` runs 5 safety frames → `_safetyFrames` reaches 0 → `enabled = false`

Second Init() call (same zombie, same modelTransform):
1. `GetOrAddComponent<ZombieColorScript>()` → returns EXISTING **disabled** component
2. `TargetColor` updated ← OK
3. `Start()` does **NOT** fire again (Unity lifecycle: Start is called once per MonoBehaviour instance)
4. Component remains `enabled = false`, `_safetyFrames = 0`, `_applied = true`
5. No coroutine, no LateUpdate → **new TargetColor is never applied**

**When this happens:**
- Game re-initializes a zombie model (LOD swap, SwitchModelAndView back to third person, entity pool recycling)
- Any code path that calls `EModelBase.Init()` again without destroying the model transform

**Why it matters:** The zombie silently reverts to default textures. No log, no error — the `if (!_loggedFirstHit)` guard at L59 already fired on the first init, so no diagnostic output on re-init either.

**Fix:** Add a public reset method to `ZombieColorScript` and call it from the Postfix:

```csharp
// In ZombieColorScript:
public void ResetAndApply(Color color)
{
    TargetColor = color;
    StopAllCoroutines();
    _applied = false;
    _safetyFrames = 5;
    enabled = true;
    StartCoroutine(ApplyColorDelayed());
}

// In Postfix, replace lines 56-57:
var script = modelT.gameObject.GetOrAddComponent<ZombieColorScript>();
script.ResetAndApply(finalColor);
```

This is idempotent: first call creates + starts, subsequent calls reset + restart.

---

## Checklist Answers (tech-lead questions 1-12)

### 1. Is the 3-frame coroutine delay sufficient?

**Likely yes, but empirical.** The SphereII production pattern uses similar timing. The 3 frames let the game run `createModel()`, `SetSkinTexture()`, and `AltMaterial` setup. Combined with the LateUpdate safety net (5 additional frames), the total window is ~8 frames from attachment. If any material initialization callback fires later than 8 frames, colors will be overwritten with no recovery. **Verdict: acceptable for first iteration. Verify in-game.**

### 2. Is the LateUpdate safety of 5 frames sufficient?

**Probably.** After the initial coroutine apply, 5 more re-applications cover frames 4-8 post-attachment. The self-disabling at L161 (`enabled = false`) is correct for performance. The risk is a late async callback (texture streaming, addressable load) overwriting materials past frame 8. **Verdict: acceptable. If testing shows color "flickers" (appears then disappears), bump `_safetyFrames` to 10-15.**

### 3. Is GetOrAddComponent idempotent if Init() is called twice?

**NO. This is BUG #1 above.** `GetOrAddComponent` returns the existing component, `TargetColor` is updated, but the component is disabled and Start() doesn't re-fire. The new color is never applied.

### 4. Is there a race condition between Start() coroutine and LateUpdate?

**No.** Unity execution order within a frame is: coroutine resume → ... → LateUpdate. On the frame the coroutine completes (frame N+3):
1. Coroutine: `ApplyColor()`, `_applied = true`
2. LateUpdate: sees `_applied == true`, decrements `_safetyFrames`, calls `ApplyColor()` again

This is a harmless double-apply on one frame. No race condition.

### 5. Does `renderer.materials = mats` (L200) persist changes?

**Yes.** `renderer.materials` getter returns per-instance copies. Modifying them and assigning back via `renderer.materials = mats` replaces the renderer's materials with the modified copies. This is standard Unity API. Confirmed correct.

### 6. Is the TexCache thread-safe?

**Yes, by design.** All Unity MonoBehaviour code (Start, LateUpdate, coroutines) runs on the main thread. `TexCache` is only accessed from `GetColorTexture()` → `ApplyColor()` → main thread only. No threading concern.

### 7. Does DontDestroyOnLoad on Texture2D prevent leaks or cause them?

**Prevents scene-transition breakage at negligible cost.** Without it, scene changes (main menu → game → main menu) could unload cached textures, causing pink/magenta fallback. Maximum textures: 36 bases × 5 variants = 180 entries × 64 bytes (4×4 RGBA32) = ~11.5 KB total. This is not a memory leak concern.

### 8. Is EnableKeyword("_EMISSION") without HasProperty safe?

**Yes.** `Material.EnableKeyword()` sets a shader keyword flag. If the shader doesn't declare `_EMISSION`, the flag is ignored — no error, no exception. This is documented Unity behavior. The subsequent `HasProperty("_EmissionColor")` check at L197 correctly guards the actual color assignment. No issue.

### 9. Are the 36 ColorMap colors kid-appropriate?

**PASS.** All 36 colors are standard, recognizable colors (blue, red, green, pink, gold, etc.). None reference gore, violence, or scary themes. Code comments like "Toxic green" are never visible to players — only the visual color (bright chartreuse) is shown. Internal entity names like `zombiePlagueSpitter` are overridden by Localization.txt kid-friendly names (verified in Fase 1 review: all 155 entities have localization entries).

### 10. Are the variant color transformations kid-appropriate?

**PASS.**
- Charged (+30% brightness): brighter, more cheerful
- Feral (saturation boost): more vivid
- Infernal (-30% brightness): darker but still clearly colored
- Radiated (green tint): greenish tint on base color

All produce normal, non-threatening color variations. No blood-red, no gore-like hues.

### 11. What happens on LOD swap / SwitchModelAndView?

**Two scenarios:**

**(a) Model transform is destroyed and recreated, Init() fires again:**
`GetOrAddComponent` creates a new component on the new transform. Start() fires, coroutine runs, color applied. **Works correctly.**

**(b) Model transform is swapped without Init() re-firing:**
Old transform (with ZombieColorScript) is destroyed. New model has no color script. Zombie reverts to original appearance. **Color lost, no recovery.** This is the known limitation the architect flagged (plan section 4: "Re-aplicacion de color fallback — only if testing shows AltMats/censor reset colors"). **Non-blocking for first iteration — verify during in-game testing.**

**(c) Init() fires again on same model transform:**
See BUG #1.

### 12. Is zombieSpider color Black (333333) too dark/scary?

**Non-blocking observation.** 333333 renders as a very dark gray — nearly a silhouette. On dark environments (night, caves), the zombie becomes almost invisible. On lit areas, it's a dark blob.

**Kid-appropriateness concern:** A barely-visible dark shape crawling on the ground could be more unsettling than a clearly visible colored shape. The "what you can't see clearly" factor can be scarier for young children than a bright purple or green blob.

**Recommendation:** Bump to `555555` (medium gray) or better, use a distinctive dark color like `4A3D6B` (dark purple) so the spider is always clearly visible and reads as a colored toy rather than a shadow creature. **Not a blocker, but strongly recommended for kid testing.**

---

## Additional Observations (non-blocking)

### OBS-A: Emission alpha scaling

**File:** L198 — `mat.SetColor("_EmissionColor", TargetColor * 0.3f);`

`Color * float` in Unity scales all four components (R, G, B, **A**). Alpha goes from 1.0 to 0.3. In Unity's Standard shader, emission alpha is typically ignored (HDR emission uses color magnitude), so this likely has no visual effect. But if any zombie uses a custom shader that reads emission alpha for intensity, emission would be dimmed to 30% of expected.

**Safer:** `new Color(TargetColor.r * 0.3f, TargetColor.g * 0.3f, TargetColor.b * 0.3f, 1f)`

### OBS-B: No diagnostic logging on re-init

**File:** L58-63 — the `_loggedFirstHit` guard ensures only the first zombie attachment is logged.

If BUG #1 occurs (re-init fails silently), there's no log output to indicate something went wrong. After fixing BUG #1, consider adding a log for re-init events at a lower frequency (e.g., log once per entity class name) to aid debugging.

### OBS-C: Held item renderers

**File:** L168 — `GetComponentsInChildren<Renderer>(true)` on the model transform.

If a zombie holds a weapon and the weapon's renderers are children of the model transform, they get colored too. The architect review flagged this (section 4: "Held item exclusion filter — only if testing shows weapons colored"). **Verify during in-game testing.**

### OBS-D: LoadingScreenPatch.cs correctness

Not in the original review scope but included in the commit. The implementation is clean — 3 fallback strategies for blackout, defensive null checks, shared DontDestroyOnLoad texture. **No issues found.** The architect's note about potential duplication with `Config/loadingscreen.xml` is valid — if only the Harmony patch works, the XML file is dead weight.

---

## Summary

| # | Item | Verdict |
|---|---|---|
| BUG #1 | Re-init kills color (GetOrAddComponent + disabled MonoBehaviour) | **BLOCKER** |
| Q1 | 3-frame coroutine delay | Acceptable, verify empirically |
| Q2 | 5-frame LateUpdate safety | Acceptable, bump if flicker seen |
| Q3 | Idempotent re-init | **NO — BUG #1** |
| Q4 | Race condition coroutine/LateUpdate | None |
| Q5 | renderer.materials persistence | Correct |
| Q6 | TexCache thread safety | Safe (main thread only) |
| Q7 | DontDestroyOnLoad leak | No leak, prevents breakage |
| Q8 | EnableKeyword without HasProperty | Safe |
| Q9 | Colors kid-appropriate | PASS |
| Q10 | Variants kid-appropriate | PASS |
| Q11 | LOD/model swap | Known limitation, verify in testing |
| Q12 | Spider black color | Recommend lighter/distinctive color |
| OBS-A | Emission alpha scaling | Minor, likely harmless |
| OBS-B | No re-init logging | Improve after BUG #1 fix |
| OBS-C | Held item coloring | Verify in testing |
| OBS-D | LoadingScreenPatch | Clean, check XML redundancy |

**Kid-appropriateness verdict:** All content reviewed — colors, names, variants, code comments. **PASS.** No inappropriate content found. One recommendation to reconsider zombieSpider's near-black color for young children (not a content issue, a "scary shadows" UX concern).
