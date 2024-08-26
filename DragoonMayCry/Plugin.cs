using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using DragoonMayCry.Audio;
using DragoonMayCry.Configuration;
using DragoonMayCry.Score;
using DragoonMayCry.Score.Action;
using DragoonMayCry.Score.Style;
using DragoonMayCry.State;
using DragoonMayCry.UI;

namespace DragoonMayCry;

public unsafe class Plugin : IDalamudPlugin
{
    [PluginService] public static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    public static ActionManager* ActionManager { get; private set; }
    public static DmcConfiguration? Configuration { get; private set; }

    private const string CommandName = "/dmc";



    public static ScoreManager? ScoreManager { get; private set; }
    public static StyleRankHandler? StyleRankHandler { get; private set; }
    public static PluginUI? PluginUi { get; private set; }

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
        playerState = PlayerState.GetInstance();
        Configuration = PluginInterface.GetPluginConfig() as DmcConfiguration ?? new DmcConfiguration();
        actionTracker = new();

        StyleRankHandler = new(actionTracker);
        ScoreManager = new(StyleRankHandler, actionTracker);
        scoreProgressBar = new(ScoreManager, StyleRankHandler, actionTracker);
        PluginUi = new(scoreProgressBar, StyleRankHandler, ScoreManager);

        Service.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "A useful message to display in /xlhelp"
        });
    }

    public void Dispose()
    {
        PluginUi?.Dispose();
        playerState.Dispose();
        scoreProgressBar.Dispose();
        actionTracker.Dispose();
        ScoreManager?.Dispose();

        Service.CommandManager.RemoveHandler(CommandName);
    }

    private void OnCommand(string command, string args)
    {
        // in response to the slash command, just toggle the display status of our main ui
        PluginUi?.ToggleConfigUI();
    }

    
}
