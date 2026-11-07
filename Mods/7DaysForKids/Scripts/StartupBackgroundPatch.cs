using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace SevenDaysForKids
{
    // ================================================================
    // Startup "Signing into services..." background
    //
    // Before the XUi main menu opens, the game shows the legacy NGUI
    // window EnumNGUIWindow.MainMenuBackground (big still image of a
    // zombie eating a person + game logo). It is NOT XUi, so XML
    // patches cannot reach it. XUiC_MainMenu hides it later.
    //
    // The NGUIWindowManager may be created BEFORE mods load, so we
    // apply the blackout from several points (idempotent):
    //   - ModInit.InitMod (FindObjectOfType)
    //   - NGUIWindowManager.ParseWindows / SetBackgroundScale / Show
    //
    // Only the large background textures are tinted black; the logo
    // (smaller widget) and the labels stay visible.
    // ================================================================

    public static class StartupBackground
    {
        private const string TAG = "[7DaysForKids] ";
        private static bool _loggedWidgets;

        public static void ApplyFromScene()
        {
            NGUIWindowManager mgr = Object.FindObjectOfType<NGUIWindowManager>();
            if (mgr == null)
            {
                Log.Out(TAG + "StartupBackground: NGUIWindowManager not in scene yet (patches will handle it)");
                return;
            }
            Apply(mgr);
        }

        public static void Apply(NGUIWindowManager mgr)
        {
            if (mgr == null)
                return;

            Transform win;
            try
            {
                win = mgr.GetWindow(EnumNGUIWindow.MainMenuBackground);
            }
            catch
            {
                return;
            }
            if (win == null)
                return;

            UITexture[] textures = win.GetComponentsInChildren<UITexture>(true);

            // Find the biggest widget: that is the background image.
            int maxArea = 0;
            foreach (UITexture t in textures)
            {
                int area = t.width * t.height;
                if (area > maxArea)
                    maxArea = area;
            }

            int blacked = 0;
            foreach (UITexture t in textures)
            {
                int area = t.width * t.height;
                bool isBackground = maxArea > 0 && area >= maxArea / 2;

                if (!_loggedWidgets)
                {
                    Log.Out(TAG + "StartupBackground widget: " + t.name +
                            " tex=" + (t.mainTexture != null ? t.mainTexture.name : "null") +
                            " size=" + t.width + "x" + t.height +
                            (isBackground ? " -> BLACK" : " -> keep"));
                }

                if (isBackground)
                {
                    // Tint works even if the texture is swapped or is a
                    // video render target.
                    t.color = Color.black;
                    blacked++;
                }
            }

            // Stop any video player living under this window.
            foreach (Behaviour b in win.GetComponentsInChildren<Behaviour>(true))
            {
                if (b != null && b.GetType().Name == "VideoPlayer" && b.enabled)
                {
                    b.enabled = false;
                    if (!_loggedWidgets)
                        Log.Out(TAG + "StartupBackground: disabled VideoPlayer on " + b.name);
                }
            }

            if (!_loggedWidgets)
            {
                _loggedWidgets = true;
                Log.Out(TAG + "StartupBackground: blacked out " + blacked + " of " + textures.Length + " textures");
            }
        }
    }

    [HarmonyPatch(typeof(NGUIWindowManager), "ParseWindows")]
    public static class StartupBackground_ParseWindowsPatch
    {
        static void Postfix(NGUIWindowManager __instance)
        {
            StartupBackground.Apply(__instance);
        }
    }

    [HarmonyPatch(typeof(NGUIWindowManager), "SetBackgroundScale")]
    public static class StartupBackground_SetBackgroundScalePatch
    {
        static void Postfix(NGUIWindowManager __instance)
        {
            StartupBackground.Apply(__instance);
        }
    }

    [HarmonyPatch(typeof(NGUIWindowManager), "Show")]
    public static class StartupBackground_ShowPatch
    {
        static void Postfix(NGUIWindowManager __instance, EnumNGUIWindow _eWindow, bool _bEnable)
        {
            if (_eWindow == EnumNGUIWindow.MainMenuBackground && _bEnable)
                StartupBackground.Apply(__instance);
        }
    }
}
