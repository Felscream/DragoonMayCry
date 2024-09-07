using Dalamud.Interface.Windowing;
using DragoonMayCry.Configuration;
using ImGuiNET;
using System;
using System.Numerics;
using DragoonMayCry.Score.Style;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using DragoonMayCry.State;
using KamiLib.Drawing;
using KamiLib;
using DragoonMayCry.Audio;

namespace DragoonMayCry.UI;

public class ConfigWindow : Window, IDisposable
{
    public EventHandler<bool>? ActiveOutsideInstanceChange;
    public EventHandler<int>? SfxVolumeChange;
    private readonly DmcConfigurationOne configuration;

    // We give this window a constant ID using ###
    // This allows for labels being dynamic, like "{FPS Counter}fps###XYZ counter window",
    // and the window ID will always be "###XYZ counter window" for ImGui
    public ConfigWindow(PluginUI pluginUi, DmcConfigurationOne configuration) : base("DragoonMayCry - Configuration")
    {

        Size = new Vector2(600,300);
        SizeCondition = ImGuiCond.Appearing;

        this.configuration = configuration;
    }

    public void Dispose() { }

    public override void Draw()
    {
        InfoBox.Instance.AddTitle("General")
            .AddConfigCheckbox("Lock rank window", configuration.LockScoreWindow)
            .AddAction(() =>
            {
                if(ImGui.Checkbox("Active outside instance", ref configuration.ActiveOutsideInstance.Value)){
                    KamiCommon.SaveConfiguration();
                    ActiveOutsideInstanceChange?.Invoke(this, configuration.ActiveOutsideInstance.Value);
                }
            })
            .Draw();

        InfoBox.Instance.AddTitle("Audio")
            .AddConfigCheckbox("Play sound effects", configuration.PlaySoundEffects)
            .AddConfigCheckbox("Force sound effect on blunders", configuration.ForceSoundEffectsOnBlunder)
            .AddAction(() => { 
                if(ImGui.Checkbox("Apply game volume", ref configuration.ApplyGameVolume.Value)){
                    KamiCommon.SaveConfiguration();
                    SfxVolumeChange?.Invoke(this, configuration.SfxVolume.Value);
                }
            })
            .AddAction(() =>
            {
                ImGui.SetNextItemWidth(200f);
                if (ImGui.SliderInt("Sound effect volume", ref configuration.SfxVolume.Value, 0, 100))
                {
                    KamiCommon.SaveConfiguration();
                    SfxVolumeChange?.Invoke(this, configuration.SfxVolume.Value);
                }
            })
            .AddString("Play each unique SFX only once every")
            .SameLine()
            .AddSliderInt("occurrences", configuration.PlaySfxEveryOccurrences, 1, 20)
            .Draw();

#if DEBUG
        InfoBox.Instance.AddTitle("Debug")
            .AddButton("Dead weight", () => AudioService.Instance.PlaySfx(SoundId.DeadWeight))
            .SameLine().AddButton("D", () => AudioService.Instance.PlaySfx(SoundId.Dirty))
            .SameLine().AddButton("C", () => AudioService.Instance.PlaySfx(SoundId.Cruel))
            .SameLine().AddButton("B", () => AudioService.Instance.PlaySfx(SoundId.Brutal))
            .SameLine().AddButton("A", () => AudioService.Instance.PlaySfx(SoundId.Anarchic))
            .SameLine().AddButton("S", () => AudioService.Instance.PlaySfx(SoundId.Savage))
            .SameLine().AddButton("SS", () => AudioService.Instance.PlaySfx(SoundId.Sadistic))
            .SameLine().AddButton("SSS", () => AudioService.Instance.PlaySfx(SoundId.Sensational))
            .Draw();
#endif
    }
}
