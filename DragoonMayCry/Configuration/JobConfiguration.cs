using System;
using DragoonMayCry.Audio.StyleAnnouncer;
using KamiLib.Configuration;

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
            Randomize = 4
        }

        public Setting<bool> EnableDmc = new(true);
        public Setting<AnnouncerType> Announcer = new(AnnouncerType.DmC5);
        public Setting<BgmConfiguration> Bgm = new(BgmConfiguration.Off);

        [Obsolete("This property is obsolete, use DifficultyMode instead.")]
        public Setting<bool> EstinienMustDie = new(false);

        public Setting<float> GcdDropThreshold = new(0.2f);
        public Setting<bool> RandomizeAnnouncement = new(false);
        public Setting<float> ScoreMultiplier = new(1.0f);
        public Setting<DifficultyMode> DifficultyMode = new(Configuration.DifficultyMode.WyrmHunter);
    }
}
