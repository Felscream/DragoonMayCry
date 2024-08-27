using Dalamud.Configuration;
using Dalamud.Plugin;
using DragoonMayCry.Configuration;
using System;

namespace DragoonMayCry.Configuration;

[Serializable]
public class DmcConfiguration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool Enabled = true;
    public int SfxVolume { get; set; } = 80;
    public bool PlaySoundEffects { get; set; } = true;
    public int AnnouncerCooldown { get; set; } = 0;
    public bool ApplyGameVolume { get; set; } = true;
    public bool ActiveOutsideInstance { get; set; } = false;
    public float TimeBetweenDemotions { get; set; } = 4f;
    public float TimeBetweenRankChanges { get; set; } = 2.0f;
    public float DemotionTimerDuration { get; set; } = 3000f;
    public float GcdDropThreshold { get; set; } = 0.1f;
    public int DamageInstancesToCancelOnGcdClip { get; set; } = 3;
    public int GcdClippingRestrictionDuration { get; set; } = 6000; //milliseconds

    public int TimeToResetScoreAfterCombat { get; set; } = 10000;
    public StyleRankUiConfiguration StyleRankUiConfiguration = new();


    // the below exist just to make saving less cumbersome
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
