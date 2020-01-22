using System;
using BattleTech;
using SimpleEjectionSystem.Extensions;
using SimpleEjectionSystem.Utilities;

namespace SimpleEjectionSystem.Control
{
    internal static class Attack
    {
        public static bool TryPenetrateStressResistance(Mech mech, AttackDirector.AttackSequence attackSequence, out int stressLevel, out float ejectionChance)
        {
            Pilot pilot = mech.GetPilot();

            stressLevel = 0;
            ejectionChance = 0;

            // No brainers
            if (pilot == null || mech == null || mech.IsDead || (mech.IsFlaggedForDeath && !mech.HasHandledDeath))
            {
                Logger.Info($"[Attack_TryPenetrateStressResistance] ({mech?.DisplayName}) EXITING: No pilot, no mech or mech is already dead");
                return false;
            }

            // Elite pilots always resist
            if (pilot.IsElite && SimpleEjectionSystem.Settings.ElitePilotsAlwaysResists)
            {
                Logger.Info($"[Attack_TryPenetrateStressResistance] ({mech.DisplayName}) EXITING: Pilot is elite");
                return false;
            }

            // Don't take control from human player
            if (pilot.IsPlayerCharacter && SimpleEjectionSystem.Settings.PlayerCharacterAlwaysResists)
            {
                Logger.Info($"[Attack_TryPenetrateStressResistance] ({mech.DisplayName}) EXITING: Is commander");
                return false;
            }

            // No damage
            if (!attackSequence.GetAttackDidDamage(mech.GUID))
            {
                Logger.Info($"[Attack_TryPenetrateStressResistance] ({mech.DisplayName}) EXITING: Took no damage");
                return false;
            }

            // Ejection modifiers
            float ejectModifiers = 0;
            ejectModifiers += Assess.GetEjectionModifiersFromState(mech, pilot);
            ejectModifiers += Assess.GetEjectionModifiersFromAttack(mech, attackSequence);

            // Resistance modifiers
            float resistModifiers = 0;
            resistModifiers += Assess.GetResistanceModifiers(mech);

            // Evaluate
            Logger.Debug("---");
            float finalModifiers = (ejectModifiers - resistModifiers) * 5;
            Logger.Debug($"[Attack_TryPenetrateStressResistance] ({mech.DisplayName}) finalModifiers: {finalModifiers}");

            if (finalModifiers < 0)
            {
                Logger.Debug($"[Attack_TryPenetrateStressResistance] ({mech.DisplayName}) RESISTED!");
                Logger.Debug("---");
                return false;
            }

            // Raise pilot's stresslevel
            stressLevel = pilot.IncreaseStressLevel(1).GetStressLevel();

            // Sanitize ejection chance
            ejectionChance = Math.Min(100, finalModifiers);

            // Save ejection chance in pilot's StatCollection to potentially use it on next activation
            pilot.SetLastEjectionChance(ejectionChance);

            Logger.Debug($"[Attack_TryPenetrateStressResistance] ({mech.DisplayName}) Resistances BREACHED!");
            Logger.Debug("---");

            return true;
        }
    }
}
