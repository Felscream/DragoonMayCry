using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using DragoonMayCry.Audio;
using DragoonMayCry.Score;
using DragoonMayCry.Score.Style;
using DragoonMayCry.State;
using KamiLib;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Diagnostics;

namespace DragoonMayCry.UI
{
    public sealed class PluginUI : IDisposable
    {
        private readonly WindowSystem windowSystem = new("DragoonMayCry");
        private ConfigWindow ConfigWindow { get; init; }
        private HowItWorksWindow HowItWorksWindow { get; init; }

        private readonly StyleRankUI styleRankUi;
        private readonly IDalamudPluginInterface pluginInterface;
        private readonly PlayerState playerState;
        private readonly Stopwatch hideRankUiStopwatch;
        private const float TimeToResetScoreAfterCombat = 10000;
        
        public PluginUI(ScoreProgressBar scoreProgressBar, StyleRankHandler styleRankHandler, ScoreManager scoreManager, FinalRankCalculator finalRankCalculator, EventHandler<bool> OnActiveOutsideInstanceChange)
        {
            ConfigWindow = new ConfigWindow(this, Plugin.Configuration!);
            ConfigWindow.ActiveOutsideInstanceChange += OnActiveOutsideInstanceChange;
            ConfigWindow.SfxVolumeChange += AudioService.Instance.OnVolumeChange;

            HowItWorksWindow = new HowItWorksWindow();

            styleRankUi = new StyleRankUI(scoreProgressBar, styleRankHandler, scoreManager, finalRankCalculator);

            KamiCommon.WindowManager.AddWindow(ConfigWindow);
            KamiCommon.WindowManager.AddWindow(HowItWorksWindow);

            pluginInterface = Plugin.PluginInterface;
            pluginInterface.UiBuilder.Draw += DrawUI;
            pluginInterface.UiBuilder.OpenMainUi += ToggleHowItWorks;
            pluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;

            this.playerState = PlayerState.GetInstance();
            playerState.RegisterCombatStateChangeHandler(OnCombatChange!);

            hideRankUiStopwatch = new Stopwatch();


        }
        public void Dispose()
        {
            pluginInterface.UiBuilder.Draw -= DrawUI;
            pluginInterface.UiBuilder.OpenMainUi -= ToggleConfigUI;
           pluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUI;

            windowSystem.RemoveAllWindows();

            ConfigWindow.Dispose();
            HowItWorksWindow.Dispose();
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
            if (!Plugin.Configuration!.LockScoreWindow.Value)
            {
                return true;
            }

            return Plugin.CanRunDmc() || hideRankUiStopwatch.IsRunning;
        }

        private void OnCombatChange(object send, bool enteringCombat)
        {
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
            if(KamiCommon.WindowManager.GetWindowOfType<ConfigWindow>() is { } window)
            {
                window.IsOpen = !window.IsOpen;
            }
        }

        public void ToggleHowItWorks()
        {
            if (KamiCommon.WindowManager.GetWindowOfType<HowItWorksWindow>() is { } window)
            {
                window.IsOpen = !window.IsOpen;
            }
        }
    }
}
