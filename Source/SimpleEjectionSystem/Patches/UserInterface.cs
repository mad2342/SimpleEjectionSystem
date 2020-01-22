using System;
using System.Collections.Generic;
using BattleTech;
using BattleTech.UI;
using Harmony;
using HBS;
using SVGImporter;
using UnityEngine;
using SimpleEjectionSystem.Extensions;
using SimpleEjectionSystem.Utilities;

namespace SimpleEjectionSystem.Patches
{
    class UserInterface
    {
        [HarmonyPatch(typeof(CombatHUDMWStatus), "InitForPilot")]
        public static class CombatHUDMWStatus_InitForPilot_Patch
        {
            public static void Postfix(CombatHUDMWStatus __instance, Pilot pilot)
            {
                try
                {
                    if (pilot == null)
                    {
                        return;
                    }
                    Logger.Debug($"[CombatHUDMWStatus_InitForPilot_POSTFIX] Pilot: {pilot.Callsign}");
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            }
        }

        // Expand/Change CombatHUDMWStatus popout with more detailed information
        [HarmonyPatch(typeof(CombatHUDMWStatus), "RefreshPilot")]
        public static class CombatHUDMWStatus_RefreshPilot_Patch
        {
            public static void Postfix(CombatHUDMWStatus __instance, Pilot pilot)
            {
                try
                {
                    if (pilot == null)
                    {
                        return;
                    }
                    if(!(pilot.ParentActor is Mech mech))
                    {
                        return;
                    }

                    CombatHUDStatusStackItem pilotStateHeader = __instance.InjuriesItem;
                    Color high = LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.PilotDead.color;
                    Color med = LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.PilotWounded.color;
                    Color low = LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.PilotHealthy.color;

                    int stressLevel = pilot.GetStressLevel();
                    Localize.Text stressLevelDescription = new Localize.Text(Miscellaneous.GetStressLevelString(stressLevel), Array.Empty<object>());
                    Localize.Text unknownDescription = new Localize.Text("UNKNOWN", Array.Empty<object>());
                    Localize.Text stateDescription = pilot.IsIncapacitated || pilot.HasEjected ? unknownDescription : stressLevelDescription;

                    SVGAsset stateIcon = __instance.InjuriesItem.Icon.vectorGraphics;
                    //SVGAsset stateIcon = LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.FloatieIconPilotInjury;
                    Color stateColor = pilot.IsIncapacitated || pilot.HasEjected || (stressLevel >= 4) ? high : (stressLevel > 0) ? med : low;

                    //pilotStateHeader.ShowIcon(stateIcon, stateDescription, stateColor);
                    pilotStateHeader.ShowExistingIcon(stateDescription, stateColor);
                    pilotStateHeader.AddTooltipString(new Localize.Text("CARE FOR YOUR PILOT'S CONDITION OR THEY WILL LIKELY EJECT", new object[] { }), EffectNature.Buff);



                    // BEN: Beware! __instance.PassiveList needs to be cleared as it only has 20 "stackslots" available and there is no safeguard. What a bullshit interface design.
                    /*
                    Logger.Debug($"[CombatHUDMWStatus_RefreshPilot_PREFIX] __instance.PassivesList.Count: {__instance.PassivesList.Count}");
                    Logger.Debug($"[CombatHUDMWStatus_RefreshPilot_PREFIX] __instance.PassivesList.DisplayedStatusCount: {__instance.PassivesList.DisplayedStatusCount}");
                    List<CombatHUDStatusStackItem> ___stackItems = (List<CombatHUDStatusStackItem>)AccessTools.Field(typeof(CombatHUDStatusItemList), "stackItems").GetValue(__instance.PassivesList);
                    Logger.Debug($"[CombatHUDMWStatus_RefreshPilot_PREFIX] __instance.PassivesList.___stackItems: {___stackItems}");
                    */
                    __instance.PassivesList.ClearAllStatuses();



                    // Injuries
                    SVGAsset injuriesIcon = LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.FloatieIconPilotInjury;
                    Localize.Text injuriesText;
                    Color injuriesColor;

                    if (pilot.IsIncapacitated)
                    {
                        injuriesText = new Localize.Text("INCAPACITATED", new object[] { });
                        injuriesColor = high;
                    }
                    else
                    {
                        injuriesText = new Localize.Text("INJURIES: {0}/{1}", new object[]
                        {
                            pilot.Injuries,
                            pilot.Health
                        });
                        injuriesColor = (pilot.Injuries > 0) ? med : low;
                    }
                    __instance.PassivesList.ShowItem(injuriesIcon, injuriesText, new List<Localize.Text>(), new List<Localize.Text>()).Icon.color = injuriesColor;

                    // StressLevel
                    SVGAsset stressLevelIcon = LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.FloatieIconPilotInjury;
                    Localize.Text stressLevelText = new Localize.Text("STRESS: {0}/{1}", new object[]
                    {
                        stressLevel,
                        4
                    });
                    Color stressLevelColor = (stressLevel >= 4) ? high : (stressLevel > 0) ? med : low;
                    __instance.PassivesList.ShowItem(stressLevelIcon, stressLevelText, new List<Localize.Text>(), new List<Localize.Text>()).Icon.color = stressLevelColor;

                    // Morale
                    SVGAsset resolveIcon = LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.StatusInspiredIcon;
                    Localize.Text resolveText = new Localize.Text("FORTITUDE: {0}{1}", new object[]
                    {
                        (int)Math.Round(Assess.GetResistanceModifiers(mech, true, false)),
                        ""
                    });
                    Color resolveColor = low;
                    if (pilot.HasMoraleInspiredEffect)
                    {
                        resolveColor = LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.PilotInspired.color;
                    }
                    __instance.PassivesList.ShowItem(resolveIcon, resolveText, new List<Localize.Text>(), new List<Localize.Text>()).Icon.color = resolveColor;

                    // Weapons
                    SVGAsset weaponHealthIcon = LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.SmallHardpointIcon;
                    if (pilot.ParentActor?.UnitType == UnitType.Mech)
                    {
                        Mech m = pilot.ParentActor as Mech;
                        int functionalWeapons = m.Weapons.FindAll(w => w.DamageLevel == ComponentDamageLevel.Functional).Count;
                        int penalizedWeapons = m.Weapons.FindAll(w => w.DamageLevel == ComponentDamageLevel.Penalized).Count;
                        float weaponHealthRatio = (functionalWeapons - penalizedWeapons / 2) / (m.Weapons.Count);
                        int weaponHealthPercent = (int)Math.Round(weaponHealthRatio * 100);
                        Localize.Text weaponHealthText = new Localize.Text("WEAPONS: {0}{1}", new object[]
                        {
                            weaponHealthPercent,
                            "%"
                        });
                        Color weaponHealthColor = (weaponHealthPercent <= 25) ? high : (weaponHealthPercent <= 75) ? med : low;
                        __instance.PassivesList.ShowItem(weaponHealthIcon, weaponHealthText, new List<Localize.Text>(), new List<Localize.Text>()).Icon.color = weaponHealthColor;
                    }

                    // Mech Health
                    SVGAsset mechHealthIcon = LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.StatusCoverIcon;
                    if (pilot.ParentActor?.UnitType == UnitType.Mech)
                    {
                        Mech m = pilot.ParentActor as Mech;
                        float mechHealthRatio = (m.SummaryStructureCurrent + m.SummaryArmorCurrent) / (m.SummaryStructureMax + m.SummaryArmorMax);
                        int mechHealthPercent = (int)Math.Round(mechHealthRatio * 100);
                        Localize.Text mechHealthText = new Localize.Text("MECH: {0}{1}", new object[]
                        {
                            mechHealthPercent,
                            "%"
                        });
                        Color mechHealthColor = (mechHealthPercent <= 25) ? high : (mechHealthPercent <= 85) ? med : low;
                        __instance.PassivesList.ShowItem(mechHealthIcon, mechHealthText, new List<Localize.Text>(), new List<Localize.Text>()).Icon.color = mechHealthColor;
                    }

                    // Refresh colors? No joy
                    /*
                    List<CombatHUDStatusStackItem> stackItems = Traverse.Create(__instance.PassivesList).Field("stackItems").GetValue<List<CombatHUDStatusStackItem>>();
                    foreach (CombatHUDStatusStackItem item in stackItems)
                    {
                        if(item.IsInUse)
                        {
                            item.Icon.SetAllDirty();
                        }
                    }
                    //__instance.PassivesList.UpdateStatusVisibilities();
                    */



                    // Reference
                    //__instance.PassivesList.ShowItem("uixSvgIcon_genericDiamond", stressLevelDescription, new List<Localize.Text>(), new List<Localize.Text>()).Icon.color = injuriesColor;
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            }
        }
    }
}
