using System.Collections.Generic;

namespace DragoonMayCry.Data
{
    public static class DebuffIds
    {
        public static HashSet<uint> DamageDownIds = new()
        {
            215, 628, 696, 1016, 1090, 2092, 2404, 2522, 2911, 3166, 3304, 3964
        };

        public static HashSet<uint> SustainedDamageIds = new()
        {
            2935, // found on Valigarmanda, M1S, P9S, P10S
            3692, // P10S poison
            4149, // M3S
        };

        public static HashSet<uint> VulnerabilityUpIds = new()
        {
            202,
            1789, // found on Valigarmanda, Zoraal Ja
        };

        public static HashSet<uint> ToadIds = new()
        {
            439, 1292, 1134, // toad, piggy, imp (required for a mechanic in O3S)
        };

        public static HashSet<uint> DownForTheCountIds = new()
        {
            625, 774, 783, 896, 1762, 1785, 1950, 1953, 1963, 2408, 2910, 2961, 3165, 3501, 3730, 3908, 3983, 4132
        };

        // 3552 on P10S if you don't find a safe spot near a tower
        public static HashSet<uint> FettersIds = new()
        {
            292, 504, 510, 667, 668, 770, 800, 822, 901, 930, 990, 1010, 1055, 1153, 1258, 1391, 1399, 1460, 1477, 1497, 1614, 1726, 1757, 1849, 1908, 2285, 2286, 2304, 2407, 2975, 3249, 3283, 3324, 3421, 3755
        };

        public static HashSet<uint> OutOfTheActionIds = new()
        {
            626, 939, 1113, 1284, 1462, 2109
        };

        public static HashSet<uint> ForcedMarchIds = new()
        {
            1257, 3629, 3719, 3737
        };

        public static HashSet<uint> PyreticIds = new()
        {
            639, 960, 1049, 1133, 1599, 3522
        };

        public static HashSet<uint> StopAndStunsIds = new()
        {
            900,
            2653,
            2953, // stun in tower of babil
        };

        public static bool IsIncapacitatingDebuff(uint debuffId)
        {
            return ToadIds.Contains(debuffId)
                || DownForTheCountIds.Contains(debuffId)
                || FettersIds.Contains(debuffId)
                || OutOfTheActionIds.Contains(debuffId)
                || ForcedMarchIds.Contains(debuffId)
                || PyreticIds.Contains(debuffId)
                || StopAndStunsIds.Contains(debuffId);
        }
    }
}
