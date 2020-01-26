using System;
using BattleTech;
using UnityEngine;

namespace SimpleEjectionSystem.Extensions
{
    public static class PilotDefExtensions
    {
        public static float GetRelativeCombatExperience(this PilotDef pilotDef)
        {
            //int rank = PilotDef.GetPilotRank(pilotDef);

            const float min = 0.1f;
            const float max = 1.1f;
            float result = 0;

            Logger.Info($"[PilotDefExtensions_GetRelativeCombatExperience] ({pilotDef.Description.Callsign}) MechKills: {pilotDef.MechKills}");
            Logger.Info($"[PilotDefExtensions_GetRelativeCombatExperience] ({pilotDef.Description.Callsign}) OtherKills: {pilotDef.OtherKills}");
            Logger.Info($"[PilotDefExtensions_GetRelativeCombatExperience] ({pilotDef.Description.Callsign}) MissionsPiloted: {pilotDef.MissionsPiloted}");
            Logger.Info($"[PilotDefExtensions_GetRelativeCombatExperience] ({pilotDef.Description.Callsign}) MissionsEjected: {pilotDef.MissionsEjected}");
            Logger.Info($"[PilotDefExtensions_GetRelativeCombatExperience] ({pilotDef.Description.Callsign}) LifetimeInjuries: {pilotDef.LifetimeInjuries}");

            // What * Importance / PercentageDivisor
            float mechKillsFactor = pilotDef.MechKills * 1f / 100;
            float otherKillsFactor = pilotDef.OtherKills * 0.5f / 100;
            float missionsPilotedFactor = pilotDef.MissionsPiloted * 0.5f / 100;
            float missionsEjectedFactor = pilotDef.MissionsEjected * 3f / 100;
            float lifetimeInjuriesFactor = pilotDef.LifetimeInjuries * 2f / 100;
            

            Logger.Info($"[PilotDefExtensions_GetRelativeCombatExperience] ({pilotDef.Description.Callsign}) mechKillsFactor: {mechKillsFactor}");
            Logger.Info($"[PilotDefExtensions_GetRelativeCombatExperience] ({pilotDef.Description.Callsign}) otherKillsFactor: {otherKillsFactor}");
            Logger.Info($"[PilotDefExtensions_GetRelativeCombatExperience] ({pilotDef.Description.Callsign}) missionsPilotedFactor: {missionsPilotedFactor}");
            Logger.Info($"[PilotDefExtensions_GetRelativeCombatExperience] ({pilotDef.Description.Callsign}) missionsEjectedFactor: {missionsEjectedFactor}");
            Logger.Info($"[PilotDefExtensions_GetRelativeCombatExperience] ({pilotDef.Description.Callsign}) lifetimeInjuriesFactor: {lifetimeInjuriesFactor}");
            

            result += mechKillsFactor;
            result += otherKillsFactor;
            result += missionsPilotedFactor;
            result += missionsEjectedFactor;
            result += lifetimeInjuriesFactor;

            // Prevent division by zero!
            if (pilotDef.MissionsPiloted > 0)
            {
                float averageKillsPerMission = (float)(pilotDef.MechKills + pilotDef.OtherKills) / (float)pilotDef.MissionsPiloted;
                Logger.Info($"[PilotDefExtensions_GetRelativeCombatExperience] ({pilotDef.Description.Callsign}) Average kills per mission: {averageKillsPerMission}");

                float averageKillsPerMissionFactor = averageKillsPerMission * 9f / 100;
                Logger.Info($"[PilotDefExtensions_GetRelativeCombatExperience] ({pilotDef.Description.Callsign}) averageKillsPerMissionFactor: {averageKillsPerMissionFactor}");

                result += averageKillsPerMissionFactor;
            }

            //result = Math.Max(min, result);
            //result = Math.Min(max, result);
            Mathf.Clamp(result, min, max);

            Logger.Debug($"[PilotDefExtensions_GetRelativeCombatExperience] ({pilotDef.Description.Callsign}) Result: {result}");

            return result;
        }
    }
}
