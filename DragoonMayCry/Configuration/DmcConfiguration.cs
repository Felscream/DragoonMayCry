using Dalamud.Configuration;
using Dalamud.Plugin;
using DragoonMayCry.Configuration;
using System;

namespace DragoonMayCry.Configuration;

[Serializable]
public class DmcConfiguration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool IsConfigWindowMovable { get; set; } = true;
    public bool SomePropertyToBeSavedAndWithADefault { get; set; } = true;

    public bool Enabled = true;
    public float SFXVolume { get; set; } = 0.2f;
    public StyleRankUiConfiguration StyleRankUiConfiguration = new();

    // the below exist just to make saving less cumbersome
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
