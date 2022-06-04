using System.IO;
using System.Reflection;
using Dalamud.Game;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using DragoonMayCry.Audio;
using Condition = Dalamud.Game.ClientState.Conditions.Condition;

namespace DragoonMayCry
{
    public class DragoonMayCry : IDalamudPlugin {
        public string Name => "Dragoon May Cry";

        private const string commandName = "/dmc";

        private DalamudPluginInterface PluginInterface { get; init; }
        private CommandManager CommandManager { get; init; }
        private Configuration Configuration { get; init; }
        private PluginUI PluginUi { get; init; }
        private Framework Framework { get; init; }
        private Condition Condition { get; init; }
        private AudioHandler AudioHandler { get; init; }

        private bool lastUpdateInCombat;

        private string bgmPath;


        public DragoonMayCry(DalamudPluginInterface pluginInterface, CommandManager commandManager, Framework frameworkP, Condition conditionP)
        {
            PluginInterface = pluginInterface;
            CommandManager = commandManager;
            Framework = frameworkP;
            Condition = conditionP;

            bgmPath = new(Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "cerberus.mp3"));
            AudioHandler = new(bgmPath, "", "", "", "", "", "", "");
            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);

            // you might normally want to embed resources and load them from the manifest stream
            /*var imagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");
            var goatImage = this.PluginInterface.UiBuilder.LoadImage(imagePath);
            this.PluginUi = new PluginUI(this.Configuration, goatImage);

            this.CommandManager.AddHandler(commandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "A useful message to display in /xlhelp"
            });*/

            //this.PluginInterface.UiBuilder.Draw += DrawUI;
            //this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
            Framework.Update += OnFrameWorkUpdate;
        }

        public void Dispose()
        {
            this.PluginUi.Dispose();
            this.CommandManager.RemoveHandler(commandName);
        }

        private void OnCommand(string command, string args)
        {
            // in response to the slash command, just display our main ui
            this.PluginUi.Visible = true;
        }

        private void DrawUI()
        {
            this.PluginUi.Draw();
        }

        private void DrawConfigUI()
        {
            this.PluginUi.SettingsVisible = true;
        }

        private void OnFrameWorkUpdate(Framework framework) {
            var inCombat = Condition[ConditionFlag.InCombat];
            if (inCombat && !lastUpdateInCombat) {
                // start BGM
                PluginLog.Debug("Combat started");
                AudioHandler.PlaySound(AudioTrigger.CombatStart);
            } else if (lastUpdateInCombat && !inCombat) {
                // stop BGM
                PluginLog.Debug("Combat ended");
                AudioHandler.StopBGM();
            }

            lastUpdateInCombat = inCombat;
        }
    }
}
