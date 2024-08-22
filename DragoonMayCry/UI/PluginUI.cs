using Dalamud.Interface.Windowing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Plugin;

namespace DragoonMayCry.UI
{
    public sealed class PluginUI : IDisposable
    {
        private readonly WindowSystem WindowSystem = new("DragoonMayCry");
        private ConfigWindow ConfigWindow { get; init; }

        private readonly StyleRankUI _styleRankUI = new();
        private readonly IDalamudPluginInterface pluginInterface;

        public PluginUI()
        {
            ConfigWindow = new ConfigWindow(Plugin.Configuration);

            WindowSystem.AddWindow(ConfigWindow);

            pluginInterface = Plugin.PluginInterface;
            pluginInterface.UiBuilder.Draw += DrawUI;

            // This adds a button to the plugin installer entry of this plugin which allows
            // to toggle the display status of the configuration ui
            pluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;
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
            _styleRankUI.Draw();
        }


        public void ToggleConfigUI() => ConfigWindow.Toggle();
    }
}
