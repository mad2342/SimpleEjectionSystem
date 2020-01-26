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
                        Logger.Info($"[SGBarracksWidget_OnPilotSelected_POSTFIX] ({p.Callsign}) StressLevel: {p.StatCollection.GetValue<int>("StressLevel")}");
                    }
                    else
                    {
                        Logger.Info($"[SGBarracksWidget_OnPilotSelected_POSTFIX] ({p.Callsign}) Did not find StatCollection.StressLevel");
                    }

                    if (p.StatCollection.ContainsStatistic("LastEjectionChance"))
                    {
                        Logger.Info($"[SGBarracksWidget_OnPilotSelected_POSTFIX] ({p.Callsign}) LastEjectionChance: {p.StatCollection.GetValue<int>("LastEjectionChance")}");
                    }
                    else
                    {
                        Logger.Info($"[SGBarracksWidget_OnPilotSelected_POSTFIX] ({p.Callsign}) Did not find StatCollection.LastEjectionChance");
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            }
        }
    }
}
