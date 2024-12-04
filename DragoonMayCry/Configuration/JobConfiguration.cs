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

        public Setting<bool> EnableDmc;
        public Setting<AnnouncerType> Announcer;
        public Setting<BgmConfiguration> Bgm;

        [Obsolete("This property is obsolete, use DifficultyMode instead.")]
        public Setting<bool> EstinienMustDie = new(false);

        public Setting<float> GcdDropThreshold;
        public Setting<bool> RandomizeAnnouncement;
        public Setting<float> ScoreMultiplier;
        public Setting<DifficultyMode> DifficultyMode;

        public JobConfiguration()
        {
            EnableDmc = new Setting<bool>(true);
            Announcer = new Setting<AnnouncerType>(AnnouncerType.DmC5);
            Bgm = new Setting<BgmConfiguration>(BgmConfiguration.Off);
            EstinienMustDie = new Setting<bool>(false);
            GcdDropThreshold = new Setting<float>(0.2f);
            RandomizeAnnouncement = new Setting<bool>(false);
            ScoreMultiplier = new Setting<float>(1.0f);
            DifficultyMode = new Setting<DifficultyMode>(Configuration.DifficultyMode.WyrmHunter);
        }
    }
}
