using Dalamud.Interface.Windowing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Plugin;
using DragoonMayCry.State;
using DragoonMayCry.Style;
using Lumina.Excel.GeneratedSheets2;
using System.Threading;
using DragoonMayCry.Score;

namespace DragoonMayCry.UI
{
    public sealed class PluginUI : IDisposable
    {
        private readonly WindowSystem WindowSystem = new("DragoonMayCry");
        private ConfigWindow ConfigWindow { get; init; }

        private readonly StyleRankUI styleRankUI;
        private readonly IDalamudPluginInterface pluginInterface;
        private readonly PlayerState playerState;
        private Stopwatch hideRankUiStopwatch;
        public PluginUI(ScoreProgressBar scoreProgressBar, StyleRankHandler styleRankHandler, ScoreManager scoreManager)
        {
            ConfigWindow = new ConfigWindow(Plugin.Configuration);
            styleRankUI = new StyleRankUI(scoreProgressBar, styleRankHandler, scoreManager);

            WindowSystem.AddWindow(ConfigWindow);

            pluginInterface = Plugin.PluginInterface;
            pluginInterface.UiBuilder.Draw += DrawUI;

            pluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;
            this.playerState = PlayerState.Instance();
            playerState.RegisterCombatStateChangeHandler(OnCombatChange);

            hideRankUiStopwatch = new Stopwatch();


        }

        public void Dispose()
        {
            pluginInterface.UiBuilder.Draw -= DrawUI;
            pluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUI;

            WindowSystem.RemoveAllWindows();

            ConfigWindow.Dispose();

        }

        private void DrawUI()
        {
            if (hideRankUiStopwatch.IsRunning &&
                hideRankUiStopwatch.ElapsedMilliseconds > Plugin.Configuration.TimeToResetScoreAfterCombat)
            {
                hideRankUiStopwatch.Stop();
            }

            WindowSystem.Draw();
            if (CanDrawStyleRank() || Plugin.Configuration.StyleRankUiConfiguration.TestRankDisplay)
            {
                styleRankUI.Draw();
            }
            
        }

        private bool CanDrawStyleRank()
        {
            if (!Plugin.Configuration.StyleRankUiConfiguration.LockScoreWindow)
            {
                return true;
            }
            if (!playerState.IsInsideInstance &&
                !Plugin.Configuration.ActiveOutsideInstance)
            {
                return false;
            }

            return playerState.IsInCombat || hideRankUiStopwatch.IsRunning;
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
