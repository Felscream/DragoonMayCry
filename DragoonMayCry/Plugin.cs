using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using DragoonMayCry.Audio;
using DragoonMayCry.Audio.BGM.FSM;
using DragoonMayCry.Configuration;
using DragoonMayCry.Score;
using DragoonMayCry.Score.Action;
using DragoonMayCry.Score.Model;
using DragoonMayCry.Score.Style;
using DragoonMayCry.State;
using DragoonMayCry.UI;
using KamiLib;
using Newtonsoft.Json;
using System;
using System.IO;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace DragoonMayCry;

public unsafe class Plugin : IDalamudPlugin
{
    [PluginService] public static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    public static DmcConfigurationOne? Configuration { get; private set; }

    private const string CommandName = "/dmc";

    private readonly ScoreManager scoreManager;
    private readonly PluginUI pluginUi;
    private readonly PlayerState playerState;
    private readonly ScoreProgressBar scoreProgressBar;
    private readonly PlayerActionTracker playerActionTracker;
    private readonly StyleRankHandler styleRankHandler;
    private readonly FinalRankCalculator finalRankCalculator;
    private readonly AudioService audioService;
    private static BgmFsm buryTheLight;
    public Plugin()
    {
        PluginInterface.Create<Service>();

        KamiCommon.Initialize(PluginInterface, "DragoonMayCry", () => Configuration?.Save());
        playerState = PlayerState.GetInstance();

        Configuration = InitConfig();
        Configuration.Save();
        this.audioService = AudioService.Instance;
        playerActionTracker = new();

        styleRankHandler = new(playerActionTracker);
        buryTheLight = new BgmFsm(this.audioService, playerState, styleRankHandler, Service.Framework);
        scoreManager = new(styleRankHandler, playerActionTracker);
        scoreProgressBar = new(scoreManager, styleRankHandler, playerActionTracker, playerState);
        finalRankCalculator = new(playerState, styleRankHandler);
        pluginUi = new(scoreProgressBar, styleRankHandler, scoreManager, finalRankCalculator, OnActiveOutsideInstanceConfChange);

        scoreProgressBar.DemotionApplied += styleRankHandler.OnDemotion;
        scoreProgressBar.Promotion += styleRankHandler.OnPromotion;

        Service.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "opens configuration menu"
        });

        AssetsManager.VerifyAndUpdateAssets();
    }

    public static void StartBgm()
    {
        buryTheLight.Start();
    }

    public static void StopBgm()
    {
        buryTheLight.ResetToIntro();
        AudioService.Instance.StopBgm();
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

       // buryTheLight.Dispose();
        audioService.Dispose();
        KamiCommon.Dispose();
        scoreProgressBar.Dispose();
        playerActionTracker.Dispose();
        scoreManager.Dispose();
        playerState.Dispose();
        pluginUi.Dispose();
        Service.CommandManager.RemoveHandler(CommandName);
    }


    public static void SimulateBgmRankChanges(StyleType previous, StyleType newStyle)
    {
        buryTheLight.OnRankChange(null, new StyleRankHandler.RankChangeData(previous, newStyle, false));
    }
    public static void BgmTransitionNext()
    {
        buryTheLight.Promotion();
    }

    public static void BgmEndCombat()
    {
        buryTheLight.LeaveCombat();
    }

    public static void BgmDemotion()
    {
        buryTheLight.Demotion();
    }

    private void OnCommand(string command, string args)
    {
        pluginUi.ToggleConfigUI();
    }

    private void OnActiveOutsideInstanceConfChange(object? sender, bool activeOutsideInstance)
    {
        if (playerState.IsInsideInstance || activeOutsideInstance)
        {
            return;
        }
        scoreProgressBar.Reset();
        scoreManager.Reset();
        finalRankCalculator.Reset();
        styleRankHandler.Reset();
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
}
