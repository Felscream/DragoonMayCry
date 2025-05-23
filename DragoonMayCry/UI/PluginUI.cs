using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using DragoonMayCry.Audio;
using DragoonMayCry.Audio.BGM;
using DragoonMayCry.Audio.StyleAnnouncer;
using DragoonMayCry.Record;
using DragoonMayCry.Score;
using DragoonMayCry.Score.Action;
using DragoonMayCry.Score.Rank;
using DragoonMayCry.State;
using KamiLib;
using System;
using System.Diagnostics;

namespace DragoonMayCry.UI
{
    public sealed class PluginUI : IDisposable
    {
        private const float TimeToResetScoreAfterCombat = 10000;
        private readonly FinalRankCalculator finalRankCalculator;
        private readonly Stopwatch hideRankUiStopwatch;
        private readonly PlayerState playerState;
        private readonly IDalamudPluginInterface pluginInterface;

        private readonly StyleRankUi styleRankUi;
        private readonly WindowSystem windowSystem = new("DragoonMayCry");

        public PluginUI(
            ScoreProgressBar scoreProgressBar,
            StyleRankHandler styleRankHandler,
            ScoreManager scoreManager,
            FinalRankCalculator finalRankCalculator,
            StyleAnnouncerService styleAnnouncerService,
            DynamicBgmService dynamicBgmService,
            PlayerActionTracker playerActionTracker,
            RecordService recordService,
            HitCounter hitCounter)
        {
            this.finalRankCalculator = finalRankCalculator;
            JobConfigurationWindow = new JobConfigurationWindow(Plugin.Configuration!);
            JobConfigurationWindow.JobAnnouncerTypeChange += styleAnnouncerService.OnAnnouncerTypeChange;
            JobConfigurationWindow.EnabledForJobChange += dynamicBgmService.OnJobEnableChange;

            BgmDutyBlacklistWindow = new BgmDutyBlacklistConfigurationWindow(Plugin.Configuration!);
            BgmDutyBlacklistWindow.BgmBlacklistChanged += dynamicBgmService.OnBgmBlacklistChanged;

            HowItWorksWindow = new HowItWorksWindow();

            ConfigWindow = new ConfigWindow(Plugin.Configuration!, JobConfigurationWindow, BgmDutyBlacklistWindow);
            ConfigWindow.ActiveOutsideInstanceChange += Plugin.OnActiveOutsideInstanceConfChange;
            ConfigWindow.ToggleDynamicBgmChange += dynamicBgmService.ToggleDynamicBgm;
            ConfigWindow.MuffledOnDeathChange += dynamicBgmService.OnMuffledOnDeathChange;
            ConfigWindow.SfxVolumeChange += AudioService.Instance.OnSfxVolumeChange;
            ConfigWindow.BgmVolumeChange += AudioService.Instance.OnBgmVolumeChange;

            CharacterRecordWindow = new CharacterRecordWindow(recordService, ConfigWindow, HowItWorksWindow);

            styleRankUi = new StyleRankUi(scoreProgressBar, styleRankHandler, scoreManager, finalRankCalculator,
                                          playerActionTracker, hitCounter);

            KamiCommon.WindowManager.AddWindow(JobConfigurationWindow);
            KamiCommon.WindowManager.AddWindow(ConfigWindow);
            KamiCommon.WindowManager.AddWindow(HowItWorksWindow);
            KamiCommon.WindowManager.AddWindow(CharacterRecordWindow);
            KamiCommon.WindowManager.AddWindow(BgmDutyBlacklistWindow);

            pluginInterface = Plugin.PluginInterface;
            pluginInterface.UiBuilder.Draw += DrawUi;
            pluginInterface.UiBuilder.OpenMainUi += ToggleCharacterRecords;
            pluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;

            playerState = PlayerState.GetInstance();
            playerState.RegisterCombatStateChangeHandler(OnCombatChange!);

            hideRankUiStopwatch = new Stopwatch();
        }
        private ConfigWindow ConfigWindow { get; init; }
        private HowItWorksWindow HowItWorksWindow { get; init; }
        private JobConfigurationWindow JobConfigurationWindow { get; init; }
        private CharacterRecordWindow CharacterRecordWindow { get; init; }
        private BgmDutyBlacklistConfigurationWindow BgmDutyBlacklistWindow { get; init; }

        public void Dispose()
        {
            styleRankUi.Dispose();
            pluginInterface.UiBuilder.Draw -= DrawUi;
            pluginInterface.UiBuilder.OpenMainUi -= ToggleCharacterRecords;
            pluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
            windowSystem.RemoveAllWindows();
        }

        private void DrawUi()
        {
            if (hideRankUiStopwatch.IsRunning &&
                hideRankUiStopwatch.ElapsedMilliseconds > TimeToResetScoreAfterCombat)
            {
                hideRankUiStopwatch.Stop();
            }

            windowSystem.Draw();
            if (CanDrawStyleRank())
            {
                styleRankUi.Draw();
            }
        }

        private bool CanDrawStyleRank()
        {
            if (Plugin.Configuration!.HideInCutscenes && PlayerState.GetInstance().IsInCutscene)
            {
                return false;
            }

            if (!Plugin.Configuration!.LockScoreWindow)
            {
                return true;
            }

            return Plugin.CanRunDmc() || hideRankUiStopwatch.IsRunning;
        }

        private void OnCombatChange(object send, bool enteringCombat)
        {
            if (!finalRankCalculator.CanDisplayFinalRank())
            {
                hideRankUiStopwatch.Reset();
                return;
            }

            if (!enteringCombat)
            {
                hideRankUiStopwatch.Restart();
            }
            else
            {
                hideRankUiStopwatch.Reset();
            }
        }

        public static void ToggleConfigUi()
        {
            if (KamiCommon.WindowManager.GetWindowOfType<ConfigWindow>() is { } window)
            {
                window.IsOpen = !window.IsOpen;
            }
        }

        public static void ToggleCharacterRecords()
        {
            if (KamiCommon.WindowManager.GetWindowOfType<CharacterRecordWindow>() is { } window)
            {
                window.IsOpen = !window.IsOpen;
            }
        }
    }
}
