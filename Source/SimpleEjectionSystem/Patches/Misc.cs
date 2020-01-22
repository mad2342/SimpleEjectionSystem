using System;
using BattleTech;
using BattleTech.UI;
using Harmony;

namespace SimpleEjectionSystem.Patches
{
    class Misc
    {
        // Checking if the new StatCollection entries find their ways to Sim and stay forever
        [HarmonyPatch(typeof(SGBarracksWidget), "OnPilotSelected")]
        public static class SGBarracksWidget_OnPilotSelected_Patch
        {
            public static void Postfix(SGBarracksWidget __instance, Pilot p)
            {
                try
                {
                    if (p.StatCollection.ContainsStatistic("StressLevel"))
                    {
                        Logger.Debug($"[SGBarracksWidget_OnPilotSelected_POSTFIX] ({p.Callsign}) StressLevel: {p.StatCollection.GetValue<int>("StressLevel")}");
                    }
                    else
                    {
                        Logger.Debug($"[SGBarracksWidget_OnPilotSelected_POSTFIX] ({p.Callsign}) Did not find StatCollection.StressLevel");
                    }

                    if (p.StatCollection.ContainsStatistic("LastEjectionChance"))
                    {
                        Logger.Debug($"[SGBarracksWidget_OnPilotSelected_POSTFIX] ({p.Callsign}) LastEjectionChance: {p.StatCollection.GetValue<int>("LastEjectionChance")}");
                    }
                    else
                    {
                        Logger.Debug($"[SGBarracksWidget_OnPilotSelected_POSTFIX] ({p.Callsign}) Did not find StatCollection.LastEjectionChance");
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            }
        }

        // Try to add a stress level pip bar, no joy
        /*
        [HarmonyPatch(typeof(CombatHUDPortrait), "Init")]
        public static class CombatHUDPortrait_Init_Patch
        {
            public static void Postfix(CombatHUDPortrait __instance, CombatGameState Combat, CombatHUD HUD, LayoutElement PortraitHolder)
            {
                try
                {
                    var stressDisplay = __instance.gameObject.AddComponent<CombatHUDLifeBarPips>();
                    stressDisplay.enabled = true;
                    //var healthDisplay = __instance.GetComponentInChildren<CombatHUDLifeBarPips>(true);
                    stressDisplay.Init(HUD);
                    stressDisplay.UpdateSummary(0, false);
                }
                catch (Exception e)
                {
                    Logger.LogError(e);
                }
            }
        }
        */
    }
}
