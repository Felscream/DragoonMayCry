using Dalamud.Game.Command;
using Dalamud.Interface.Utility.Raii;
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
using DragoonMayCry.Util;

namespace DragoonMayCry;

public unsafe class Plugin : IDalamudPlugin
{
    [PluginService] public static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    public static ActionManager* ActionManager { get; private set; }
    public static DmcConfiguration? Configuration { get; private set; }

    private const string CommandName = "/dmc";

    private readonly ScoreManager scoreManager;
    private readonly PluginUI pluginUi;
    private readonly PlayerState playerState;
    private readonly ScoreProgressBar scoreProgressBar;
    private readonly PlayerActionTracker playerActionTracker;
    private readonly StyleRankHandler styleRankHandler;

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        PluginInterface.Create<Service>();
        ActionManager =
            (ActionManager*)FFXIVClientStructs.FFXIV.Client.Game.ActionManager.Instance();
        
        playerState = PlayerState.GetInstance();
        Configuration = PluginInterface.GetPluginConfig() as DmcConfiguration ?? new DmcConfiguration();
        playerActionTracker = new();

        styleRankHandler = new(playerActionTracker);
        scoreManager = new(styleRankHandler, playerActionTracker);
        scoreProgressBar = new(scoreManager, styleRankHandler, playerActionTracker);
        pluginUi = new(scoreProgressBar, styleRankHandler, scoreManager);

        scoreProgressBar.DemotionApplied += styleRankHandler.OnDemotion;
        scoreProgressBar.Promotion += styleRankHandler.OnPromotion;

        Service.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "opens configuration menu"
        });
    }

    public static bool CanRunDmc()
    {
        var playerState = PlayerState.GetInstance();
        return JobHelper.IsCombatJob()
               && playerState.IsInCombat
               && (playerState.IsInsideInstance ||
                   Configuration!.ActiveOutsideInstance);
    }

    public void Dispose()
    {
        scoreProgressBar.Dispose();
        playerActionTracker.Dispose();
        scoreManager.Dispose();
        playerState.Dispose();
        pluginUi.Dispose();
        Service.CommandManager.RemoveHandler(CommandName);
    }

    private void OnCommand(string command, string args)
    {
        // in response to the slash command, just toggle the display status of our main ui
        pluginUi.ToggleConfigUI();
    }

    
}
