using System;
using HarmonyLib;
using UnityEngine;

namespace SevenDaysForKids
{
    /// <summary>
    /// One-shot full clear on death: wipes ALL blood overlays + disables
    /// ScreenEffects once. DisableScreenEffects is called only here (not
    /// every frame) to avoid interfering with other camera effects.
    /// </summary>
    [HarmonyPatch(typeof(EntityPlayerLocal), "OnEntityDeath")]
    public static class ClearBloodOnDeathPatch
    {
        private static bool _loggedFirst;

        static void Postfix(EntityPlayerLocal __instance)
        {
            if (__instance == null) return;

            // Full clear: direction indicators + blood drops + screen blood + dying effect
            if (__instance.overlayDirectionTime != null)
                Array.Clear(__instance.overlayDirectionTime, 0, __instance.overlayDirectionTime.Length);
            if (__instance.overlayBloodDropsPositions != null)
                for (int i = 0; i < __instance.overlayBloodDropsPositions.Length; i++)
                    __instance.overlayBloodDropsPositions[i] = Vector2.zero;
            if (__instance.screenBloodEffect != null)
                foreach (var go in __instance.screenBloodEffect)
                    if (go != null) go.SetActive(false);
            __instance.dyingEffectCur = 0f;
            __instance.dyingEffectLast = 0f;

            // Disable camera post-processing effects ONCE on death
            if (ScreenEffects.Instance != null)
                ScreenEffects.Instance.DisableScreenEffects();

            if (!_loggedFirst)
            {
                _loggedFirst = true;
                Log.Out("[7DaysForKids] OnEntityDeath: blood overlays + ScreenEffects cleared");
            }
        }
    }

    /// <summary>
    /// Per-frame clear while dead: only blood-specific fields.
    /// Does NOT clear overlayDirectionTime (respawn needs it intact).
    /// Does NOT call DisableScreenEffects (other effects may be active).
    /// </summary>
    [HarmonyPatch(typeof(EntityPlayerLocal), "OnDeathUpdate")]
    public static class ClearBloodDuringDeathPatch
    {
        static void Postfix(EntityPlayerLocal __instance)
        {
            if (__instance == null) return;

            if (__instance.overlayBloodDropsPositions != null)
                for (int i = 0; i < __instance.overlayBloodDropsPositions.Length; i++)
                    __instance.overlayBloodDropsPositions[i] = Vector2.zero;
            if (__instance.screenBloodEffect != null)
                foreach (var go in __instance.screenBloodEffect)
                    if (go != null) go.SetActive(false);
            __instance.dyingEffectCur = 0f;
            __instance.dyingEffectLast = 0f;
        }
    }

    /// <summary>
    /// LateUpdate — latest moment before render. Zeros dying vignette
    /// only when dead. Does NOT call DisableScreenEffects every frame
    /// (would interfere with radiation/blur/infection effects).
    /// </summary>
    [HarmonyPatch(typeof(EntityPlayerLocal), "LateUpdate")]
    public static class ClearDeathVignetteOnLateUpdatePatch
    {
        private static bool _loggedFirst;

        static void Postfix(EntityPlayerLocal __instance)
        {
            if (__instance == null || !__instance.IsDead())
                return;

            __instance.dyingEffectCur = 0f;
            __instance.dyingEffectLast = 0f;

            if (!_loggedFirst)
            {
                _loggedFirst = true;
                Log.Out("[7DaysForKids] LateUpdate: dying vignette cleared on death");
            }
        }
    }
}
