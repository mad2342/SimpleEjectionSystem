using BattleTech.UI;
using HBS;
using UnityEngine;

namespace SimpleEjectionSystem.Utilities
{
    public static class Miscellaneous
    {
        public static bool TryGetStressLevelColor(string str, out Color color)
        {
            switch (str)
            {
                case "CONFIDENT":
                    color = LazySingletonBehavior<UIManager>.Instance.UIColorRefs.white;
                    return true;
                case "UNEASY":
                    color = LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.StabilityPipsShown.color;
                    return true;
                case "UNSETTLED":
                    color = LazySingletonBehavior<UIManager>.Instance.UIColorRefs.orange;
                    return true;
                case "UNNERVED":
                    color = LazySingletonBehavior<UIManager>.Instance.UIColorRefs.orange;
                    return true;
                case "DESPERATE":
                    color = LazySingletonBehavior<UIManager>.Instance.UIColorRefs.red;
                    return true;
                default:
                    color = LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.FloatiePilotDamage.color;
                    return false;
            }
        }

        public static Color GetStressLevelColor(int lvl)
        {
            switch (lvl)
            {
                case 0:
                    return LazySingletonBehavior<UIManager>.Instance.UIColorRefs.white;
                case 1:
                    return LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.StabilityPipsShown.color;
                case 2:
                    return LazySingletonBehavior<UIManager>.Instance.UIColorRefs.orange;
                case 3:
                    return LazySingletonBehavior<UIManager>.Instance.UIColorRefs.orange;
                case 4:
                    return LazySingletonBehavior<UIManager>.Instance.UIColorRefs.red;
                default:
                    return LazySingletonBehavior<UIManager>.Instance.UIColorRefs.white;
            }
        }

        public static string GetStressLevelString(int lvl)
        {
            switch (lvl)
            {
                case 0:
                    return "CONFIDENT";
                case 1:
                    return "UNEASY";
                case 2:
                    return "UNSETTLED";
                case 3:
                    return "UNNERVED";
                case 4:
                    return "DESPERATE";
                default:
                    return "CONFIDENT";
            }
        }

        public static int GetStressLevelValue(string str)
        {
            switch (str)
            {
                case "CONFIDENT":
                    return 0;
                case "UNEASY":
                    return 1;
                case "UNSETTLED":
                    return 2;
                case "UNNERVED":
                    return 3;
                case "DESPERATE":
                    return 4;
                default:
                    return 0;
            }
        }
    }
}
