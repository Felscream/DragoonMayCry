using Dalamud.Configuration;
using Dalamud.Plugin;
using DragoonMayCry.Configuration;
using System;

namespace DragoonMayCry.Configuration;

[Serializable]
public class DmcConfiguration : IPluginConfiguration
{
    public int Version { get; set; } = 0;
    public int SfxVolume { get; set; } = 80;
    public bool PlaySoundEffects { get; set; } = true;
    public bool ForceSoundEffectsOnBlunder { get; set; } = false;
    public int PlaySfxEveryOccurrences { get; set; } = 3;
    public bool ApplyGameVolume { get; set; } = true;
    public bool ActiveOutsideInstance { get; set; } = false;

    public StyleRankUiConfiguration StyleRankUiConfiguration = new();


    // the below exist just to make saving less cumbersome
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
