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
using DragoonMayCry.Score.Model;
using DragoonMayCry.Score.Style.Announcer;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using System.Collections.Generic;
using System.Linq;

namespace DragoonMayCry.UI;

public class ConfigWindow : Window, IDisposable
{
    public EventHandler<bool>? ActiveOutsideInstanceChange;
    public EventHandler<bool>? ToggleDynamicBgmChange;
    public EventHandler<int>? SfxVolumeChange;
    public EventHandler<int>? BgmVolumeChange;
    public EventHandler<AnnouncerType> AnnouncerTypeChange;
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
            case AssetsManager.Status.Updating:
                ImGui.Text($"Downloading additional assets...");
                break;
            case AssetsManager.Status.FailedDownloading:
            case AssetsManager.Status.FailedInsufficientDiskSpace:
            case AssetsManager.Status.FailedFileIntegrity:
                DrawError();
                break;
        }
        DrawConfigMenu();
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

        var announcerValues = Enum.GetValues(typeof(AnnouncerType)).Cast<AnnouncerType>().ToList();
        InfoBox.Instance.AddTitle("SFX")
            .AddConfigCheckbox("Play sound effects", configuration.PlaySoundEffects)
            .AddAction(() =>
            {
                ImGui.SetNextItemWidth(200);
                if(ImGui.BeginCombo("Style rank announcer", GetAnnouncerTypeLabel(configuration.Announcer.Value)))
                {
                    for(int i = 0; i < announcerValues.Count; i++)
                    {
                        if (ImGui.Selectable(GetAnnouncerTypeLabel(announcerValues[i]), configuration.Announcer.Value.Equals(announcerValues[i]))){
                            configuration.Announcer.Value = announcerValues[i];
                            KamiCommon.SaveConfiguration();
                            AnnouncerTypeChange?.Invoke(this, announcerValues[i]);
                        }
                    }
                    ImGui.EndCombo();
                }
            })
            .SameLine()
            .AddIconButton("play-sfx", Dalamud.Interface.FontAwesomeIcon.PlayCircle, () => Plugin.StyleAnnouncerService.PlayRandomAnnouncerLine())
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
                if (ImGui.SliderInt("Sound effect volume", ref configuration.SfxVolume.Value, 0, 200))
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
                    ToggleDynamicBgmChange?.Invoke(this, configuration.EnableDynamicBgm.Value);
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
                ImGuiComponents.HelpMarker("Only Master volume is applied.");
            })
            .AddAction(() =>
            {
                ImGui.SetNextItemWidth(200f);
                if (ImGui.SliderInt("Background music volume", ref configuration.BgmVolume.Value, 0, 200))
                {
                    KamiCommon.SaveConfiguration();
                    BgmVolumeChange?.Invoke(this, configuration.BgmVolume.Value);
                }
            })
            .Draw();

#if DEBUG
        InfoBox.Instance.AddTitle("Debug")
            .AddButton("Dead weight", () => Plugin.StyleAnnouncerService.PlayBlunder())
            .SameLine().AddButton("D", () => Plugin.StyleAnnouncerService.PlayForStyle(StyleType.D))
            .SameLine().AddButton("C", () => Plugin.StyleAnnouncerService.PlayForStyle(StyleType.C))
            .SameLine().AddButton("B", () => Plugin.StyleAnnouncerService.PlayForStyle(StyleType.B))
            .SameLine().AddButton("A", () => Plugin.StyleAnnouncerService.PlayForStyle(StyleType.A))
            .SameLine().AddButton("S", () => Plugin.StyleAnnouncerService.PlayForStyle(StyleType.S))
            .SameLine().AddButton("SS", () => Plugin.StyleAnnouncerService.PlayForStyle(StyleType.SS))
            .SameLine().AddButton("SSS", () => Plugin.StyleAnnouncerService.PlayForStyle(StyleType.SSS))
            .SameLine().AddButton("BGM test", () => Plugin.StartBgm())
            .SameLine().AddButton("Stop BGM", () => Plugin.StopBgm())
            .AddButton("BGM Enter combat", () => Plugin.BgmTransitionNext())
            .SameLine().AddButton("BGM Rank up", () => Plugin.SimulateBgmRankChanges(Score.Model.StyleType.A, Score.Model.StyleType.S))
            .SameLine().AddButton("BGM Rank down", () => Plugin.SimulateBgmRankChanges(Score.Model.StyleType.S, Score.Model.StyleType.D))
            .SameLine().AddButton("EndCombat", () => Plugin.BgmEndCombat())

            .Draw();
#endif
        }
    }

    private string GetAnnouncerTypeLabel(AnnouncerType type)
    {
        return type switch
        {
            AnnouncerType.DmC => "DmC: Devil May Cry",
            AnnouncerType.DmC5 => "Devil May Cry 5",
            AnnouncerType.DmC5Balrog => "Devil May Cry 5 / Balrog VA",
            AnnouncerType.Nico => "Nico",
            AnnouncerType.Morrison => "Morrison"
        };
    }
}
