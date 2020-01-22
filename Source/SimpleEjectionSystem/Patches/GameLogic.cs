using System;
using System.Collections.Generic;
using BattleTech;
using Harmony;
using SimpleEjectionSystem.Extensions;
using SimpleEjectionSystem.Utilities;
using SimpleEjectionSystem.Control;

namespace SimpleEjectionSystem.Patches
{
    class GameLogic
    {
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
                    Logger.Debug("---");
                    Logger.Debug($"[Mech_ApplyMoraleDefendEffects_POSTFIX] ({__instance.DisplayName}) Called");

                    int stressLevel = pilot.GetStressLevel();
                    if (stressLevel > 0)
                    {
                        stressLevel = pilot.DecreaseStressLevel(2).GetStressLevel();
                        __instance.Combat.MessageCenter.PublishMessage(new FloatieMessage(__instance.GUID, __instance.GUID, "FOCUSSED!", FloatieMessage.MessageNature.Inspiration));

                        string floatieMessage = Miscellaneous.GetStressLevelString(stressLevel);
                        FloatieMessage.MessageNature floatieNature = (stressLevel > 0) ? FloatieMessage.MessageNature.Neutral : FloatieMessage.MessageNature.Buff;
                        __instance.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(new ShowActorInfoSequence(__instance, floatieMessage, floatieNature, true)));
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

                    Logger.Debug("---");
                    int stressLevel = pilot.GetStressLevel();
                    bool canFocus = __instance.IsOperational && !__instance.IsProne && stressLevel > 0;
                    Logger.Debug($"[Mech_OnNewRound_POSTFIX] ({__instance.DisplayName}) canFocus: {canFocus}");

                    if (canFocus && Actor.TryReduceStressLevel(__instance, pilot, out stressLevel))
                    {
                        //__instance.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(new ShowActorInfoSequence(__instance, "REASSURED!", FloatieMessage.MessageNature.Inspiration, true)));
                        __instance.Combat.MessageCenter.PublishMessage(new FloatieMessage(__instance.GUID, __instance.GUID, "FOCUSSED!", FloatieMessage.MessageNature.Inspiration));
                    }
                    string floatieMessage = Miscellaneous.GetStressLevelString(stressLevel);
                    FloatieMessage.MessageNature floatieNature = (stressLevel >= 4) ? FloatieMessage.MessageNature.PilotInjury : (stressLevel > 0) ? FloatieMessage.MessageNature.Neutral : FloatieMessage.MessageNature.Buff;

                    // Player only? -> if(__instance.team.IsLocalPlayer)
                    __instance.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(new ShowActorInfoSequence(__instance, floatieMessage, floatieNature, true)));
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            }
        }

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
                                __instance.Combat.MessageCenter.PublishMessage(new FloatieMessage(__instance.GUID, __instance.GUID, "FOCUSSED!", FloatieMessage.MessageNature.Inspiration));
                                int stressLevel = pilot.DecreaseStressLevel(1).GetStressLevel();
                                string floatieMessage = Miscellaneous.GetStressLevelString(stressLevel);
                                FloatieMessage.MessageNature floatieNature = (stressLevel > 0) ? FloatieMessage.MessageNature.Neutral : FloatieMessage.MessageNature.Buff;
                                __instance.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(new ShowActorInfoSequence(__instance, floatieMessage, floatieNature, true)));
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
                        Logger.Debug($"[AttackStackSequence_OnAttack_Complete_PREFIX] allEffectedTargets: {combatant.DisplayName}");
                    }

                    //if ((__instance.directorSequences[0].chosenTarget is Mech targetMech) && Eject.RollForStressGainAndEjectionResult(targetMech, attackCompleteMessage.attackSequence))
                    if ((__instance.directorSequences[0].chosenTarget is Mech mech) && Attack.TryPenetrateStressResistance(mech, attackCompleteMessage.attackSequence, out int stressLevel, out float ejectionChance))
                    {
                        if (Actor.RollForEjection(mech, stressLevel, ejectionChance))
                        {
                            mech.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(new ShowActorInfoSequence(mech, "PANICKED!", FloatieMessage.MessageNature.PilotInjury, true)));
                            mech.EjectPilot(mech.GUID, attackCompleteMessage.stackItemUID, DeathMethod.PilotEjection, false);
                        }
                        else
                        {
                            string floatieMessage = Miscellaneous.GetStressLevelString(stressLevel);
                            mech.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(new ShowActorInfoSequence(mech, floatieMessage, FloatieMessage.MessageNature.PilotInjury, true)));
                        }
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
