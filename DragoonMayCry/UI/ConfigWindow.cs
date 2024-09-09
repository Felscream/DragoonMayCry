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
using Dalamud.Interface.Components;

namespace DragoonMayCry.UI;

public class ConfigWindow : Window, IDisposable
{
    public EventHandler<bool>? ActiveOutsideInstanceChange;
    public EventHandler<bool>? ActivateDynamicBgmChange;
    public EventHandler<int>? SfxVolumeChange;
    public EventHandler<int>? BgmVolumeChange;
    private readonly DmcConfigurationOne configuration;

    // We give this window a constant ID using ###
    // This allows for labels being dynamic, like "{FPS Counter}fps###XYZ counter window",
    // and the window ID will always be "###XYZ counter window" for ImGui
    public ConfigWindow(PluginUI pluginUi, DmcConfigurationOne configuration) : base("DragoonMayCry - Configuration")
    {

        Size = new Vector2(600,400);
        SizeCondition = ImGuiCond.Appearing;

        this.configuration = configuration;
    }

    public void Dispose() { }



    public override void Draw()
    {
        switch (AssetsManager.status)
        {
            case AssetsManager.Status.Done:
                DrawConfigMenu(); 
                break;
            case AssetsManager.Status.Updating:
                ImGui.Text($"Downloading additional assets...");
                break;
            case AssetsManager.Status.FailedDownloading:
            case AssetsManager.Status.FailedInsufficientDiskSpace:
            case AssetsManager.Status.FailedFileIntegrity:
                DrawError();
                break;
        }
    }

    private void DrawError()
    {
        var errorMessage = AssetsManager.status switch
        {
            AssetsManager.Status.FailedInsufficientDiskSpace => "Not enough disk space for additional assets",
            AssetsManager.Status.FailedDownloading => "Could not retrieve additional assets",
            AssetsManager.Status.FailedFileIntegrity => "File integrity check failed, redownload them",
            _ => "An unexpected error occured",
        };

        ImGui.Indent();
        ImGui.Text(errorMessage);
        if(ImGui.Button("Download assets"))
        {
            AssetsManager.FetchAudioFiles();
        }
    }

    private void DrawConfigMenu()
    {

        if (AssetsManager.status == AssetsManager.Status.Done)
        {
            InfoBox.Instance.AddTitle("General")
            .AddConfigCheckbox("Lock rank window", configuration.LockScoreWindow)
            .AddAction(() =>
            {
                if (ImGui.Checkbox("Active outside instance", ref configuration.ActiveOutsideInstance.Value))
                {
                    KamiCommon.SaveConfiguration();
                    ActiveOutsideInstanceChange?.Invoke(this, configuration.ActiveOutsideInstance.Value);
                }
            })
            .Draw();

        InfoBox.Instance.AddTitle("SFX")
            .AddConfigCheckbox("Play sound effects", configuration.PlaySoundEffects)
            .AddConfigCheckbox("Force sound effect on blunders", configuration.ForceSoundEffectsOnBlunder)
            .AddAction(() => { 
                if(ImGui.Checkbox("Apply game volume on sound effects", ref configuration.ApplyGameVolumeSfx.Value)){
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

        InfoBox.Instance.AddTitle("Dynamic BGM - mega experimental")
            .AddAction(() =>
            {
                if (ImGui.Checkbox("Enable dynamic background music", ref configuration.EnableDynamicBgm.Value))
                {

                    KamiCommon.SaveConfiguration();
                    ActivateDynamicBgmChange?.Invoke(this, configuration.EnableDynamicBgm.Value);
                }
                ImGuiComponents.HelpMarker("This will disable the game background music only inside instances. You may have to change your BGM configuration.");
            })
            .AddAction(() =>
            {
                if (ImGui.Checkbox("Apply game volume on dynamic background music", ref configuration.ApplyGameVolumeBgm.Value))
                {
                    
                    KamiCommon.SaveConfiguration();
                    BgmVolumeChange?.Invoke(this, configuration.SfxVolume.Value);
                }
            })
            .AddAction(() =>
            {
                ImGui.SetNextItemWidth(200f);
                if (ImGui.SliderInt("Background music volume", ref configuration.BgmVolume.Value, 0, 100))
                {
                    KamiCommon.SaveConfiguration();
                    BgmVolumeChange?.Invoke(this, configuration.BgmVolume.Value);
                }
            })
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
            .SameLine().AddButton("BGM test", () => Plugin.StartBgm())
            .SameLine().AddButton("Stop BGM", () => Plugin.StopBgm())
            .AddButton("BGM Rank up", () => Plugin.BgmTransitionNext())
            .SameLine().AddButton("EndCombat", () => Plugin.BgmEndCombat())
            .SameLine().AddButton("Demotion", () => Plugin.BgmDemotion())

            .Draw();
#endif
        }
    }
}
