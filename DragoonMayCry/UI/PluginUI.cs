using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using DragoonMayCry.Score;
using DragoonMayCry.Score.Style;
using DragoonMayCry.State;
using System;
using System.Diagnostics;

namespace DragoonMayCry.UI
{
    public sealed class PluginUI : IDisposable
    {
        private readonly WindowSystem windowSystem = new("DragoonMayCry");
        private ConfigWindow ConfigWindow { get; init; }

        private readonly StyleRankUI styleRankUi;
        private readonly IDalamudPluginInterface pluginInterface;
        private readonly PlayerState playerState;
        private readonly Stopwatch hideRankUiStopwatch;
        private const float TimeToResetScoreAfterCombat = 10000;
        
        public PluginUI(ScoreProgressBar scoreProgressBar, StyleRankHandler styleRankHandler, ScoreManager scoreManager)
        {
            ConfigWindow = new ConfigWindow(Plugin.Configuration!, styleRankHandler);
            styleRankUi = new StyleRankUI(scoreProgressBar, styleRankHandler, scoreManager);

            windowSystem.AddWindow(ConfigWindow);

            pluginInterface = Plugin.PluginInterface;
            pluginInterface.UiBuilder.Draw += DrawUI;
            pluginInterface.UiBuilder.OpenMainUi += ToggleConfigUI;
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
        }

        private void DrawUI()
        {
            if (hideRankUiStopwatch.IsRunning &&
                hideRankUiStopwatch.ElapsedMilliseconds > TimeToResetScoreAfterCombat)
            {
                hideRankUiStopwatch.Stop();
            }

            windowSystem.Draw();
            if (CanDrawStyleRank() || Plugin.Configuration!.StyleRankUiConfiguration.TestRankDisplay)
            {
                styleRankUi.Draw();
            }
            
        }

        private bool CanDrawStyleRank()
        {
            if (!Plugin.Configuration!.StyleRankUiConfiguration.LockScoreWindow)
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
            ConfigWindow.Toggle();
        }
    }
}
