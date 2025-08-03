#region

using DragoonMayCry.Audio.BGM;
using DragoonMayCry.Audio.StyleAnnouncer;
using KamiLib.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

#endregion

namespace DragoonMayCry.Configuration
{
    public class JobConfiguration
    {
        public enum BgmConfiguration : long
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
        public Setting<long> Bgm = new(0);
        public Setting<HashSet<long>> BgmRandomSelection = new([
            BgmKeys.BuryTheLight, BgmKeys.CrimsonCloud, BgmKeys.DevilTrigger, BgmKeys.Subhuman,
        ]);
        public Setting<DifficultyMode> DifficultyMode = new(Configuration.DifficultyMode.WyrmHunter);

        public Setting<bool> EnableDmc = new(true);

        [Obsolete("This property is obsolete, use DifficultyMode instead.")]
        public Setting<bool> EstinienMustDie = new(false);

        public Setting<float> GcdDropThreshold = new(0.2f);
        public Setting<bool> RandomizeAnnouncement = new(false);
        public Setting<float> ScoreMultiplier = new(1.0f);
        public JobConfiguration() { }

        [JsonConstructor]
        public JobConfiguration(
            Setting<AnnouncerType> announcer, Setting<long> bgm, Setting<HashSet<long>> bgmRandomSelection,
            Setting<DifficultyMode> difficultyMode, Setting<bool> enableDmc,
            Setting<float> gcdDropThreshold, Setting<bool> randomizeAnnoucement, Setting<float> scoreMultiplier)
        {
            Announcer = announcer;
            Bgm = bgm;
            BgmRandomSelection = bgmRandomSelection;
            DifficultyMode = difficultyMode;
            EnableDmc = enableDmc;
            GcdDropThreshold = gcdDropThreshold;
            RandomizeAnnouncement = randomizeAnnoucement;
            ScoreMultiplier = scoreMultiplier;
        }
    }
}
