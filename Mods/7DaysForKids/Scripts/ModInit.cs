using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SevenDaysForKids
{
    /// <summary>
    /// Mod entry point. 7DTD auto-discovers IModApi implementations in mod DLLs.
    /// Harmony patches are applied here on game init.
    /// </summary>
    public class ModInit : IModApi
    {
        public void InitMod(Mod _modInstance)
        {
            Debug.Log("[7DaysForKids] Mod loading — v" + Assembly.GetExecutingAssembly().GetName().Version);

            var harmony = new Harmony("com.mblua.7daysforkids");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            Debug.Log("[7DaysForKids] Harmony patches applied");
        }
    }
}
