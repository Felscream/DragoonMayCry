using Dalamud.Interface.Windowing;
using DragoonMayCry.Windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DragoonMayCry.UI
{
    public sealed class PluginUI : IDisposable
    {
        private readonly WindowSystem WindowSystem = new("DragoonMayCry");
        private ConfigWindow ConfigWindow { get; init; }
        private MainWindow MainWindow { get; init; }

        private readonly StyleRankUI _styleRankUI = new();

        public PluginUI(Plugin plugin)
        {
            // you might normally want to embed resources and load them from the manifest stream
            var goatImagePath = Path.Combine(Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");

            ConfigWindow = new ConfigWindow(Plugin.Configuration);
            MainWindow = new MainWindow(plugin, Plugin.Configuration, goatImagePath);

            WindowSystem.AddWindow(ConfigWindow);
            WindowSystem.AddWindow(MainWindow);

            Plugin.PluginInterface.UiBuilder.Draw += DrawUI;

            // This adds a button to the plugin installer entry of this plugin which allows
            // to toggle the display status of the configuration ui
            Plugin.PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;

            // Adds another button that is doing the same but for the main ui of the plugin
            Plugin.PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;
        }

        public void Dispose()
        {
            Plugin.PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUI;
            Plugin.PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUI;

            WindowSystem.RemoveAllWindows();

            ConfigWindow.Dispose();
            MainWindow.Dispose();

        }

        private void DrawUI()
        {
            WindowSystem.Draw();
            _styleRankUI.Draw();
        }


        public void ToggleConfigUI() => ConfigWindow.Toggle();
        public void ToggleMainUI() => MainWindow.Toggle();
    }
}
