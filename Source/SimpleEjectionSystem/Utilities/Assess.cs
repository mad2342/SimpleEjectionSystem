using System;
using BattleTech;
using SimpleEjectionSystem.Extensions;

namespace SimpleEjectionSystem.Utilities
{
    public static class Assess
    {
        public static float GetEjectionModifiersFromState(Mech mech, Pilot pilot, out bool isGoingToDie)
        {
            isGoingToDie = false;
            float ejectModifiers = 0f;

            // Head
            float headHealthRatio = mech.GetRemainingHealthRatio(ArmorLocation.Head);
            Logger.Info($"[Assess_GetEjectionModifiersFromState] ({mech.DisplayName}) headHealthRatio: {headHealthRatio}");
            if (headHealthRatio < 1)
            {
                ejectModifiers += SimpleEjectionSystem.Settings.HeadDamageMaxModifier * (1 - headHealthRatio);
                Logger.Info($"[Assess_GetEjectionModifiersFromState] ({mech.DisplayName}) -> ejectModifiers: {ejectModifiers}");
            }

            // Center Torso
            float ctHealthRatio = mech.GetRemainingHealthRatio(ArmorLocation.CenterTorso);
            Logger.Info($"[Assess_GetEjectionModifiersFromState] ({mech.DisplayName}) ctHealthRatio: {ctHealthRatio}");
            if (ctHealthRatio < 1)
            {
                ejectModifiers += SimpleEjectionSystem.Settings.CTDamageMaxModifier * (1 - ctHealthRatio);
                Logger.Info($"[Assess_GetEjectionModifiersFromState] ({mech.DisplayName}) -> ejectModifiers: {ejectModifiers}");
            }

            // Side Torsos
            float ltStructureRatio = mech.GetCurrentStructure(ChassisLocations.LeftTorso) / mech.GetMaxStructure(ChassisLocations.LeftTorso);
            Logger.Info($"[Assess_GetEjectionModifiersFromState] ({mech.DisplayName}) ltStructureRatio: {ltStructureRatio}");
            if (ltStructureRatio < 1)
            {
                ejectModifiers += SimpleEjectionSystem.Settings.SideTorsoInternalDamageMaxModifier * (1 - ltStructureRatio);
                Logger.Info($"[Assess_GetEjectionModifiersFromState] ({mech.DisplayName}) -> ejectModifiers: {ejectModifiers}");
            }
            float rtStructureRatio = mech.GetCurrentStructure(ChassisLocations.RightTorso) / mech.GetMaxStructure(ChassisLocations.RightTorso);
            Logger.Info($"[Assess_GetEjectionModifiersFromState] ({mech.DisplayName}) rtStructureRatio: {rtStructureRatio}");
            if (rtStructureRatio < 1)
            {
                ejectModifiers += SimpleEjectionSystem.Settings.SideTorsoInternalDamageMaxModifier * (1 - rtStructureRatio);
                Logger.Info($"[Assess_GetEjectionModifiersFromState] ({mech.DisplayName}) -> ejectModifiers: {ejectModifiers}");
            }

            // Legs
            if (mech.RightLegDamageLevel == LocationDamageLevel.Destroyed || mech.LeftLegDamageLevel == LocationDamageLevel.Destroyed)
            {
                float remainingLegHealthRatio;
                if (mech.LeftLegDamageLevel == LocationDamageLevel.Destroyed)
                {
                    Logger.Info($"[Assess_GetEjectionModifiersFromState] ({mech.DisplayName}) Left leg destroyed");
                    remainingLegHealthRatio = mech.GetRemainingHealthRatio(ArmorLocation.RightLeg);
                }
                else
                {
                    Logger.Info($"[Assess_GetEjectionModifiersFromState] ({mech.DisplayName}) Right leg destroyed");
                    remainingLegHealthRatio = mech.GetRemainingHealthRatio(ArmorLocation.LeftLeg);
                }
                if (remainingLegHealthRatio < 1)
                {
                    ejectModifiers += SimpleEjectionSystem.Settings.LeggedMaxModifier * (1 - remainingLegHealthRatio);
                    Logger.Info($"[Assess_GetEjectionModifiersFromState] ({mech.DisplayName}) -> ejectModifiers: {ejectModifiers}");
                }
            }

            // Unsteady (Custom simple check as Mech.CheckForInstability is invoked in original method and this is called by a prefix)
            if (mech.CurrentStability > mech.UnsteadyThreshold)
            {
                Logger.Info($"[Assess_GetEjectionModifiersFromState] ({mech.DisplayName}) is or will be unsteady");
                ejectModifiers += SimpleEjectionSystem.Settings.UnsteadyModifier;
                Logger.Info($"[Assess_GetEjectionModifiersFromState] ({mech.DisplayName}) -> ejectModifiers: {ejectModifiers}");
            }

            // All weapons destroyed
            if (mech.Weapons.TrueForAll(w => w.DamageLevel == ComponentDamageLevel.Destroyed || w.DamageLevel == ComponentDamageLevel.NonFunctional))
            {
                Logger.Info($"[Assess_GetEjectionModifiersFromState] ({mech.DisplayName}) All weapons destroyed");
                ejectModifiers += SimpleEjectionSystem.Settings.WeaponlessModifier;
                Logger.Info($"[Assess_GetEjectionModifiersFromState] ({mech.DisplayName}) -> ejectModifiers: {ejectModifiers}");
            }

            // Outnumbered
            int allLivingEnemyMechs = mech.Combat.GetAllEnemiesOf(mech).FindAll(m => !m.IsDead && m.UnitType == UnitType.Mech).Count;
            int allLivingAlliedMechs = 1 + mech.Combat.GetAllAlliesOf(mech).FindAll(m => !m.IsDead && m.UnitType == UnitType.Mech).Count;
            //Logger.Info($"[Assess_GetEjectionModifiersFromState] ({mech.DisplayName}) allLivingEnemyMechs: {allLivingEnemyMechs}");
            //Logger.Info($"[Assess_GetEjectionModifiersFromState] ({mech.DisplayName}) allLivingAlliedMechs: {allLivingAlliedMechs}");
            int isOutnumberedBy = Math.Max(0, (allLivingEnemyMechs - allLivingAlliedMechs));
            if (isOutnumberedBy > 0)
            {
                Logger.Info($"[Assess_GetEjectionModifiersFromState] ({mech.DisplayName}) isOutnumberedBy: {isOutnumberedBy}");
                ejectModifiers += (isOutnumberedBy * SimpleEjectionSystem.Settings.OutnumberedPerMechModifier);
                Logger.Info($"[Assess_GetEjectionModifiersFromState] ({mech.DisplayName}) -> ejectModifiers: {ejectModifiers}");
            }

            // Alone
            if (mech.Combat.GetAllAlliesOf(mech).TrueForAll(m => m.IsDead || m == mech))
            {
                Logger.Info($"[Assess_GetEjectionModifiersFromState] ({mech.DisplayName}) Alone");
                ejectModifiers += SimpleEjectionSystem.Settings.AloneModifier;
                Logger.Info($"[Assess_GetEjectionModifiersFromState] ({mech.DisplayName}) -> ejectModifiers: {ejectModifiers}");
            }

            // Pilot's health
            float pilotHealthRatio = ((pilot.Health - pilot.Injuries) / pilot.Health); // Don't use "TotalHealth" here, as Injuries only start to occur after "BonusHealth" is gone
            Logger.Info($"[Assess_GetEjectionModifiersFromState] ({mech.DisplayName}) pilotHealthRatio: {pilotHealthRatio} (H:{(pilot.Health - pilot.Injuries)}/{pilot.Health})");
            if (pilotHealthRatio < 1)
            {
                ejectModifiers += SimpleEjectionSystem.Settings.PilotHealthMaxModifier * (1 - pilotHealthRatio);

                // Pilot has only one health remaining
                if ((pilot.Health - pilot.Injuries) == 1)
                {
                    ejectModifiers += SimpleEjectionSystem.Settings.PilotHealthOneAddModifier;
                }
                Logger.Info($"[Assess_GetEjectionModifiersFromState] ({mech.DisplayName}) -> ejectModifiers: {ejectModifiers}");
            }

            // Pilot's morale (Low spirits)
            if (pilot.HasLowMorale)
            {
                Logger.Info($"[Assess_GetEjectionModifiersFromState] ({mech.DisplayName}) Pilot has low spirits");
                ejectModifiers += SimpleEjectionSystem.Settings.PilotLowMoraleModifier;
                Logger.Info($"[Assess_GetEjectionModifiersFromState] ({mech.DisplayName}) -> ejectModifiers: {ejectModifiers}");
            }

            // Pilot's stress level
            int stressLevel = pilot.GetStressLevel();
            if (stressLevel > 0)
            {
                ejectModifiers += (stressLevel * SimpleEjectionSystem.Settings.StressPerLevelModifier);
                Logger.Info($"[Assess_GetEjectionModifiersFromState] ({mech.DisplayName}) -> ejectModifiers: {ejectModifiers}");
            }

            // Is going to die 
            int pilotRemainingHealth = pilot.Health - pilot.Injuries; // Don't use "TotalHealth" here, as Injuries only start to occur after "BonusHealth" is gone
            if (mech.CheckForInstability() && pilotRemainingHealth == 1)
            {
                Logger.Info($"[Assess_GetEjectionModifiersFromState] ({mech.DisplayName}) Is going to die!");
                isGoingToDie = true;
                Logger.Info($"[Assess_GetEjectionModifiersFromState] ({mech.DisplayName}) out isGoingToDie: {isGoingToDie}");
            }
            Logger.Debug($"[Assess_GetEjectionModifiersFromState] ({mech.DisplayName}) ---> ejectModifiers: {ejectModifiers}");

            return ejectModifiers;
        }



        public static float GetEjectionModifiersFromAttack(Mech mech, AttackDirector.AttackSequence attackSequence)
        {
            Logger.Info("---");
            float ejectModifiers = 0f;

            // Attack destroyed any location
            if (attackSequence.GetAttackDestroyedAnyLocation(mech.GUID))
            {
                Logger.Info($"[Assess_GetEjectionModifiersFromAttack] ({mech.DisplayName}) just lost a location");
                ejectModifiers += SimpleEjectionSystem.Settings.AttackDestroyedAnyLocationModifier;
                Logger.Info($"[Assess_GetEjectionModifiersFromAttack] ({mech.DisplayName}) -> ejectModifiers: {ejectModifiers}");
            }

            // Attack caused ammo explosion
            if (attackSequence.GetAttackCausedAmmoExplosion(mech.GUID))
            {
                Logger.Info($"[Assess_GetEjectionModifiersFromAttack] ({mech.DisplayName}) just suffered an ammo explosion");
                ejectModifiers += SimpleEjectionSystem.Settings.AttackCausedAmmoExplosionModifier;
                Logger.Info($"[Assess_GetEjectionModifiersFromAttack] ({mech.DisplayName}) -> ejectModifiers: {ejectModifiers}");
            }

            // Lost one or more weapons
            if (attackSequence.GetAttackDestroyedWeapon(mech.GUID))
            {
                Logger.Info($"[Assess_GetEjectionModifiersFromAttack] ({mech.DisplayName}) just lost one or more weapons");
                ejectModifiers += SimpleEjectionSystem.Settings.AttackDestroyedWeaponModifier;
                Logger.Info($"[Assess_GetEjectionModifiersFromAttack] ({mech.DisplayName}) -> ejectModifiers: {ejectModifiers}");
            }

            // Next shot like that could kill 
            Logger.Info($"[Assess_GetEjectionModifiersFromAttack] ({mech.DisplayName}) attackSequence.cumulativeDamage: {attackSequence.cumulativeDamage}");
            float ctHealth = mech.GetRemainingHealth(ArmorLocation.CenterTorso);
            float legHealth = mech.GetRemainingHealth(ArmorLocation.RightLeg) + mech.GetRemainingHealth(ArmorLocation.LeftLeg);
            float mostVulnerableLocation = Math.Min(ctHealth, legHealth);
            Logger.Info($"[Assess_GetEjectionModifiersFromAttack] ({mech.DisplayName}) ctHealth: {ctHealth}");
            Logger.Info($"[Assess_GetEjectionModifiersFromAttack] ({mech.DisplayName}) legHealth: {legHealth}");
            Logger.Info($"[Assess_GetEjectionModifiersFromAttack] ({mech.DisplayName}) mostVulnerableLocation: {mostVulnerableLocation}");
            if (mostVulnerableLocation <= attackSequence.cumulativeDamage)
            {
                Logger.Info($"[Assess_GetEjectionModifiersFromAttack] ({mech.DisplayName}) Next shot like that could kill({attackSequence.cumulativeDamage} dmg)");
                ejectModifiers += SimpleEjectionSystem.Settings.NextShotLikeThatCouldKillModifier;
                Logger.Info($"[Assess_GetEjectionModifiersFromAttack] ({mech.DisplayName}) -> ejectModifiers: {ejectModifiers}");
            }
            Logger.Debug($"[Assess_GetEjectionModifiersFromAttack] ({mech.DisplayName}) ---> ejectModifiers: {ejectModifiers}");

            return ejectModifiers;
        }



        public static float GetResistanceModifiers(Mech mech, bool includeCeaseFireModifiers = false, bool log = true)
        {
            if(!log)
            {
                Logger.Sleep();
            }

            Logger.Info("---");
            Pilot p = mech.GetPilot();
            float resistModifiers = 0f;

            // Base
            float baseResistance = SimpleEjectionSystem.Settings.BaseEjectionResist;
            if (baseResistance > 0)
            {
                Logger.Info($"[Assess_GetResistanceModifiers] ({mech.DisplayName}) baseResistance: {baseResistance}");
                resistModifiers += baseResistance;
                Logger.Info($"[Assess_GetResistanceModifiers] ({mech.DisplayName}) -> resistModifiers: {resistModifiers}");
            }

            // Guts
            int guts = mech.SkillGuts;
            if (guts > 0)
            {
                float gutsResistance = (guts * SimpleEjectionSystem.Settings.GutsEjectionResistPerPoint);
                Logger.Info($"[Assess_GetResistanceModifiers] ({mech.DisplayName}) gutsResistance: {gutsResistance}");
                resistModifiers += gutsResistance;

                // Guts of 10  grants extra resistance
                if (guts >= 10)
                {
                    Logger.Info($"[Assess_GetResistanceModifiers] ({mech.DisplayName}) Has guts of 10");
                    resistModifiers += SimpleEjectionSystem.Settings.GutsTenAddEjectionResist;
                }
                Logger.Info($"[Assess_GetResistanceModifiers] ({mech.DisplayName}) -> resistModifiers: {resistModifiers}");
            }

            // Commander fielded
            bool isCommanderFielded = mech.Combat.GetAllAlliesOf(mech).TrueForAll(m => m.GetPilot() != null && m.GetPilot().IsPlayerCharacter);
            if (isCommanderFielded)
            {
                Logger.Info($"[Assess_GetResistanceModifiers] ({mech.DisplayName}) isCommanderFielded: {isCommanderFielded}");
                resistModifiers += SimpleEjectionSystem.Settings.CommanderFieldedEjectionResist;
                Logger.Info($"[Assess_GetResistanceModifiers] ({mech.DisplayName}) -> resistModifiers: {resistModifiers}");
            }

            // Inspired
            if (mech.IsMoraleInspired)
            {
                Logger.Info($"[Assess_GetResistanceModifiers] ({mech.DisplayName}) isMoraleInspired: {mech.IsMoraleInspired}");
                resistModifiers += SimpleEjectionSystem.Settings.InspiredEjectionResist;
                Logger.Info($"[Assess_GetResistanceModifiers] ({mech.DisplayName}) -> resistModifiers: {resistModifiers}");
            }

            // Pilot's morale (High spirits)
            if (p != null && p.HasLowMorale)
            {
                Logger.Info($"[Assess_GetResistanceModifiers] ({mech.DisplayName}) Pilot has high spirits");
                resistModifiers += SimpleEjectionSystem.Settings.PilotHighMoraleEjectionResist;
                Logger.Info($"[Assess_GetResistanceModifiers] ({mech.DisplayName}) -> resistModifiers: {resistModifiers}");
            }

            // Include modifiers for rolls during firing pauses (OnNewRound, OnActivation)
            if (includeCeaseFireModifiers)
            {
                // Company morale
                if (p != null && (mech.team.IsLocalPlayer && mech.team.CompanyMorale > 0))
                {
                    float moraleUtilizationRate = p.pilotDef.GetRelativeCombatExperience();
                    Logger.Info($"[Assess_GetResistanceModifiers] ({mech.DisplayName}) moraleUtilizationRate: {moraleUtilizationRate}");
                    Logger.Info($"[Assess_GetResistanceModifiers] ({mech.DisplayName}) mech.team.CompanyMorale: {mech.team.CompanyMorale}");
                    resistModifiers += moraleUtilizationRate * mech.team.CompanyMorale;
                    Logger.Info($"[Assess_GetResistanceModifiers] ({mech.DisplayName}) -> resistModifiers: {resistModifiers}");
                }

                // Pilot is still at full health
                if (p != null && p.Injuries <= 0)
                {
                    Logger.Info($"[Assess_GetResistanceModifiers] ({mech.DisplayName}) Pilot still has full health");
                    resistModifiers += SimpleEjectionSystem.Settings.PilotStillAtFullHealthModifier;
                    Logger.Info($"[Assess_GetResistanceModifiers] ({mech.DisplayName}) -> resistModifiers: {resistModifiers}");
                }

                // Mech is still at good health
                float mechHealthRatio = (mech.SummaryStructureCurrent + mech.SummaryArmorCurrent) / (mech.SummaryStructureMax + mech.SummaryArmorMax);
                if (mechHealthRatio >= 0.85)
                {
                    Logger.Info($"[Assess_GetResistanceModifiers] ({mech.DisplayName}) mechHealthRatio: {mechHealthRatio}");
                    resistModifiers += SimpleEjectionSystem.Settings.MechStillAtGoodHealthModifier;
                    Logger.Info($"[Assess_GetResistanceModifiers] ({mech.DisplayName}) -> resistModifiers: {resistModifiers}");
                }
            }
            Logger.Debug($"[Assess_GetResistanceModifiers] ({mech.DisplayName}) ---> resistModifiers: {resistModifiers}");



            // Difficulty setting
            if (mech.team.IsLocalPlayer)
            {
                float resistanceMultiplier = Miscellaneous.GetResistanceMultiplierForDifficulty(SimpleEjectionSystem.Settings.Difficulty);
                Logger.Debug($"[Assess_GetResistanceModifiers] (DIFFICULTY {SimpleEjectionSystem.Settings.Difficulty}) resistanceMultiplier: {resistanceMultiplier}");

                resistModifiers *= resistanceMultiplier;
                Logger.Debug($"[Assess_GetResistanceModifiers] ({mech.DisplayName}) ------> resistModifiers: {resistModifiers}");
            }



            if (!log)
            {
                Logger.Wake();
            }

            return resistModifiers;
        }
    }
}
