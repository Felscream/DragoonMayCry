using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using DragoonMayCry.Audio;
using DragoonMayCry.Audio.BGM;
using DragoonMayCry.Audio.BGM.FSM;
using DragoonMayCry.Configuration;
using DragoonMayCry.Score;
using DragoonMayCry.Score.Action;
using DragoonMayCry.Score.Model;
using DragoonMayCry.Score.Style.Announcer;
using DragoonMayCry.Score.Style.Rank;
using DragoonMayCry.State;
using DragoonMayCry.UI;
using KamiLib;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace DragoonMayCry;

public unsafe class Plugin : IDalamudPlugin
{
    [PluginService] public static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    public static DmcConfigurationOne? Configuration { get; private set; }

    private const string CommandName = "/dmc";

    private static ScoreManager? ScoreManager;
    private readonly PluginUI PluginUi;
    private static PlayerState? PlayerState;
    private static ScoreProgressBar? ScoreProgressBar;
    private static PlayerActionTracker? PlayerActionTracker;
    private static StyleRankHandler? StyleRankHandler;
    private static FinalRankCalculator? FinalRankCalculator;
    private static AudioService? AudioService;
    private static DynamicBgmService? BgmService;
    public static StyleAnnouncerService StyleAnnouncerService;
    public Plugin()
    {
        PluginInterface.Create<Service>();

        KamiCommon.Initialize(PluginInterface, "DragoonMayCry", () => Configuration?.Save());
        PlayerState = PlayerState.GetInstance();

        Configuration = InitConfig();
        Configuration.Save();
        AudioService = AudioService.Instance;

        PlayerActionTracker = new();
        StyleRankHandler = new(PlayerActionTracker);
        StyleAnnouncerService = new(StyleRankHandler, PlayerActionTracker);
        BgmService = new DynamicBgmService(StyleRankHandler);
        ScoreManager = new(StyleRankHandler, PlayerActionTracker);
        ScoreProgressBar = new(ScoreManager, StyleRankHandler, PlayerActionTracker, PlayerState);
        FinalRankCalculator = new(PlayerState, StyleRankHandler);
        PluginUi = new(ScoreProgressBar, StyleRankHandler, ScoreManager, FinalRankCalculator, StyleAnnouncerService);
        

        ScoreProgressBar.DemotionApplied += StyleRankHandler.OnDemotion;
        ScoreProgressBar.Promotion += StyleRankHandler.OnPromotion;

        Service.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "opens configuration menu"
        });

        AssetsManager.VerifyAndUpdateAssets();
    }

    public static bool CanRunDmc()
    {
        var playerState = PlayerState.GetInstance();
        return playerState.IsCombatJob()
               && playerState.IsInCombat
               && !playerState.IsInPvp()
               && (playerState.IsInsideInstance ||
                   Configuration!.ActiveOutsideInstance);
    }

    public void Dispose()
    {
        BgmService?.Dispose();
        AudioService?.Dispose();
        KamiCommon.Dispose();
        ScoreProgressBar?.Dispose();
        PlayerActionTracker?.Dispose();
        ScoreManager?.Dispose();
        PlayerState?.Dispose();
        PluginUi?.Dispose();
        Service.CommandManager.RemoveHandler(CommandName);
    }



    private void OnCommand(string command, string args)
    {
        PluginUi?.ToggleConfigUI();
    }

    public static void OnActiveOutsideInstanceConfChange(object? sender, bool activeOutsideInstance)
    {
        if (PlayerState!.IsInsideInstance || activeOutsideInstance)
        {
            return;
        }
        ScoreProgressBar?.Reset();
        ScoreManager?.Reset();
        FinalRankCalculator?.Reset();
        StyleRankHandler?.Reset();
    }

    private static DmcConfigurationOne InitConfig()
    {
        var configFile = PluginInterface.ConfigFile.FullName;
        if (!File.Exists(configFile))
        {
            return new DmcConfigurationOne();
        }

        var configText = File.ReadAllText(configFile);
        try
        {
            var versionCheck = JsonSerializer.Deserialize<BaseConfiguration>(configText);
            if (versionCheck is null)
            {
                return new DmcConfigurationOne();
            }

            var version = versionCheck.Version;
            var config = version switch
            {
                0 => JsonSerializer.Deserialize<DmcConfiguration>(configText)?.MigrateToOne() ?? new DmcConfigurationOne(),
                1 => JsonConvert.DeserializeObject<DmcConfigurationOne>(configText) ?? new DmcConfigurationOne(),
                _ => new DmcConfigurationOne()
            };
            return config;
        }
        catch (Exception e)
        {
            if (e.StackTrace is not null)
            {
                Service.Log.Debug(e.StackTrace);
            }
            Service.Log.Warning("Your configuration migration failed, it has been reinitialized");
            return new DmcConfigurationOne();
        }
    }

    public static void ToggleDynamicBgm(object? sender, bool value)
    {
        BgmService?.ToggleDynamicBgmActivation();
    }

    [Conditional("DEBUG")]
    public static void StartBgm()
    {
        BgmService?.GetFsm().Start();
    }

    [Conditional("DEBUG")]
    public static void StopBgm()
    {
        BgmService?.GetFsm().ResetToIntro();
        AudioService.Instance.StopBgm();
    }

    [Conditional("DEBUG")]
    public static void SimulateBgmRankChanges(StyleType previous, StyleType newStyle)
    {
        BgmService?.GetFsm().OnRankChange(null, new StyleRankHandler.RankChangeData(previous, newStyle, false));
    }

    [Conditional("DEBUG")]
    public static void BgmTransitionNext()
    {
        BgmService?.GetFsm().Promotion();
    }

    [Conditional("DEBUG")]
    public static void BgmEndCombat()
    {
        BgmService?.GetFsm().LeaveCombat();
    }

    [Conditional("DEBUG")]
    public static void BgmDemotion()
    {
        BgmService?.GetFsm().Demotion();
    }
}
