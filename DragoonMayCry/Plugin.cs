using Dalamud.Game.Command;
using Dalamud.Game.Gui.FlyText;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using DragoonMayCry.Configuration;
using DragoonMayCry.Score;
using DragoonMayCry.State;
using DragoonMayCry.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using DragoonMayCry.Style;
using DragoonMayCry.Util;
using Action = Lumina.Excel.GeneratedSheets.Action;
using DragoonMayCry.Score.Action;

namespace DragoonMayCry;

public unsafe class Plugin : IDalamudPlugin
{
    [PluginService] public static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    public static ActionManager* ActionManager { get; private set; }
    public static DmcConfiguration? Configuration { get; private set; } = null;

    private const string CommandName = "/dmc";



    public static ScoreManager ScoreManager { get; private set; } = null;
    public static StyleRankHandler StyleRankHandler { get; private set; } = null;
    public static PluginUI PluginUI { get; private set; } = null;

    private readonly IPluginLog logger;
    private readonly PlayerState playerState;
    private readonly ScoreProgressBar scoreProgressBar;
    private readonly ActionTracker actionTracker;

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        PluginInterface.Create<Service>();
        ActionManager =
            (ActionManager*)FFXIVClientStructs.FFXIV.Client.Game.ActionManager.Instance();
        logger = Service.Log;
        playerState = PlayerState.Instance();
        Configuration = PluginInterface.GetPluginConfig() as DmcConfiguration ?? new DmcConfiguration();
        actionTracker = new();

        StyleRankHandler = new(actionTracker);
        ScoreManager = new(StyleRankHandler);
        scoreProgressBar = new(ScoreManager, StyleRankHandler);
        PluginUI = new(scoreProgressBar, StyleRankHandler, ScoreManager);
        


        Service.ClientState.Logout += ScoreManager.OnLogout;

        Service.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "A useful message to display in /xlhelp"
        });
    }

    public void Dispose()
    {
        PluginUI.Dispose();
        playerState.Dispose();
        scoreProgressBar.Dispose();
        actionTracker.Dispose();

        Service.ClientState.Logout -= ScoreManager.OnLogout;

        Service.CommandManager.RemoveHandler(CommandName);
    }

    private void OnCommand(string command, string args)
    {
        // in response to the slash command, just toggle the display status of our main ui
        PluginUI.ToggleConfigUI();
    }

    
}
