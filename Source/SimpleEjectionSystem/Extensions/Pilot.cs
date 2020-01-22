using System;
using BattleTech;

namespace SimpleEjectionSystem.Extensions
{
    public static class PilotExtensions
    {
        public static float GetLastEjectionChance(this Pilot pilot)
        {
            float lec = 0;
            if (pilot.StatCollection.ContainsStatistic("LastEjectionChance"))
            {
                lec = pilot.StatCollection.GetValue<float>("LastEjectionChance");
            }
            else
            {
                pilot.StatCollection.AddStatistic<float>("LastEjectionChance", lec);
            }
            Logger.Debug($"[PilotExtensions_GetLastEjectionChance] ({pilot.Callsign}) LastEjectionChance: {lec}");
            return lec;
        }

        public static void SetLastEjectionChance(this Pilot pilot, float num)
        {
            if (num < 0f || num > 100f)
            {
                throw new ArgumentOutOfRangeException("num");
            }

            if (pilot.StatCollection.ContainsStatistic("LastEjectionChance"))
            {
                pilot.StatCollection.Set<float>("LastEjectionChance", num);
            }
            else
            {
                pilot.StatCollection.AddStatistic<float>("LastEjectionChance", num);
            }
            Logger.Debug($"[PilotExtensions_SetLastEjectionChance] ({pilot.Callsign}) LastEjectionChance: {num}");
        }

        public static int GetStressLevel(this Pilot pilot)
        {
            int sl = 0;
            if (pilot.StatCollection.ContainsStatistic("StressLevel"))
            {
                sl = pilot.StatCollection.GetValue<int>("StressLevel");
            }
            else
            {
                pilot.StatCollection.AddStatistic<int>("StressLevel", sl);
            }
            Logger.Debug($"[PilotExtensions_GetStressLevel] ({pilot.Callsign}) StressLevel: {sl}");
            return sl;
        }

        public static Pilot SetStressLevel(this Pilot pilot, int num)
        {
            if(num < 0 || num > 4)
            {
                throw new ArgumentOutOfRangeException("num");
            }

            if (pilot.StatCollection.ContainsStatistic("StressLevel"))
            {
                pilot.StatCollection.Set<int>("StressLevel", num);
            }
            else
            {
                pilot.StatCollection.AddStatistic<int>("StressLevel", num);
            }
            Logger.Debug($"[PilotExtensions_SetStressLevel] ({pilot.Callsign}) StressLevel: {num}");

            return pilot;
        }

        public static Pilot IncreaseStressLevel(this Pilot pilot, int num)
        {
            int sl = 0;
            if (pilot.StatCollection.ContainsStatistic("StressLevel"))
            {
                sl = pilot.StatCollection.GetValue<int>("StressLevel");
            }
            else
            {
                pilot.StatCollection.AddStatistic<int>("StressLevel", sl);
            }
            sl = Math.Min(4, sl + num);
            pilot.StatCollection.Set<int>("StressLevel", sl);
            Logger.Debug($"[PilotExtensions_IncreaseStressLevel] ({pilot.Callsign}) StressLevel: {sl}");

            return pilot;
        }

        public static Pilot DecreaseStressLevel(this Pilot pilot, int num)
        {
            int sl = 0;
            if (pilot.StatCollection.ContainsStatistic("StressLevel"))
            {
                sl = pilot.StatCollection.GetValue<int>("StressLevel");
            }
            else
            {
                pilot.StatCollection.AddStatistic<int>("StressLevel", sl);
            }
            sl = Math.Max(0, sl - num);
            pilot.StatCollection.Set<int>("StressLevel", sl);
            Logger.Debug($"[PilotExtensions_DecreaseStressLevel] ({pilot.Callsign}) StressLevel: {sl}");

            return pilot;
        }

        public static bool IsDesperate(this Pilot pilot)
        {
            int sl = 0;
            if (pilot.StatCollection.ContainsStatistic("StressLevel"))
            {
                sl = pilot.StatCollection.GetValue<int>("StressLevel");
            }
            else
            {
                pilot.StatCollection.AddStatistic<int>("StressLevel", sl);
            }
            Logger.Debug($"[PilotExtensions_IsDesperate] ({pilot.Callsign}) IsDesperate: {(sl >= 4)}");
            return (sl >= 4);
        }
    }
}
