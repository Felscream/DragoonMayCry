using System;
using System.IO;


using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Party;

using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Hooking;
using Dalamud.Logging;
using Dalamud.Plugin;
using DragoonMayCry.Audio;
using DragoonMayCry.Style;
using ImGuiScene;
using static DragoonMayCry.Style.Structures;

using Condition = Dalamud.Game.ClientState.Conditions.Condition;
using Character = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;
using Style = DragoonMayCry.Style.StyleRank;

namespace DragoonMayCry
{
    public unsafe class DragoonMayCry : IDalamudPlugin {
        public string Name => "Dragoon May Cry";

        private const string commandName = "/dmc";

        private DalamudPluginInterface PluginInterface { get; init; }
        private CommandManager CommandManager { get; init; }
        private Configuration Configuration { get; init; }
        private DragoonMayCryUI PluginUi { get; init; }
        private ChatGui ChatGui { get; init; }
        private Framework Framework { get; init; }
        private DragoonMayCryUI DragoonMayCryUI {get; init;}

        private Condition Condition { get; init; }
        private AudioHandler AudioHandler { get; init; }
        private StyleRankHandler StyleRankHandler { get; init; }
        private PartyList PartyList { get; init; }

        private readonly TextureWrap dTexture;
        private readonly TextureWrap cTexture;
        private readonly TextureWrap bTexture;
        private readonly TextureWrap aTexture;
        private readonly TextureWrap sTexture;
        private readonly TextureWrap ssTexture;
        private readonly TextureWrap sssTexture;

        private ClientState clientState;
        private PlayerState PlayerState { get; } = new();

        private bool currenlyInCombat;
        private bool died;
        private delegate void ReceiveActionEffectDelegate(uint sourceId, Character* sourceCharacter, IntPtr pos, EffectHeader* effectHeader, EffectEntry* effectArray, ulong* effectTail);
        private Hook<ReceiveActionEffectDelegate> receiveActionHook;

        public DragoonMayCry(DalamudPluginInterface pluginInterface, 
            CommandManager commandManager, 
            Framework frameworkP, 
            Condition conditionP, 
            ChatGui ChatGuiP,
            SigScanner scanner,
            ClientState client,
            PartyList partyList) {
            PluginInterface = pluginInterface;
            CommandManager = commandManager;
            ChatGui = ChatGuiP;
            Framework = frameworkP;
            Condition = conditionP;
            clientState = client;
            partyList = partyList;

            string bgmPath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "cerberus.wav");
            string dAnnouncerPath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "dead_weight.wav");
            string cAnnouncerPath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "cruel.wav");
            string bAnnouncerPath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "brutal.wav");
            string aAnnouncerPath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "anarchic.wav");
            string sAnnouncerPath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "savage.wav");
            string ssAnnouncerPath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "sadistic.wav");
            string sssAnnouncerPath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "sensational.wav");
            AudioHandler = new AudioHandler(bgmPath, dAnnouncerPath, cAnnouncerPath, bAnnouncerPath, aAnnouncerPath, sAnnouncerPath, ssAnnouncerPath, sssAnnouncerPath);

            dTexture = PluginInterface.UiBuilder.LoadImage(Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "d.png"));
            cTexture = PluginInterface.UiBuilder.LoadImage(Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "c.png"));
            bTexture = PluginInterface.UiBuilder.LoadImage(Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "b.png"));
            aTexture = PluginInterface.UiBuilder.LoadImage(Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "a.png"));
            sTexture = PluginInterface.UiBuilder.LoadImage(Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "s.png"));
            ssTexture = PluginInterface.UiBuilder.LoadImage(Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "ss.png"));
            sssTexture = PluginInterface.UiBuilder.LoadImage(Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "sss.png"));
            TextureWrap gauge = PluginInterface.UiBuilder.LoadImage(Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "gauge.png"));
            StyleRankHandler = new(dTexture, cTexture, bTexture, aTexture, sTexture, ssTexture, sssTexture);
            StyleRankHandler.OnStyleChange += OnStyleChange;
            StyleRankHandler.OnProgressChange += OnProgressChange;
            
            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);
            AudioHandler.SFXVolume = Configuration.SFXVolume;
            AudioHandler.BGMVolume = Configuration.BGMVolume;
            CommandManager.AddHandler(commandName, new CommandInfo(OnCommand) {
                HelpMessage = "Used to control the volume of the audio using \"bgm 0-100\" or \"sfx 0-100\""
            });

            DragoonMayCryUI = new(Configuration, gauge);
            this.PluginInterface.UiBuilder.Draw += this.Draw;
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

            var receiveActionEffectFuncPtr = scanner.ScanText("4C 89 44 24 ?? 55 56 57 41 54 41 55 41 56 48 8D 6C 24");
            receiveActionHook = new Hook<ReceiveActionEffectDelegate>(receiveActionEffectFuncPtr, (ReceiveActionEffectDelegate)ReceiveActionEffect);
            receiveActionHook.Enable();
            Framework.Update += OnFrameWorkUpdate;
        }

        public void Dispose() {
            StyleRankHandler.OnStyleChange -= OnStyleChange;
            StyleRankHandler.OnProgressChange -= OnProgressChange;
            if (currenlyInCombat) {
                AudioHandler.StopBGM();
            }
            CommandManager.RemoveHandler(commandName);
            Framework.Update -= OnFrameWorkUpdate;

            receiveActionHook?.Dispose();
            DragoonMayCryUI.Dispose();
            Configuration.Save();
        }

        private void Draw() => DragoonMayCryUI.Draw();

        private void ReceiveActionEffect(uint sourceId, Character* sourceCharacter, IntPtr pos, EffectHeader* effectHeader, EffectEntry* effectArray, ulong* effectTail) {
            
            StyleRankHandler.Update(sourceId, GetCharacterId(), sourceCharacter, pos, effectHeader, effectArray, effectTail, currenlyInCombat);
            
            receiveActionHook.Original(sourceId, sourceCharacter, pos, effectHeader, effectArray, effectTail);
        }

        private void OnFrameWorkUpdate(Framework framework) {
            PlayerState.ServicesUpdate(clientState, PartyList, Condition);
            PlayerState.StateUpdate();
            var inCombat = PlayerState.IsInCombat();
            died = PlayerState.Died();
            if (inCombat == currenlyInCombat) {
                return;
            }

            currenlyInCombat = inCombat;
            if (inCombat) {
                AudioHandler.PlayBGM();
            } else {
                AudioHandler.StopBGM();
            }

            if (died) {
                StyleRankHandler.Died();
            }
        }

        private void OnStyleChange(StyleRank currentStyle, StyleRank previousStyle) {
            DragoonMayCryUI.CurrentRank = currentStyle;
            if (!currenlyInCombat) { return; }

            if (died && currentStyle.StyleType == StyleType.D || previousStyle.NextStyle != null && previousStyle.NextStyle.Equals(currentStyle)) {
                AudioHandler.PlaySFX(currentStyle.StyleType);
            }
        }

        private void OnProgressChange(StyleRank currentStyle, float progress) {
            DragoonMayCryUI.CurrentRank = currentStyle;
            DragoonMayCryUI.Progress = progress;
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

        private uint GetCharacterId() {
            return clientState.LocalPlayer?.ObjectId ?? 0;
        }
    }

   
}
