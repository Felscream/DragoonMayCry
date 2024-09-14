using Dalamud.Configuration;
using DragoonMayCry.Data;
using DragoonMayCry.Score.Style.Announcer;
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
    public Setting<AnnouncerType> Announcer = new(AnnouncerType.DmC5);
    public readonly Dictionary<JobIds, JobConfiguration> JobConfiguration = new Dictionary<JobIds, JobConfiguration>
    {
        { JobIds.AST, new JobConfiguration() },
        { JobIds.BLM, new JobConfiguration() },
        { JobIds.BRD, new JobConfiguration() },
        { JobIds.DNC, new JobConfiguration() },
        { JobIds.DRG, new JobConfiguration() },
        { JobIds.DRK, new JobConfiguration() },
        { JobIds.GNB, new JobConfiguration() },
        { JobIds.MCH, new JobConfiguration() },
        { JobIds.MNK, new JobConfiguration() },
        { JobIds.NIN, new JobConfiguration() },
        { JobIds.PCT, new JobConfiguration() },
        { JobIds.PLD, new JobConfiguration() },
        { JobIds.RDM, new JobConfiguration() },
        { JobIds.RPR, new JobConfiguration() },
        { JobIds.SAM, new JobConfiguration() },
        { JobIds.SCH, new JobConfiguration() },
        { JobIds.SGE, new JobConfiguration() },
        { JobIds.SMN, new JobConfiguration() },
        { JobIds.VPR, new JobConfiguration() },
        { JobIds.WAR, new JobConfiguration() },
        { JobIds.WHM, new JobConfiguration() },
    };

    public Setting<bool> LockScoreWindow { get; set; } = new(true);

    public void Save()
    {
        File.WriteAllText(Plugin.PluginInterface.ConfigFile.FullName,
                          JsonConvert.SerializeObject(this, Formatting.Indented));
    }
}
