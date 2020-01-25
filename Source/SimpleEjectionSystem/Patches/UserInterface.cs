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
using Localize;

namespace SimpleEjectionSystem.Patches
{
    class UserInterface
    {
        private static bool OverrideFloatiePilotDamage = false;
        private static Color OverrideFloatiePilotDamageColor = LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.FloatiePilotDamage.color;



        [HarmonyPatch(typeof(CombatHUDFloatieStack), "AddFloatie", new Type[] { typeof(Text), typeof(FloatieMessage.MessageNature) })]
        public static class CombatHUDFloatieStack_AddFloatie_Patch
        {
            public static void Prefix(CombatHUDFloatieStack __instance, Text text, FloatieMessage.MessageNature nature)
            {
                try
                {
                    Logger.Debug($"[CombatHUDFloatieStack_AddFloatie_PREFIX] text: {text}");
                    Logger.Debug($"[CombatHUDFloatieStack_AddFloatie_PREFIX] nature: {nature}");

                    if (nature == FloatieMessage.MessageNature.PilotInjury && Miscellaneous.TryGetStressLevelColor(text.ToString(), out Color color))
                    {
                        OverrideFloatiePilotDamage = true;
                        OverrideFloatiePilotDamageColor = color;
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            }

            public static void Postfix()
            {
                try
                {
                    // Reset
                    OverrideFloatiePilotDamage = false;
                    OverrideFloatiePilotDamageColor = LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.FloatiePilotDamage.color;
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(CombatHUDFloatieStack), "GetFloatieColor")]
        public static class CombatHUDFloatieStack_GetFloatieColor_Patch
        {
            public static bool Prefix(CombatHUDFloatieStack __instance, ref Color __result, FloatieMessage.MessageNature nature)
            {
                try
                {
                    if (nature == FloatieMessage.MessageNature.PilotInjury && OverrideFloatiePilotDamage)
                    {
                        __result = OverrideFloatiePilotDamageColor;

                        return false;
                    }

                    if (nature == FloatieMessage.MessageNature.Inspiration)
                    {
                        __result = LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.PilotInspired.color;
                    }

                    return true;
                }
                catch (Exception e)
                {
                    Logger.Error(e);

                    return true;
                }
            }
        }

        [HarmonyPatch(typeof(CombatHUDMWStatus), "InitForPilot")]
        public static class CombatHUDMWStatus_InitForPilot_Patch
        {
            public static void Postfix(CombatHUDMWStatus __instance, Pilot pilot, SVGAsset highMorale, SVGAsset lowMorale, CombatHUD ___HUD)
            {
                try
                {
                    if (pilot == null)
                    {
                        return;
                    }
                    Logger.Debug($"[CombatHUDMWStatus_InitForPilot_POSTFIX] Pilot: {pilot.Callsign}");

                    List<CombatHUDStatusStackItem> ___stackItems = (List<CombatHUDStatusStackItem>)AccessTools.Field(typeof(CombatHUDStatusItemList), "stackItems").GetValue(__instance.PassivesList);

                    ___stackItems.Clear();
                    __instance.PassivesList.Init(___HUD, CombatHUDTooltipHoverElement.ToolTipOrientation.None, Vector3.zero);
                    __instance.PassivesList.ClearAllStatuses();

                    if (pilot.HasHighMorale)
                    {
                        SVGAsset highSpiritsIcon = LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.CommandButtonIcon;
                        __instance.PassivesList.ShowItem(highSpiritsIcon, new Text("HIGH SPIRITS", Array.Empty<object>()), new List<Text>(), new List<Text>()).Icon.color = LazySingletonBehavior<UIManager>.Instance.UIColorRefs.blue;
                    }
                    else if (pilot.HasLowMorale)
                    {
                        SVGAsset lowSpiritsIcon = LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.CommandButtonIcon;
                        __instance.PassivesList.ShowItem(lowSpiritsIcon, new Text("LOW SPIRITS", Array.Empty<object>()), new List<Text>(), new List<Text>()).Icon.color = LazySingletonBehavior<UIManager>.Instance.UIColorRefs.orange;
                    }
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
                    Color perfect = LazySingletonBehavior<UIManager>.Instance.UIColorRefs.white;
                    Color good = LazySingletonBehavior<UIManager>.Instance.UIColorRefs.green;
                    Color average = LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.StabilityPipsShown.color;
                    Color bad = LazySingletonBehavior<UIManager>.Instance.UIColorRefs.orange;
                    Color critical = LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.FloatiePilotDamage.color;

                    int stressLevel = pilot.GetStressLevel();
                    Localize.Text stressLevelDescription = new Localize.Text(Miscellaneous.GetStressLevelString(stressLevel), Array.Empty<object>());
                    Localize.Text unknownDescription = new Localize.Text("UNKNOWN", Array.Empty<object>());
                    Localize.Text stateDescription = pilot.IsIncapacitated || pilot.HasEjected ? unknownDescription : stressLevelDescription;

                    SVGAsset stateIcon = __instance.InjuriesItem.Icon.vectorGraphics;
                    //SVGAsset stateIcon = LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.FloatieIconPilotInjury;
                    Color stateColor = Miscellaneous.GetStressLevelColor(stressLevel);

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
                        injuriesColor = critical;
                    }
                    else
                    {
                        injuriesText = new Localize.Text("INJURIES: {0}/{1}", new object[]
                        {
                            pilot.Injuries,
                            pilot.Health
                        });
                        float pilotHealthRatio = ((pilot.Health - pilot.Injuries) / pilot.Health);
                        injuriesColor = (pilotHealthRatio < 0.4) ? bad : (pilotHealthRatio < 0.6) ? average : (pilotHealthRatio < 0.8) ? good : perfect;
                    }
                    __instance.PassivesList.ShowItem(injuriesIcon, injuriesText, new List<Localize.Text>(), new List<Localize.Text>()).Icon.color = injuriesColor;

                    // StressLevel
                    SVGAsset stressLevelIcon = LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.FloatieIconPilotInjury;
                    Localize.Text stressLevelText = new Localize.Text("STRESS: {0}/{1}", new object[]
                    {
                        stressLevel,
                        4
                    });
                    Color stressLevelColor = Miscellaneous.GetStressLevelColor(stressLevel);
                    __instance.PassivesList.ShowItem(stressLevelIcon, stressLevelText, new List<Localize.Text>(), new List<Localize.Text>()).Icon.color = stressLevelColor;

                    // Fortitude (Derived from resists, morale and combat experience)
                    SVGAsset fortitudeIcon = LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.MoraleDefendButtonIcon;
                    int fortitudeValue = (int)Math.Round(Assess.GetResistanceModifiers(mech, true, false));
                    Localize.Text fortitudeText = new Localize.Text("FORTITUDE: {0}{1}", new object[]
                    {
                        fortitudeValue,
                        ""
                    });
                    Color fortitudeColor;

                    if (pilot.HasMoraleInspiredEffect)
                    {
                        fortitudeColor = LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.PilotInspired.color;
                    }
                    else
                    {
                        //fortitudeColor = (fortitudeValue < 20) ? bad : (fortitudeValue < 30) ? average : good;
                        fortitudeColor = perfect;
                    }
                    __instance.PassivesList.ShowItem(fortitudeIcon, fortitudeText, new List<Localize.Text>(), new List<Localize.Text>()).Icon.color = fortitudeColor;

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
                        Color mechHealthColor = (mechHealthPercent <= 25) ? critical : (mechHealthPercent <= 50) ? bad : (mechHealthPercent <= 75) ? average : (mechHealthPercent <= 95) ? good : perfect;
                        __instance.PassivesList.ShowItem(mechHealthIcon, mechHealthText, new List<Localize.Text>(), new List<Localize.Text>()).Icon.color = mechHealthColor;
                    }

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
                        Color weaponHealthColor = (weaponHealthPercent <= 25) ? critical : (weaponHealthPercent <= 50) ? bad : (weaponHealthPercent <= 75) ? average : (weaponHealthPercent <= 95) ? good : perfect;
                        __instance.PassivesList.ShowItem(weaponHealthIcon, weaponHealthText, new List<Localize.Text>(), new List<Localize.Text>()).Icon.color = weaponHealthColor;
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
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            }
        }
    }
}
