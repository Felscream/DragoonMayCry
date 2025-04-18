#region

using DragoonMayCry.Audio.BGM;
using DragoonMayCry.Audio.StyleAnnouncer;
using KamiLib.Configuration;
using System;
using System.Collections.Generic;

#endregion

namespace DragoonMayCry.Configuration
{
    public class JobConfiguration
    {
        public enum BgmConfiguration
        {
            Off = 0,
            BuryTheLight = 1,
            DevilTrigger = 2,
            CrimsonCloud = 3,
            Subhuman = 5,
            Randomize = 4,
            DevilsNeverCry = 6,
        }

        public Setting<AnnouncerType> Announcer = new(AnnouncerType.DmC5);
        public Setting<BgmConfiguration> Bgm = new(BgmConfiguration.Off);
        public Setting<HashSet<string>> BgmRandomSelection = new([
            BgmKeys.BuryTheLight, BgmKeys.DevilsNeverCry, BgmKeys.CrimsonCloud, BgmKeys.DevilTrigger, BgmKeys.Subhuman,
        ]);
        public Setting<DifficultyMode> DifficultyMode = new(Configuration.DifficultyMode.WyrmHunter);

        public Setting<bool> EnableDmc = new(true);

        [Obsolete("This property is obsolete, use DifficultyMode instead.")]
        public Setting<bool> EstinienMustDie = new(false);

        public Setting<float> GcdDropThreshold = new(0.2f);
        public Setting<bool> RandomizeAnnouncement = new(false);
        public Setting<float> ScoreMultiplier = new(1.0f);
    }
}
