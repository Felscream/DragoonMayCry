using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using DragoonMayCry.Configuration;
using DragoonMayCry.Data;
using DragoonMayCry.Score;
using DragoonMayCry.Score.Action;
using DragoonMayCry.Score.Style;
using DragoonMayCry.State;
using DragoonMayCry.UI;

namespace DragoonMayCry;

public unsafe class Plugin : IDalamudPlugin
{
    [PluginService] public static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    public static DmcConfiguration? Configuration { get; private set; }

    private const string CommandName = "/dmc";

    private readonly ScoreManager scoreManager;
    private readonly PluginUI pluginUi;
    private readonly PlayerState playerState;
    private readonly ScoreProgressBar scoreProgressBar;
    private readonly PlayerActionTracker playerActionTracker;
    private readonly StyleRankHandler styleRankHandler;
    private readonly FinalRankCalculator finalRankCalculator;
    private static bool IsCombatJob = false;

    public Plugin()
    {
        PluginInterface.Create<Service>();
        
        playerState = PlayerState.GetInstance();
        playerState.RegisterJobChangeHandler(OnJobChange);

        Configuration = PluginInterface.GetPluginConfig() as DmcConfiguration ?? new DmcConfiguration();
        playerActionTracker = new();

        styleRankHandler = new(playerActionTracker);
        scoreManager = new(styleRankHandler, playerActionTracker);
        scoreProgressBar = new(scoreManager, styleRankHandler, playerActionTracker);
        finalRankCalculator = new (playerState, styleRankHandler);
        pluginUi = new(scoreProgressBar, styleRankHandler, scoreManager, finalRankCalculator, OnActiveOutsideInstanceConfChange);

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
        return IsCombatJob
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

    private void OnJobChange(object? sender, JobIds job)
    {
        IsCombatJob = playerState.IsCombatJob();
    }
    private void OnActiveOutsideInstanceConfChange(object? sender, bool activeOutsideInstance)
    {
        if(playerState.IsInsideInstance || activeOutsideInstance)
        {
            return;
        }
        scoreProgressBar.Reset();
        scoreManager.Reset();
        finalRankCalculator.Reset();
        styleRankHandler.Reset();
    }
}
