using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
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
    public EventHandler<bool>? MuffledOnDeathChange;

    private readonly DmcConfigurationOne configuration;
    private readonly JobConfigurationWindow jobConfigurationWindow;
    private readonly HowItWorksWindow howItWorksWindow;
    private readonly Setting<int> decay = new(0);

    // We give this window a constant ID using ###
    // This allows for labels being dynamic, like "{FPS Counter}fps###XYZ counter window",
    // and the window ID will always be "###XYZ counter window" for ImGui
    public ConfigWindow(DmcConfigurationOne configuration, JobConfigurationWindow jobConfiguration, HowItWorksWindow howItWorks) : base("DragoonMayCry - Configuration")
    {
        Size = new Vector2(525, 470);
        SizeCondition = ImGuiCond.Appearing;
        howItWorksWindow = howItWorks;
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
            AssetsManager.VerifyAndUpdateAssets();
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
                var cursorPos = ImGui.GetCursorPos();
                if (ImGui.Checkbox("", ref configuration.ActiveOutsideInstance.Value))
                {
                    KamiCommon.SaveConfiguration();
                    ActiveOutsideInstanceChange?.Invoke(this, configuration.ActiveOutsideInstance.Value);
                }
                AddLabel("Active outside instance", cursorPos);
            })
            .AddConfigCheckbox("Output final rank to chat", configuration.EnabledFinalRankChatLogging, "The message will be sent in the echo channel")
            .AddButton("How it works", () => howItWorksWindow.Toggle())
            .AddButton("Open job configuration", () => jobConfigurationWindow.Toggle())
            .Draw();

            var announcerValues = Enum.GetValues(typeof(AnnouncerType)).Cast<AnnouncerType>().ToList();
            InfoBox.Instance.AddTitle("Announcer")

                .AddConfigCheckbox("Enable announcer", configuration.PlaySoundEffects)
                .AddConfigCheckbox("Force announcer on blunders", configuration.ForceSoundEffectsOnBlunder)
                .AddAction(() =>
                {
                    var cursorPos = ImGui.GetCursorPos();
                    if (ImGui.Checkbox("##AnnouncerGameVolume", ref configuration.ApplyGameVolumeSfx.Value))
                    {
                        KamiCommon.SaveConfiguration();
                        SfxVolumeChange?.Invoke(this, configuration.SfxVolume.Value);
                    }
                    AddLabel("Apply game volume on announcer", cursorPos);
                })
                .AddAction(() =>
                {
                    ImGui.SetNextItemWidth(150f);
                    if (ImGui.SliderInt("Announcer volume", ref configuration.SfxVolume.Value, 0, 400))
                    {
                        KamiCommon.SaveConfiguration();
                        SfxVolumeChange?.Invoke(this, configuration.SfxVolume.Value);
                    }
                })
                .AddString("Play each unique line only once every")
                .SameLine()
                .AddSliderInt("occurrences", configuration.PlaySfxEveryOccurrences, 1, 20, 100)
                .Draw();

            InfoBox.Instance.AddTitle("Dynamic BGM")
                .AddAction(() =>
                {
                    var cursorPosition = ImGui.GetCursorPos();
                    if (ImGui.Checkbox("##EnableDynBgm", ref configuration.EnableDynamicBgm.Value))
                    {
                        KamiCommon.SaveConfiguration();
                        ToggleDynamicBgmChange?.Invoke(this, configuration.EnableDynamicBgm.Value);
                    }
                    AddLabel("Enable dynamic BGM", cursorPosition);
                    ImGuiComponents.HelpMarker("Only inside duties.\nThis will disable the game's background music inside instances.\n Check the job configuration window to select a BGM, they are set to off by default. \n You can use this checkbox to disable dynamic BGM if things go terribly wrong.");
                })
                .AddAction(() =>
                {
                    var cursorPosition = ImGui.GetCursorPos();
                    if (ImGui.Checkbox("##EnableMuffledOnDeath", ref configuration.EnableMuffledEffectOnDeath.Value))
                    {
                        KamiCommon.SaveConfiguration();
                        MuffledOnDeathChange?.Invoke(this, configuration.EnableMuffledEffectOnDeath.Value);
                    }
                    AddLabel("Enable muffled effect on death", cursorPosition);
                })
                .AddAction(() =>
                {
                    var cursorPosition = ImGui.GetCursorPos();
                    if (ImGui.Checkbox("##BgmGameVolume", ref configuration.ApplyGameVolumeBgm.Value))
                    {
                        KamiCommon.SaveConfiguration();
                        BgmVolumeChange?.Invoke(this, configuration.SfxVolume.Value);
                    }
                    AddLabel("Apply game volume on dynamic background music", cursorPosition);
                    ImGuiComponents.HelpMarker("Only Master volume is applied.");
                })
                .AddAction(() =>
                {
                    ImGui.SetNextItemWidth(150f);
                    if (ImGui.SliderInt("Background music volume", ref configuration.BgmVolume.Value, 0, 400))
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
                .AddButton("Muffle", () => AudioService.Instance.ApplyDeathEffect())
                .SameLine().AddButton("Remove muffled", () => AudioService.Instance.RemoveDeathEffect())
                .AddSliderInt("Decay", decay, 0, 70)
                .AddButton("Apply decay", () => AudioService.Instance.ApplyDecay(decay.Value / 100f))
                .AddButton("Char id", () => Service.Log.Debug($"{Service.ClientState.LocalContentId}"))
                .Draw();
#endif
        }
    }

    public static void AddLabel(string label, Vector2 cursorPosition)
    {
        var spacing = ImGui.GetStyle().ItemSpacing;
        cursorPosition += spacing;
        ImGui.SetCursorPos(cursorPosition with { X = cursorPosition.X + 27.0f * ImGuiHelpers.GlobalScale });

        ImGui.TextUnformatted(label);
    }
}
