using Dalamud.Configuration;
using DragoonMayCry.Audio.StyleAnnouncer;
using DragoonMayCry.Data;
using KamiLib.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace DragoonMayCry.Configuration;

[Serializable]
public class DmcConfigurationOne : IPluginConfiguration
{
    public int Version { get; set; } = 1;
    public Setting<bool> ActiveOutsideInstance = new(false);
    public Setting<int> SfxVolume = new(80);
    public Setting<int> BgmVolume = new(80);
    public Setting<bool> PlaySoundEffects = new(true);
    public Setting<bool> ForceSoundEffectsOnBlunder = new(false);
    public Setting<int> PlaySfxEveryOccurrences = new(3);
    public Setting<bool> ApplyGameVolumeSfx = new(true);
    public Setting<bool> ApplyGameVolumeBgm = new(false);
    public Setting<bool> EnableDynamicBgm = new(false);
    public Setting<bool> EnableMuffledEffectOnDeath = new(false);
    public Setting<bool> EnabledFinalRankChatLogging = new(true);

    public Setting<bool> LockScoreWindow { get; set; } = new(true);

    // deprecated
    public Setting<AnnouncerType> Announcer = new(AnnouncerType.DmC5);
    public Setting<int> RankDisplayScale = new(100);

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

    public void Save()
    {
        File.WriteAllText(Plugin.PluginInterface.ConfigFile.FullName,
                          JsonConvert.SerializeObject(this, Formatting.Indented));
    }
}
