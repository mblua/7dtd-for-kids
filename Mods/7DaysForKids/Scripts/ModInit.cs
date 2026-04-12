using System;
using System.Reflection;
using HarmonyLib;

namespace SevenDaysForKids
{
    public class ModInit : IModApi
    {
        private const string TAG = "[7DaysForKids] ";

        public void InitMod(Mod _modInstance)
        {
            Log.Out(TAG + "Mod loading — v" + Assembly.GetExecutingAssembly().GetName().Version);

            // Visual patches are client-only — skip on dedicated server
            if (GameManager.IsDedicatedServer)
            {
                Log.Out(TAG + "Dedicated server detected — skipping visual patches");
                return;
            }

            // Verify target methods exist before patching
            var initMethod = typeof(EModelBase).GetMethod("Init",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (initMethod != null)
                Log.Out(TAG + "EModelBase.Init() found: " + initMethod);
            else
                Log.Error(TAG + "EModelBase.Init() NOT FOUND — color patch will not work!");

            try
            {
                var harmony = new Harmony("com.mblua.7daysforkids");
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                Log.Out(TAG + "All Harmony patches applied OK");
            }
            catch (Exception ex)
            {
                Log.Error(TAG + "PatchAll FAILED: " + ex);
            }
        }
    }
}
