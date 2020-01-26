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

        //private static CombatHUDStatusStackItem _passiveStackItemMech;


        [HarmonyPatch(typeof(CombatHUDFloatieStack), "AddFloatie", new Type[] { typeof(Text), typeof(FloatieMessage.MessageNature) })]
        public static class CombatHUDFloatieStack_AddFloatie_Patch
        {
            public static void Prefix(CombatHUDFloatieStack __instance, Text text, FloatieMessage.MessageNature nature)
            {
                try
                {
                    //Logger.Debug($"[CombatHUDFloatieStack_AddFloatie_PREFIX] text: {text}");
                    //Logger.Debug($"[CombatHUDFloatieStack_AddFloatie_PREFIX] nature: {nature}");

                    if (nature == FloatieMessage.MessageNature.PilotInjury && Miscellaneous.TryGetStressLevelColor(text.ToString(), out Color color))
                    {
                        Logger.Debug($"[CombatHUDFloatieStack_AddFloatie_PREFIX] SET override color for FloatieMessage.MessageNature.PilotInjury");
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
                    //Logger.Debug($"[CombatHUDFloatieStack_AddFloatie_PREFIX] RESETTED color for FloatieMessage.MessageNature.PilotInjury");
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
                        Logger.Debug($"[CombatHUDFloatieStack_AddFloatie_PREFIX] OVERRIDING color for FloatieMessage.MessageNature.PilotInjury");
                        __result = OverrideFloatiePilotDamageColor;

                        return false;
                    }

                    if (nature == FloatieMessage.MessageNature.Inspiration)
                    {
                        Logger.Debug($"[CombatHUDFloatieStack_AddFloatie_PREFIX] OVERRIDING color for FloatieMessage.MessageNature.Inspiration");
                        __result = LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.PilotInspired.color;

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
        }

        [HarmonyPatch(typeof(CombatHUDMWStatus), "InitForPilot")]
        public static class CombatHUDMWStatus_InitForPilot_Patch
        {
            // Completeley overriding original method
            public static bool Prefix(CombatHUDMWStatus __instance, Pilot pilot)
            {
                try
                {
                    if (!(pilot.ParentActor is Mech mech))
                    {
                        return true;
                    }
                    Logger.Info($"[CombatHUDMWStatus_InitForPilot_PREFIX] Pilot: {pilot.Callsign}");

                    // These never change during a mission so they can stay here
                    __instance.MWStatsList.ClearAllStatuses();
                    __instance.MWStatsList.ShowItem(pilot.Tactics, new Text("TACTICS", Array.Empty<object>()), new List<Text>(), new List<Text>());
                    __instance.MWStatsList.ShowItem(pilot.Guts, new Text("GUTS", Array.Empty<object>()), new List<Text>(), new List<Text>());
                    __instance.MWStatsList.ShowItem(pilot.Piloting, new Text("PILOTING", Array.Empty<object>()), new List<Text>(), new List<Text>());
                    __instance.MWStatsList.ShowItem(pilot.Gunnery, new Text("GUNNERY", Array.Empty<object>()), new List<Text>(), new List<Text>());

                    // All potentially changing info goes in this method as it's the only one called after init
                    __instance.RefreshPilot(pilot);

                    __instance.InjuriesItem.UpdateVisibility();
                    __instance.InspiredItem.UpdateVisibility();
                    __instance.MWStatsList.UpdateStatusVisibilities();
                    __instance.PassivesList.UpdateStatusVisibilities();
                    __instance.ExpandedParentObject.SetActive(false);



                    // Skipping original method
                    return false;
                }
                catch (Exception e)
                {
                    Logger.Error(e);

                    return true;
                }
            }
        }

        // Expand/Change CombatHUDMWStatus popout with more detailed information
        [HarmonyPatch(typeof(CombatHUDMWStatus), "RefreshPilot")]
        public static class CombatHUDMWStatus_RefreshPilot_Patch
        {
            // Completeley overriding original method
            public static bool Prefix(CombatHUDMWStatus __instance, Pilot pilot)
            {
                try
                {
                    if(!(pilot.ParentActor is Mech mech))
                    {
                        return true;
                    }
                    Logger.Info($"[CombatHUDMWStatus_RefreshPilot_PREFIX] Pilot: {pilot.Callsign}");

                    Color inspired = LazySingletonBehavior<UIManager>.Instance.UIColorRefs.blue;
                    Color good = LazySingletonBehavior<UIManager>.Instance.UIColorRefs.white;
                    Color medium = LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.StabilityPipsShown.color;
                    Color bad = LazySingletonBehavior<UIManager>.Instance.UIColorRefs.orange;
                    Color critical = LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.FloatiePilotDamage.color;

                    // Utilize the former injuries item as a general state description
                    CombatHUDStatusStackItem pilotStateHeader = __instance.InjuriesItem;
                    int stressLevel = pilot.GetStressLevel();
                    Localize.Text stressLevelDescriptor = new Localize.Text(Miscellaneous.GetStressLevelString(stressLevel), Array.Empty<object>());
                    Localize.Text unknownDescriptor = new Localize.Text("UNKNOWN", Array.Empty<object>());
                    Localize.Text stateDescriptor = pilot.IsIncapacitated || pilot.HasEjected ? unknownDescriptor : stressLevelDescriptor;

                    SVGAsset stateIcon = __instance.InjuriesItem.Icon.vectorGraphics;
                    //SVGAsset stateIcon = LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.FloatieIconPilotInjury;
                    Color stateColor = Miscellaneous.GetStressLevelColor(stressLevel);

                    //pilotStateHeader.ShowIcon(stateIcon, stateDescription, stateColor);
                    pilotStateHeader.ShowExistingIcon(stateDescriptor, stateColor);
                    pilotStateHeader.AddTooltipString(new Localize.Text("???WILL THIS BE VISIBLE ANYWHERE AT ALL???", new object[] { }), EffectNature.Buff);

                    __instance.InspiredItem.Free();
                    
                    /* TRY: Put pilots personal morale (high|low spirits) in InspiredItem?
                    if (pilot.HasHighMorale)
                    {
                        Localize.Text highSpiritsDescriptor = new Localize.Text("HIGH SPIRITS", Array.Empty<object>());
                        Color highSpiritsColor = LazySingletonBehavior<UIManager>.Instance.UIColorRefs.blue;
                        __instance.InspiredItem.ShowExistingIcon(highSpiritsDescriptor, highSpiritsColor);
                    }
                    else if (pilot.HasLowMorale)
                    {
                        Localize.Text lowSpiritsDescriptor = new Localize.Text("LOW SPIRITS", Array.Empty<object>());
                        Color lowSpiritsColor = LazySingletonBehavior<UIManager>.Instance.UIColorRefs.orange;
                        __instance.InspiredItem.ShowExistingIcon(lowSpiritsDescriptor, lowSpiritsColor);
                    }
                    */



                    // Passives
                    // BEWARE: This needs to be cleared everytime as there's a limited number of stackslots available and there are no control mechanisms or safeguards available!
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
                        float pilotRemainingHealth = pilot.Health - pilot.Injuries;
                        injuriesColor = (pilotRemainingHealth == 1) ? critical : (pilotRemainingHealth == 2) ? bad : (pilotRemainingHealth < pilot.Health) ? medium : good;
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
                    int fortitudeValue = (int)Math.Round(Assess.GetResistanceModifiers(mech, true, false));

                    SVGAsset fortitudeIcon = LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.MoraleDefendButtonIcon;
                    Localize.Text fortitudeText = new Localize.Text("FORTITUDE: {0}{1}", new object[]
                    {
                        fortitudeValue,
                        ""
                    });
                    Color fortitudeColor = pilot.HasMoraleInspiredEffect ? inspired : good;
                    __instance.PassivesList.ShowItem(fortitudeIcon, fortitudeText, new List<Localize.Text>(), new List<Localize.Text>()).Icon.color = fortitudeColor;



                    /* High|Low spirits
                    if (pilot.HasHighMorale)
                    {
                        SVGAsset highSpiritsIcon = LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.StatusInspiredIcon;
                        Localize.Text highSpiritsDescriptor = new Localize.Text("HIGH SPIRITS", Array.Empty<object>());
                        Color highSpiritsColor = LazySingletonBehavior<UIManager>.Instance.UIColorRefs.blue;
                        __instance.PassivesList.ShowItem(highSpiritsIcon, highSpiritsDescriptor, new List<Localize.Text>(), new List<Localize.Text>()).Icon.color = highSpiritsColor;

                        // REF: This variant doesn't change anything regarding color updates
                        //CombatHUDStatusStackItem passiveStackItemHighSpirits = __instance.PassivesList.ShowItem(highSpiritsIcon, new Text("DUMMY", Array.Empty<object>()), new List<Localize.Text>(), new List<Localize.Text>());
                        //passiveStackItemHighSpirits.ShowExistingIcon(highSpiritsDescriptor, highSpiritsColor);
                    }
                    else if (pilot.HasLowMorale)
                    {
                        SVGAsset lowSpiritsIcon = LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.StatusInspiredIcon;
                        Localize.Text lowSpiritsDescriptor = new Localize.Text("LOW SPIRITS", Array.Empty<object>());
                        Color lowSpiritsColor = LazySingletonBehavior<UIManager>.Instance.UIColorRefs.orange;
                        __instance.PassivesList.ShowItem(lowSpiritsIcon, lowSpiritsDescriptor, new List<Localize.Text>(), new List<Localize.Text>()).Icon.color = lowSpiritsColor;
                    }
                    */



                    // Weapons 
                    int functionalWeapons = mech.Weapons.FindAll(w => w.DamageLevel == ComponentDamageLevel.Functional).Count;
                    int penalizedWeapons = mech.Weapons.FindAll(w => w.DamageLevel == ComponentDamageLevel.Penalized).Count;
                    float weaponHealthRatio = (float)(functionalWeapons - penalizedWeapons / 2) / (float)mech.Weapons.Count;
                    int weaponHealthPercent = (int)Math.Round(weaponHealthRatio * 100);

                    SVGAsset weaponHealthIcon = LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.SmallHardpointIcon;
                    Localize.Text weaponHealthText = new Localize.Text("WEAPONS: {0}{1}", new object[]
                    {
                        weaponHealthPercent,
                        "%"
                    });
                    Color weaponHealthColor = (weaponHealthPercent <= 25) ? critical : (weaponHealthPercent <= 50) ? bad : (weaponHealthPercent <= 75) ? medium : good;
                    __instance.PassivesList.ShowItem(weaponHealthIcon, weaponHealthText, new List<Localize.Text>(), new List<Localize.Text>()).Icon.color = weaponHealthColor;
                    


                    // Mech Health
                    float mechHealthRatio = (mech.SummaryStructureCurrent + mech.SummaryArmorCurrent) / (mech.SummaryStructureMax + mech.SummaryArmorMax);
                    int mechHealthPercent = (int)Math.Round(mechHealthRatio * 100);

                    SVGAsset mechHealthIcon = LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.StatusCoverIcon;
                    Localize.Text mechHealthDescriptor = new Localize.Text("MECH: {0}{1}", new object[] { mechHealthPercent, "%" });
                    Color mechHealthColor = (mechHealthPercent <= 25) ? critical : (mechHealthPercent <= 55) ? bad : (mechHealthPercent <= 85) ? medium : good;
                    __instance.PassivesList.ShowItem(mechHealthIcon, mechHealthDescriptor, new List<Localize.Text>(), new List<Localize.Text>()).Icon.color = mechHealthColor;



                    // Skipping original method
                    return false;
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                    return true;
                }
            }
        }
    }
}
