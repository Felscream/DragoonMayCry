using Dalamud.Configuration;
using DragoonMayCry.Audio.StyleAnnouncer;
using DragoonMayCry.Data;
using KamiLib.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace DragoonMayCry.Configuration
{
    [Serializable]
    public class DmcConfiguration : IPluginConfiguration
    {

        public readonly Dictionary<JobId, JobConfiguration> JobConfiguration = new()
        {
            { JobId.AST, new JobConfiguration() },
            { JobId.BLM, new JobConfiguration() },
            { JobId.BRD, new JobConfiguration() },
            { JobId.DNC, new JobConfiguration() },
            { JobId.DRG, new JobConfiguration() },
            { JobId.DRK, new JobConfiguration() },
            { JobId.GNB, new JobConfiguration() },
            { JobId.MCH, new JobConfiguration() },
            { JobId.MNK, new JobConfiguration() },
            { JobId.NIN, new JobConfiguration() },
            { JobId.PCT, new JobConfiguration() },
            { JobId.PLD, new JobConfiguration() },
            { JobId.RDM, new JobConfiguration() },
            { JobId.RPR, new JobConfiguration() },
            { JobId.SAM, new JobConfiguration() },
            { JobId.SCH, new JobConfiguration() },
            { JobId.SGE, new JobConfiguration() },
            { JobId.SMN, new JobConfiguration() },
            { JobId.VPR, new JobConfiguration() },
            { JobId.WAR, new JobConfiguration() },
            { JobId.WHM, new JobConfiguration() },
        };
        public Setting<bool> ActiveOutsideInstance = new(false);
        // deprecated
        public Setting<AnnouncerType> Announcer = new(AnnouncerType.DmC5);
        public Setting<bool> ApplyGameVolumeBgm = new(false);
        public Setting<bool> ApplyGameVolumeSfx = new(true);
        public Setting<int> BgmVolume = new(80);
        public Setting<bool> DisableAnnouncerBlunder = new(false);
        public Setting<ISet<uint>> DynamicBgmBlacklistDuties = new(new SortedSet<uint>());
        public Setting<bool> EnabledFinalRankChatLogging = new(true);
        public Setting<bool> EnableDynamicBgm = new(false);
        public Setting<bool> EnableHitCounter = new(true);
        public Setting<bool> EnableMuffledEffectOnDeath = new(false);
        public Setting<bool> EnableProgressGauge = new(true);
        public Setting<bool> ForceSoundEffectsOnBlunder = new(false);
        public Setting<bool> GoldSaucerEdition = new(false);
        public Setting<bool> HideInCutscenes = new(true);
        public Setting<int> PlaySfxEveryOccurrences = new(3);
        public Setting<bool> PlaySoundEffects = new(true);
        public Setting<int> RankDisplayScale = new(100);
        public Setting<int> SfxVolume = new(80);
        public Setting<bool> SplitLayout = new(false);
        public Setting<int> SplitLayoutProgressGaugeScale = new(100);
        public Setting<int> SplitLayoutRankDisplayScale = new(100);
        public Setting<bool> LockScoreWindow { get; set; } = new(true);
        public int Version { get; set; } = 2;

        public DmcConfiguration MigrateToVersionTwo()
        {
            foreach (var entry in JobConfiguration)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                entry.Value.DifficultyMode = entry.Value.EstinienMustDie
#pragma warning restore CS0618 // Type or member is obsolete
                                                 ? new Setting<DifficultyMode>(DifficultyMode.EstinienMustDie)
                                                 : new Setting<DifficultyMode>(DifficultyMode.WyrmHunter);
            }

            Version = 2;
            return this;
        }

        public void Save()
        {
            File.WriteAllText(Plugin.PluginInterface.ConfigFile.FullName,
                              JsonConvert.SerializeObject(this, Formatting.Indented));
        }
    }
}
