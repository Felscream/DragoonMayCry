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
        private readonly WindowSystem windowSystem = new("DragoonMayCry");
        private ConfigWindow ConfigWindow { get; init; }
        private HowItWorksWindow HowItWorksWindow { get; init; }
        private JobConfigurationWindow JobConfigurationWindow { get; init; }
        private CharacterRecordWindow CharacterRecordWindow { get; init; }

        private readonly StyleRankUI styleRankUi;
        private readonly FinalRankCalculator finalRankCalculator;
        private readonly IDalamudPluginInterface pluginInterface;
        private readonly PlayerState playerState;
        private readonly Stopwatch hideRankUiStopwatch;
        private const float TimeToResetScoreAfterCombat = 10000;

        public PluginUI(ScoreProgressBar scoreProgressBar,
            StyleRankHandler styleRankHandler,
            ScoreManager scoreManager,
            FinalRankCalculator finalRankCalculator,
            StyleAnnouncerService styleAnnouncerService,
            DynamicBgmService dynamicBgmService,
            PlayerActionTracker playerActionTracker,
            RecordService recordService)
        {
            this.finalRankCalculator = finalRankCalculator;
            JobConfigurationWindow = new(Plugin.Configuration!);
            JobConfigurationWindow.JobAnnouncerTypeChange += styleAnnouncerService.OnAnnouncerTypeChange;
            JobConfigurationWindow.EnabledForJobChange += dynamicBgmService.OnJobEnableChange;

            HowItWorksWindow = new HowItWorksWindow();



            ConfigWindow = new ConfigWindow(Plugin.Configuration!, JobConfigurationWindow);
            ConfigWindow.ActiveOutsideInstanceChange += Plugin.OnActiveOutsideInstanceConfChange;
            ConfigWindow.ToggleDynamicBgmChange += dynamicBgmService.ToggleDynamicBgm;
            ConfigWindow.MuffledOnDeathChange += dynamicBgmService.OnMuffledOnDeathChange;
            ConfigWindow.SfxVolumeChange += AudioService.Instance.OnSfxVolumeChange;
            ConfigWindow.BgmVolumeChange += AudioService.Instance.OnBgmVolumeChange;

            CharacterRecordWindow = new(recordService, ConfigWindow, HowItWorksWindow);

            styleRankUi = new StyleRankUI(scoreProgressBar, styleRankHandler, scoreManager, finalRankCalculator, playerActionTracker);

            KamiCommon.WindowManager.AddWindow(JobConfigurationWindow);
            KamiCommon.WindowManager.AddWindow(ConfigWindow);
            KamiCommon.WindowManager.AddWindow(HowItWorksWindow);
            KamiCommon.WindowManager.AddWindow(CharacterRecordWindow);

            pluginInterface = Plugin.PluginInterface;
            pluginInterface.UiBuilder.Draw += DrawUI;
            pluginInterface.UiBuilder.OpenMainUi += ToggleCharacterRecords;
            pluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;

            this.playerState = PlayerState.GetInstance();
            playerState.RegisterCombatStateChangeHandler(OnCombatChange!);

            hideRankUiStopwatch = new Stopwatch();


        }
        public void Dispose()
        {
            pluginInterface.UiBuilder.Draw -= DrawUI;
            pluginInterface.UiBuilder.OpenMainUi -= ToggleCharacterRecords;
            pluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUI;

            windowSystem.RemoveAllWindows();
        }

        private void DrawUI()
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

        public void ToggleConfigUI()
        {
            if (KamiCommon.WindowManager.GetWindowOfType<ConfigWindow>() is { } window)
            {
                window.IsOpen = !window.IsOpen;
            }
        }

        public void ToggleCharacterRecords()
        {
            if (KamiCommon.WindowManager.GetWindowOfType<CharacterRecordWindow>() is { } window)
            {
                window.IsOpen = !window.IsOpen;
            }
        }
    }
}
