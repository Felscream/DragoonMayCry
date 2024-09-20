using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
using DragoonMayCry.Audio;
using DragoonMayCry.Audio.StyleAnnouncer;
using DragoonMayCry.Configuration;
using DragoonMayCry.Score.Model;
using ImGuiNET;
using KamiLib;
using KamiLib.Configuration;
using KamiLib.Drawing;
using System;
using System.Linq;
using System.Numerics;

namespace DragoonMayCry.UI;

public class ConfigWindow : Window
{
    public EventHandler<bool>? ActiveOutsideInstanceChange;
    public EventHandler<bool>? ToggleDynamicBgmChange;
    public EventHandler<int>? SfxVolumeChange;
    public EventHandler<int>? BgmVolumeChange;

    private readonly DmcConfigurationOne configuration;
    private readonly JobConfigurationWindow jobConfigurationWindow;
    private readonly Setting<int> decay = new(0);

    // We give this window a constant ID using ###
    // This allows for labels being dynamic, like "{FPS Counter}fps###XYZ counter window",
    // and the window ID will always be "###XYZ counter window" for ImGui
    public ConfigWindow(DmcConfigurationOne configuration, JobConfigurationWindow jobConfiguration) : base("DragoonMayCry - Configuration")
    {

        Size = new Vector2(525, 425);
        SizeCondition = ImGuiCond.Appearing;
        jobConfigurationWindow = jobConfiguration;
        this.configuration = configuration;
    }

    public override void Draw()
    {
        switch (AssetsManager.Status)
        {
            case AssetsManager.AssetsStatus.Updating:
                ImGui.Text($"Downloading additional assets...");
                break;
            case AssetsManager.AssetsStatus.FailedDownloading:
            case AssetsManager.AssetsStatus.FailedInsufficientDiskSpace:
            case AssetsManager.AssetsStatus.FailedFileIntegrity:
                DrawError();
                break;
        }
        DrawConfigMenu();
    }

    private void DrawError()
    {
        var errorMessage = AssetsManager.Status switch
        {
            AssetsManager.AssetsStatus.FailedInsufficientDiskSpace => "Not enough disk space for additional assets",
            AssetsManager.AssetsStatus.FailedDownloading => "Could not retrieve additional assets",
            AssetsManager.AssetsStatus.FailedFileIntegrity => "File integrity check failed, redownload them",
            _ => "An unexpected error occured",
        };

        ImGui.Indent();
        ImGui.Text(errorMessage);
        if (ImGui.Button("Download assets"))
        {
            AssetsManager.FetchAudioFiles();
        }
    }

    private void DrawConfigMenu()
    {

        if (AssetsManager.Status == AssetsManager.AssetsStatus.Done)
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
            .AddButton("Open job configuration", () => jobConfigurationWindow.Toggle())
            .Draw();

            var announcerValues = Enum.GetValues(typeof(AnnouncerType)).Cast<AnnouncerType>().ToList();
            InfoBox.Instance.AddTitle("Announcer")

                .AddConfigCheckbox("Enable announcer", configuration.PlaySoundEffects)
                .AddConfigCheckbox("Force announcer on blunders", configuration.ForceSoundEffectsOnBlunder)
                .AddAction(() =>
                {
                    if (ImGui.Checkbox("Apply game volume on announcer", ref configuration.ApplyGameVolumeSfx.Value))
                    {
                        KamiCommon.SaveConfiguration();
                        SfxVolumeChange?.Invoke(this, configuration.SfxVolume.Value);
                    }
                })
                .AddAction(() =>
                {
                    ImGui.SetNextItemWidth(150f);
                    if (ImGui.SliderInt("Announcer volume", ref configuration.SfxVolume.Value, 0, 300))
                    {
                        KamiCommon.SaveConfiguration();
                        SfxVolumeChange?.Invoke(this, configuration.SfxVolume.Value);
                    }
                })
                .AddString("Play each unique line only once every")
                .SameLine()
                .AddSliderInt("occurrences", configuration.PlaySfxEveryOccurrences, 1, 20, 100)
                .Draw();

            InfoBox.Instance.AddTitle("Dynamic BGM - mega experimental")
                .AddAction(() =>
                {
                    if (ImGui.Checkbox("Enable dynamic background music", ref configuration.EnableDynamicBgm.Value))
                    {
                        KamiCommon.SaveConfiguration();
                        ToggleDynamicBgmChange?.Invoke(this, configuration.EnableDynamicBgm.Value);
                    }
                    ImGuiComponents.HelpMarker("This will disable the game's background music inside instances.");
                })
                .AddConfigCheckbox("Enable muffled effect on death", configuration.EnableMuffledEffectOnDeath)
                .AddAction(() =>
                {
                    if (ImGui.Checkbox("Apply game volume on dynamic background music", ref configuration.ApplyGameVolumeBgm.Value))
                    {

                        KamiCommon.SaveConfiguration();
                        BgmVolumeChange?.Invoke(this, configuration.SfxVolume.Value);
                    }
                    ImGuiComponents.HelpMarker("Only Master volume is applied.");
                })
                .AddAction(() =>
                {
                    ImGui.SetNextItemWidth(150f);
                    if (ImGui.SliderInt("Background music volume", ref configuration.BgmVolume.Value, 0, 300))
                    {
                        KamiCommon.SaveConfiguration();
                        BgmVolumeChange?.Invoke(this, configuration.BgmVolume.Value);
                    }
                })
                .Draw();

#if DEBUG
            InfoBox.Instance.AddTitle("Debug")
                .AddButton("Dead weight", () => Plugin.StyleAnnouncerService?.PlayBlunder())
                .SameLine().AddButton("D", () => Plugin.StyleAnnouncerService?.PlayForStyle(StyleType.D))
                .SameLine().AddButton("C", () => Plugin.StyleAnnouncerService?.PlayForStyle(StyleType.C))
                .SameLine().AddButton("B", () => Plugin.StyleAnnouncerService?.PlayForStyle(StyleType.B))
                .SameLine().AddButton("A", () => Plugin.StyleAnnouncerService?.PlayForStyle(StyleType.A))
                .SameLine().AddButton("S", () => Plugin.StyleAnnouncerService?.PlayForStyle(StyleType.S))
                .SameLine().AddButton("SS", () => Plugin.StyleAnnouncerService?.PlayForStyle(StyleType.SS))
                .SameLine().AddButton("SSS", () => Plugin.StyleAnnouncerService?.PlayForStyle(StyleType.SSS))
                .SameLine().AddButton("BGM test", () => Plugin.StartBgm())
                .SameLine().AddButton("Stop BGM", () => Plugin.StopBgm())
                .AddButton("BGM Enter combat", () => Plugin.BgmTransitionNext())
                .SameLine().AddButton("BGM Rank up", () => Plugin.SimulateBgmRankChanges(Score.Model.StyleType.A, Score.Model.StyleType.S))
                .SameLine().AddButton("BGM Rank down", () => Plugin.SimulateBgmRankChanges(Score.Model.StyleType.S, Score.Model.StyleType.D))
                .SameLine().AddButton("EndCombat", () => Plugin.BgmEndCombat())
                .AddButton("Muffle", () => AudioService.Instance.ApplyMuffledEffect())
                .SameLine().AddButton("Remove muffled", () => AudioService.Instance.RemoveMuffledEffect())
                .AddSliderInt("Decay", decay, 0, 70)
                .AddButton("Apply decay", () => AudioService.Instance.ApplyDecay(decay.Value / 100f))
                .Draw();
#endif
        }
    }

    private static string GetAnnouncerTypeLabel(AnnouncerType type)
    {
        return type switch
        {
            AnnouncerType.DmC => "DmC: Devil May Cry",
            AnnouncerType.DmC5 => "Devil May Cry 5",
            AnnouncerType.DmC5Balrog => "Devil May Cry 5 / Balrog VA",
            AnnouncerType.Nico => "Nico",
            AnnouncerType.Morrison => "Morrison",
            _ => "Unknown"
        };
    }
}
