using BattleTech;

namespace SimpleEjectionSystem.Extensions
{
    public static class MechExtensions
    {
        public static float GetRemainingHealthRatio(this Mech mech, ArmorLocation aLocation)
        {
            ChassisLocations cLocation = MechStructureRules.GetChassisLocationFromArmorLocation(aLocation);

            float armorCurrent = mech.GetCurrentArmor(aLocation);
            float structureCurrent = mech.GetCurrentStructure(cLocation);
            float armorMax = mech.GetMaxArmor(aLocation);
            float structureMax = mech.GetMaxStructure(cLocation);
            Logger.Info($"[MechExtensions_GetRemainingHealthRatio] ({aLocation}) S:{structureCurrent}/{structureMax}, A:{armorCurrent}/{armorMax}");

            return (armorCurrent + structureCurrent) / (armorMax + structureMax);
        }

        public static float GetRemainingHealth(this Mech mech, ArmorLocation aLocation)
        {
            ChassisLocations cLocation = MechStructureRules.GetChassisLocationFromArmorLocation(aLocation);

            float armorCurrent = mech.GetCurrentArmor(aLocation);
            float structureCurrent = mech.GetCurrentStructure(cLocation);
            Logger.Info($"[MechExtensions_GetRemainingHealth] ({aLocation}) H:{structureCurrent + armorCurrent}(S:{structureCurrent}, A:{armorCurrent})");

            return (armorCurrent + structureCurrent);
        }

        public static bool IsUseless(this Mech mech)
        {
            bool isUseless;

            bool unarmed = mech.Weapons.TrueForAll(w => w.DamageLevel == ComponentDamageLevel.Destroyed || w.DamageLevel == ComponentDamageLevel.NonFunctional);
            bool alone = mech.Combat.GetAllAlliesOf(mech).TrueForAll(m => m.IsDead || m == mech);
            bool legged = mech.IsLegged;

            isUseless = unarmed && alone && legged;
            Logger.Debug($"[MechExtensions_IsUseless] ({mech.DisplayName}) isUseless: {isUseless}");

            return isUseless;
        }

        public static void AddEjectionRoll(this Mech m)
        {
            int rolls;
            if (m.StatCollection.ContainsStatistic("EjectionRolls"))
            {
                rolls = m.StatCollection.GetValue<int>("EjectionRolls");
                rolls++;
                m.StatCollection.Set<int>("EjectionRolls", rolls);
            }
            else
            {
                rolls = 1;
                m.StatCollection.AddStatistic<int>("EjectionRolls", rolls);
            }
            Logger.Debug($"[MechExtensions_AddEjectionRoll] ({m.DisplayName}) EjectionRolls: {rolls}");
        }
    }
}
