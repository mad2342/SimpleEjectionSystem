namespace SimpleEjectionSystem.Utilities
{
    public static class Miscellaneous
    {
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
    }
}
