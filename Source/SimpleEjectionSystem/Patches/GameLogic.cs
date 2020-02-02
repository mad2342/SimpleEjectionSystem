using System;
using System.Collections.Generic;
using BattleTech;
using Harmony;
using SimpleEjectionSystem.Extensions;
using SimpleEjectionSystem.Utilities;
using SimpleEjectionSystem.Control;
using HBS;

namespace SimpleEjectionSystem.Patches
{
    class GameLogic
    {
        [HarmonyPatch(typeof(MechShutdownSequence), "CheckForHeatDamage")]
        public static class MechShutdownSequence_CheckForHeatDamage_Patch
        {
            public static bool Prefix(MechShutdownSequence __instance)
            {
                try
                {
                    Mech mech = (Mech)AccessTools.Property(typeof(MechShutdownSequence), "OwningMech").GetValue(__instance, null);
                    Pilot pilot = mech.pilot;
                    Logger.Debug($"[MechShutdownSequence_CheckForHeatDamage_PREFIX] ({mech.DisplayName}) Is shutting down");

                    bool pilotHealthOne = pilot.Health - pilot.Injuries <= 1;
                    bool shutdownCausesInjury = LazySingletonBehavior<UnityGameInstance>.Instance.Game.Simulation.CombatConstants.Heat.ShutdownCausesInjury;
                    bool isGoingToDie = pilotHealthOne && shutdownCausesInjury;
                    Logger.Debug($"[MechShutdownSequence_CheckForHeatDamage_PREFIX] ({mech.DisplayName}) Is going to die: {isGoingToDie}");

                    if (isGoingToDie && Actor.RollForEjection(mech, SimpleEjectionSystem.Settings.PointlessEjectionChance))
                    {
                        // Off he goes
                        mech.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(new ShowActorInfoSequence(mech, "PANICKED!", FloatieMessage.MessageNature.PilotInjury, true)));
                        mech.EjectPilot(mech.GUID, -1, DeathMethod.PilotEjection, false);

                        return false;
                    }

                    Logger.Debug($"[MechShutdownSequence_CheckForHeatDamage_PREFIX] ({mech.DisplayName}) Pilot is desperate: {pilot.IsDesperate()}");
                    if (pilot.IsDesperate() && Actor.RollForEjection(mech, pilot.GetLastEjectionChance()))
                    {
                        // Off he goes
                        mech.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(new ShowActorInfoSequence(mech, "PANICKED!", FloatieMessage.MessageNature.PilotInjury, true)));
                        mech.EjectPilot(mech.GUID, -1, DeathMethod.PilotEjection, false);

                        return false;
                    }

                    return true;
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                    return true;
                }
            }

            public static void Postfix(MechShutdownSequence __instance)
            {
                try
                {
                    Mech mech = (Mech)AccessTools.Property(typeof(MechShutdownSequence), "OwningMech").GetValue(__instance, null);
                    Pilot pilot = mech.pilot;
                    Logger.Debug($"[MechShutdownSequence_CheckForHeatDamage_POSTFIX] ({mech.DisplayName}) Is shutting down");

                    if (!Actor.TryResistStressIncrease(mech, pilot, out int stressLevel))
                    {
                        string floatieMessage = Miscellaneous.GetStressLevelString(stressLevel);
                        mech.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(new ShowActorInfoSequence(mech, floatieMessage, FloatieMessage.MessageNature.PilotInjury, true)));
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            }
        }



        [HarmonyPatch(typeof(Mech), "ApplyMoraleDefendEffects")]
        public static class Mech_ApplyMoraleDefendEffects_Patch
        {
            public static void Postfix(Mech __instance)
            {
                try
                {
                    Pilot pilot = __instance.GetPilot();
                    if (pilot == null)
                    {
                        return;
                    }
                    Logger.Debug($"[Mech_ApplyMoraleDefendEffects_POSTFIX] ({__instance.DisplayName}) Called");

                    int stressLevel = pilot.GetStressLevel();
                    if (stressLevel > 0)
                    {
                        stressLevel = pilot.DecreaseStressLevel(2).GetStressLevel();
                        __instance.Combat.MessageCenter.PublishMessage(new FloatieMessage(__instance.GUID, __instance.GUID, "RESOLUTE!", FloatieMessage.MessageNature.Inspiration));

                        string floatieMessage = Miscellaneous.GetStressLevelString(stressLevel);
                        __instance.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(new ShowActorInfoSequence(__instance, floatieMessage, FloatieMessage.MessageNature.PilotInjury, true)));
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(Mech), "OnNewRound")]
        public static class Mech_OnNewRound_Patch
        {
            public static void Postfix(Mech __instance)
            {
                try
                {
                    Pilot pilot = __instance.GetPilot();
                    if (__instance.IsDead || __instance.IsFlaggedForDeath || pilot == null)
                    {
                        return;
                    }

                    int stressLevel = pilot.GetStressLevel();
                    bool isUpright = __instance.IsOperational && !__instance.IsProne;
                    bool isHopeless = isUpright && __instance.IsUseless();
                    bool tryFocus = isUpright && stressLevel > 0;
                    Logger.Debug($"[Mech_OnNewRound_POSTFIX] ({__instance.DisplayName}) isHopeless: {isHopeless}");
                    Logger.Debug($"[Mech_OnNewRound_POSTFIX] ({__instance.DisplayName}) tryFocus: {tryFocus}");

                    if (isHopeless && Actor.RollForEjection(__instance, SimpleEjectionSystem.Settings.PointlessEjectionChance))
                    {
                        // Off he goes
                        __instance.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(new ShowActorInfoSequence(__instance, "HOPELESS!", FloatieMessage.MessageNature.PilotInjury, true)));
                        __instance.EjectPilot(__instance.GUID, -1, DeathMethod.PilotEjection, false);
                    }

                    if (tryFocus && Actor.TryReduceStressLevel(__instance, pilot, out stressLevel))
                    {
                        __instance.Combat.MessageCenter.PublishMessage(new FloatieMessage(__instance.GUID, __instance.GUID, "RESOLUTE!", FloatieMessage.MessageNature.Inspiration));
                        Logger.Debug($"[Mech_OnNewRound_POSTFIX] ({__instance.DisplayName}) Reduced stress level: {stressLevel}");
                    }

                    // Player only floatie
                    if (__instance.team.IsLocalPlayer)
                    {
                        string floatieMessage = Miscellaneous.GetStressLevelString(stressLevel);
                        __instance.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(new ShowActorInfoSequence(__instance, floatieMessage, FloatieMessage.MessageNature.PilotInjury, false)));
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            }
        }

        // @ToDo: In rare occasions this can lead to jumping mechs with an already ejected pilot(!), probably use OrderSequence.OnAdded() instead?
        [HarmonyPatch(typeof(AbstractActor), "OnActivationBegin")]
        public static class AbstractActor_OnActivationBegin_Patch
        {
            public static void Prefix(AbstractActor __instance)
            {
                try
                {
                    Pilot pilot = __instance.GetPilot();
                    if (!(__instance is Mech mech) || pilot == null)
                    {
                        return;
                    }

                    if (!mech.HasBegunActivation && pilot.IsDesperate())
                    {
                        Logger.Debug($"[AbstractActor_OnActivationBegin_PREFIX] ({mech.DisplayName}) has not yet begun activation AND {pilot.Callsign} is desperate");

                        if (Actor.TryResistEjection(mech, out bool criticalSuccess))
                        {
                            if (criticalSuccess)
                            {
                                // Reduce stress level
                                __instance.Combat.MessageCenter.PublishMessage(new FloatieMessage(__instance.GUID, __instance.GUID, "RESOLUTE!", FloatieMessage.MessageNature.Inspiration));
                                int stressLevel = pilot.DecreaseStressLevel(1).GetStressLevel();
                                string floatieMessage = Miscellaneous.GetStressLevelString(stressLevel);

                                __instance.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(new ShowActorInfoSequence(__instance, floatieMessage, FloatieMessage.MessageNature.PilotInjury, true)));
                            }
                            return;
                        }
                        else
                        {
                            if (Actor.RollForEjection(mech, pilot.GetStressLevel(), pilot.GetLastEjectionChance()))
                            {
                                // Off he goes
                                mech.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(new ShowActorInfoSequence(mech, "PANICKED!", FloatieMessage.MessageNature.PilotInjury, true)));
                                mech.EjectPilot(mech.GUID, -1, DeathMethod.PilotEjection, false);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(AttackStackSequence), "OnAttackComplete")]
        public static class AttackStackSequence_OnAttack_Complete_Patch
        {
            public static void Prefix(AttackStackSequence __instance, MessageCenterMessage message)
            {
                try
                {
                    if (!(message is AttackCompleteMessage attackCompleteMessage) || attackCompleteMessage.stackItemUID != __instance.SequenceGUID)
                    {
                        return;
                    }

                    //@ToDo: Find "Stray Shot" targets and call rolls too?
                    List<string> allEffectedTargetIds = __instance.directorSequences[0].allAffectedTargetIds;
                    foreach (string id in allEffectedTargetIds)
                    {
                        ICombatant combatant = __instance.directorSequences[0].Director.Combat.FindCombatantByGUID(id);
                        Logger.Debug($"[AttackStackSequence_OnAttack_Complete_PREFIX] ------> AFFECTED TARGET: {combatant.DisplayName}");

                        if ((combatant is Mech mech) && Attack.TryPenetrateStressResistance(mech, attackCompleteMessage.attackSequence, out int stressLevel, out float ejectionChance))
                        {
                            if (Actor.RollForEjection(mech, stressLevel, ejectionChance))
                            {
                                mech.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(new ShowActorInfoSequence(mech, "PANICKED!", FloatieMessage.MessageNature.PilotInjury, true)));
                                mech.EjectPilot(mech.GUID, attackCompleteMessage.stackItemUID, DeathMethod.PilotEjection, false);

                                // Ejections as a direct result of an attack should count as kills
                                if (__instance.directorSequences[0].attacker is Mech attackingMech && attackingMech.GetPilot() != null)
                                {
                                    Pilot attackingPilot = attackingMech.GetPilot();
                                    attackingPilot.LogMechKillInflicted(-1, attackingPilot.GUID);
                                }
                            }
                            else
                            {
                                string floatieMessage = Miscellaneous.GetStressLevelString(stressLevel);
                                mech.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(new ShowActorInfoSequence(mech, floatieMessage, FloatieMessage.MessageNature.PilotInjury, true)));
                            }
                        }
                    }



                    /* Only rolling on chosen target, ignoring stray shots...
                    if ((__instance.directorSequences[0].chosenTarget is Mech mech) && Attack.TryPenetrateStressResistance(mech, attackCompleteMessage.attackSequence, out int stressLevel, out float ejectionChance))
                    {
                        if (Actor.RollForEjection(mech, stressLevel, ejectionChance))
                        {
                            mech.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(new ShowActorInfoSequence(mech, "PANICKED!", FloatieMessage.MessageNature.PilotInjury, true)));
                            mech.EjectPilot(mech.GUID, attackCompleteMessage.stackItemUID, DeathMethod.PilotEjection, false);

                            // Ejections as a direct result of an attack should count as kills
                            if (__instance.directorSequences[0].attacker is Mech attackingMech && attackingMech.GetPilot() != null)
                            {
                                Pilot attackingPilot = attackingMech.GetPilot();
                                attackingPilot.LogMechKillInflicted(-1, attackingPilot.GUID);
                            }
                        }
                        else
                        {
                            string floatieMessage = Miscellaneous.GetStressLevelString(stressLevel);
                            mech.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(new ShowActorInfoSequence(mech, floatieMessage, FloatieMessage.MessageNature.PilotInjury, true)));
                        }
                    }
                    */
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            }
        }
    }
}
