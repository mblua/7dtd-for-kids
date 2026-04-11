using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SevenDaysForKids
{
    public class ModInit : IModApi
    {
        private const string TAG = "[7DaysForKids] ";

        public void InitMod(Mod _modInstance)
        {
            Debug.LogWarning(TAG + "Mod loading — v" + Assembly.GetExecutingAssembly().GetName().Version);

            // Verify the target method exists before patching
            var postInit = typeof(EModelStandard).GetMethod("PostInit",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (postInit != null)
                Debug.LogWarning(TAG + "EModelStandard.PostInit() found: " + postInit);
            else
                Debug.LogError(TAG + "EModelStandard.PostInit() NOT FOUND — color patch will not work!");

            // Check OnOpen for loading screen patch
            var onOpen = typeof(XUiC_LoadingScreen).GetMethod("OnOpen",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (onOpen != null)
                Debug.LogWarning(TAG + "XUiC_LoadingScreen.OnOpen() found: " + onOpen);
            else
                Debug.LogError(TAG + "XUiC_LoadingScreen.OnOpen() NOT FOUND!");

            try
            {
                var harmony = new Harmony("com.mblua.7daysforkids");
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                Debug.LogWarning(TAG + "All Harmony patches applied OK");
            }
            catch (Exception ex)
            {
                Debug.LogError(TAG + "PatchAll FAILED: " + ex);
            }
        }
    }
}
