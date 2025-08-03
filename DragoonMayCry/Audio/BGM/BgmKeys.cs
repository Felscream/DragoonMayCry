#region

using System.Collections.Generic;
using System.Collections.Immutable;

#endregion

namespace DragoonMayCry.Audio.BGM
{
    public static class BgmKeys
    {
        public const long Off = -1L;
        public const long Randomize = 0L;
        public const long DevilsNeverCry = 1L;
        public const long CrimsonCloud = 2L;
        public const long BuryTheLight = 3L;
        public const long DevilTrigger = 4L;
        public const long Subhuman = 5L;
        public const long MinPreconfiguredBgmKey = 1L;
        public const long MaxPreconfiguredBgmKey = 5L;
        public const long DefaultBgmKey = BuryTheLight;
        public static readonly IList<long> PreConfiguredBgmKeys = new ImmutableArray<long>
        {
            BuryTheLight, DevilTrigger, Subhuman, CrimsonCloud, DevilsNeverCry,
        };
    }

}
