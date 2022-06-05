using System;
using System.IO;
using System.Reflection;
using System.Xml.Linq;

using Dalamud.Game;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
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
        private ChatGui ChatGui { get; init; }
        private Framework Framework { get; init; }
        private Condition Condition { get; init; }
        private AudioHandler AudioHandler { get; init; }

        private bool lastUpdateInCombat;


        public DragoonMayCry(DalamudPluginInterface pluginInterface, CommandManager commandManager, Framework frameworkP, Condition conditionP, ChatGui ChatGuiP)
        {
            PluginInterface = pluginInterface;
            CommandManager = commandManager;
            ChatGui = ChatGuiP;
            Framework = frameworkP;
            Condition = conditionP;

            string bgmPath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "cerberus.wav");
            string dAnnouncerPath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "dead_weight.wav");
            string cAnnouncerPath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "cruel.wav");
            string bAnnouncerPath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "brutal.wav");
            string aAnnouncerPath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "anarchic.wav");
            string sAnnouncerPath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "savage.wav");
            string ssAnnouncerPath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "sadistic.wav");
            string sssAnnouncerPath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "sensational.wav");
            AudioHandler = new AudioHandler(bgmPath, dAnnouncerPath, cAnnouncerPath, bAnnouncerPath, aAnnouncerPath, sAnnouncerPath, ssAnnouncerPath, sssAnnouncerPath);
            
            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);
            AudioHandler.SFXVolume = Configuration.SFXVolume;
            CommandManager.AddHandler(commandName, new CommandInfo(OnCommand) {
                HelpMessage = "Used to control the volume of the audio using \"bgm 0-100\" or \"sfx 0-100\""
            });
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
            if (lastUpdateInCombat) {
                AudioHandler.StopBGM();
            }
            CommandManager.RemoveHandler(commandName);
            Framework.Update -= OnFrameWorkUpdate;

            Configuration.Save();
        }

        private void OnFrameWorkUpdate(Framework framework) {
            var inCombat = Condition[ConditionFlag.InCombat];
            if (inCombat == lastUpdateInCombat) { 
                return;
            }
            
            lastUpdateInCombat = inCombat;
            PluginLog.Debug($"lastUpdateInCombat = {lastUpdateInCombat}, inCombat = {inCombat}");
            if (inCombat) {
                PluginLog.Debug("Entering combat");
                AudioHandler.PlayBGM();
            } else { 
                PluginLog.Debug("Leaving Combat");
                AudioHandler.StopBGM();
            }
                
             
        }

        private void SetSFX(string volume) {
            try {
                var newVol = int.Parse(volume) / 100f;
                PluginLog.Debug($"{Name}: Setting sfx volume to {newVol}");
                this.AudioHandler.SFXVolume = newVol;
                this.Configuration.SFXVolume = newVol;
                this.ChatGui.Print($"SFX Volume set to {volume}%");
            } catch (Exception) {
                ChatGui.PrintError("Please use a number between 0-100");
            }
        }

        private void SetBGM(string volume) {
            try {
                var newVol = int.Parse(volume) / 100f;
                PluginLog.Debug($"{Name}: Setting bgm volume to {newVol}");
                this.AudioHandler.BGMVolume = newVol;
                this.Configuration.BGMVolume = newVol;
                this.ChatGui.Print($"BGM Volume set to {volume}%");
            } catch (Exception) {
                ChatGui.PrintError("Please use a number between 0-100");
            }
        }

        private void OnCommand(string command, string args) {
            PluginLog.Debug("{Command} - {Args}", command, args);
            var argList = args.Split(' ');

            PluginLog.Debug(argList.Length.ToString());

            if (argList.Length == 0)
                return;


            switch (argList[0]) {
                case "sfx":
                    if (argList.Length != 2)
                        return;
                    SetSFX(argList[1]);
                    break;
                case "bgm":
                    if (argList.Length != 2)
                        return;
                    SetBGM(argList[1]);
                    break;
                case "":
                    ChatGui.PrintError("Please use \"/dmc [bgm, sfx] <num>\" to control volume");
                    break;
                default:
                    break;
            }
        }
    }

   
}
