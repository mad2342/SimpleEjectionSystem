using System;
using BattleTech;
using SimpleEjectionSystem.Extensions;
using SimpleEjectionSystem.Utilities;

namespace SimpleEjectionSystem.Control
{
    internal static class Actor
    {
        public static bool TryResistStressIncrease(Mech mech, Pilot pilot, out int stressLevel)
        {
            float modifiers = Assess.GetResistanceModifiers(mech, true);

            // Rolling...
            int randomRoll = (new System.Random()).Next(100);
            float resistanceChance = Math.Min(modifiers, 95);
            bool success = (randomRoll < resistanceChance);
            Logger.Debug($"[Actor_TryResistStressIncrease] ({mech.DisplayName})  Success: {success}");
            stressLevel = success ? pilot.GetStressLevel() : pilot.IncreaseStressLevel(1).GetStressLevel();

            return success;
        }

        public static bool TryReduceStressLevel(Mech mech, Pilot pilot, out int stressLevel)
        {
            float modifiers = Assess.GetResistanceModifiers(mech, true);

            // Rolling...
            int randomRoll = (new System.Random()).Next(100);
            float reductionChance = Math.Min(modifiers, 95);
            bool success = (randomRoll < reductionChance);
            Logger.Debug($"[Actor_TryReduceStressLevel] ({mech.DisplayName}) Success: {success}");
            stressLevel = success ? pilot.DecreaseStressLevel(1).GetStressLevel() : pilot.GetStressLevel();

            return success;
        }

        public static bool TryResistEjection(Mech mech, out bool criticalSuccess)
        {
            float modifiers = Assess.GetResistanceModifiers(mech, true);

            // Rolling...
            int randomRoll = (new System.Random()).Next(100);
            float resistanceChance = Math.Min(modifiers, 95);
            bool success = (randomRoll < resistanceChance);
            Logger.Debug($"[Actor_TryResistEjection] ({mech.DisplayName}) Success: {success}");

            criticalSuccess = randomRoll < 5;
            Logger.Debug($"[Actor_TryResistEjection] ({mech.DisplayName}) Critical success: {criticalSuccess}");

            return success;
        }

        public static bool RollForEjection(Mech mech, int stressLevel, float chance)
        {
            if (stressLevel < 4)
            {
                Logger.Debug($"[Actor_RollForEjection] ({mech.DisplayName}) Exiting! (Non critical stress level: {stressLevel})");
                return false;
            }

            return RollForEjection(mech, chance);
        }

        public static bool RollForEjection(Mech mech, float chance)
        {
            Pilot pilot = mech.GetPilot();

            if (mech == null || mech.IsDead || (mech.IsFlaggedForDeath && !mech.HasHandledDeath))
            {
                return false;
            }
            if (mech.IsProne && SimpleEjectionSystem.Settings.KnockedDownCannotEject)
            {
                return false;
            }
            if (!mech.CanBeHeadShot || (pilot != null && !pilot.CanEject))
            {
                return false;
            }

            int randomRoll = (new System.Random()).Next(100);
            mech.AddEjectionRoll();
            bool success = (randomRoll < chance);
            Logger.Debug($"[Actor_RollForEjection] ({mech.DisplayName}) Resisted: {!success}");

            return success;
        }
    }
}
