using Dalamud.Interface.Windowing;
using System;
using System.Collections.Generic;
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

namespace DragoonMayCry.UI
{
    public sealed class PluginUI : IDisposable
    {
        private readonly WindowSystem WindowSystem = new("DragoonMayCry");
        private ConfigWindow ConfigWindow { get; init; }

        private readonly StyleRankUI _styleRankUI = new();
        private readonly IDalamudPluginInterface pluginInterface;
        private readonly PlayerState playerState;
        private Timer hideRankUiTimer;
        private bool displayRankUi = false;
        public PluginUI(PlayerState playerState)
        {
            ConfigWindow = new ConfigWindow(Plugin.Configuration);

            WindowSystem.AddWindow(ConfigWindow);

            pluginInterface = Plugin.PluginInterface;
            pluginInterface.UiBuilder.Draw += DrawUI;

            pluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;
            this.playerState = playerState;
            playerState.RegisterCombatStateChangeHandler(OnCombatChange);
            
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
            WindowSystem.Draw();
            if (displayRankUi)
            {
                _styleRankUI.Draw();
            }
            
        }

        private bool CanDrawStyleRank()
        {
            if (!playerState.IsInsideInstance &&
                !Plugin.Configuration.ActiveOutsideInstance)
            {
                return false;
            }

            return playerState.IsInCombat || playerState.IsInsideInstance;
        }

        private void OnCombatChange(object send, bool enteringCombat)
        {
            if (!enteringCombat)
            {
                hideRankUiTimer = new Timer(HideRankUi, null, Plugin.Configuration.TimeToResetScoreAfterCombat, Timeout.Infinite);
            }
            else
            {
                displayRankUi = CanDrawStyleRank();
                if (hideRankUiTimer != null)
                {
                    hideRankUiTimer.Dispose();
                }
            }
        }

        private void HideRankUi(object state)
        {
            displayRankUi = false;
            if (hideRankUiTimer != null)
            {
                hideRankUiTimer.Dispose();
            }
        }


        public void ToggleConfigUI()
        {
            ConfigWindow.Toggle();
        }
    }
}
