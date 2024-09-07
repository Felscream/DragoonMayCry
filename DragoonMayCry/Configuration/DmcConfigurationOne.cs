using Dalamud.Configuration;
using Dalamud.Plugin;
using DragoonMayCry.Configuration;
using KamiLib.Configuration;
using KamiLib.Misc;
using Newtonsoft.Json;
using System;
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
    public Setting<bool> ApplyGameVolume = new(true);
    public Setting<bool> EnableDynamicBgm = new(false);

    public Setting<bool> LockScoreWindow { get; set; } = new(true);

    public void Save()
    {
        File.WriteAllText(Plugin.PluginInterface.ConfigFile.FullName,
                          JsonConvert.SerializeObject(this, Formatting.Indented));
    }
}
