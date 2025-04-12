using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using DragoonMayCry.Audio;
using DragoonMayCry.Audio.BGM;
using DragoonMayCry.Audio.StyleAnnouncer;
using DragoonMayCry.Configuration;
using DragoonMayCry.Data;
using DragoonMayCry.Record;
using DragoonMayCry.Score;
using DragoonMayCry.Score.Action;
using DragoonMayCry.Score.Model;
using DragoonMayCry.Score.Rank;
using DragoonMayCry.UI;
using DragoonMayCry.Util;
using KamiLib;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Dalamud.Utility;
using JsonSerializer = System.Text.Json.JsonSerializer;
using PlayerState = DragoonMayCry.State.PlayerState;

namespace DragoonMayCry;

public unsafe class Plugin : IDalamudPlugin
{
    [PluginService]
    public static IDalamudPluginInterface PluginInterface { get; private set; } = null!;

    public static DmcConfiguration? Configuration { get; private set; }

    private const string CommandName = "/dmc";

    private static ScoreManager? ScoreManager;
    private readonly PluginUI pluginUi;
    private static PlayerState? PlayerState;
    private static ScoreProgressBar? ScoreProgressBar;
    private static PlayerActionTracker? PlayerActionTracker;
    private static StyleRankHandler? StyleRankHandler;
    private static FinalRankCalculator? FinalRankCalculator;
    private static AudioService? AudioService;
    private static DynamicBgmService? DynamicBgmService;
    private static RecordService? RecordService;
    public static StyleAnnouncerService? StyleAnnouncerService;

    private readonly HitCounter hitCounter;
    private static JobId CurrentJob = JobId.OTHER;

    public Plugin()
    {
        PluginInterface.Create<Service>();

        KamiCommon.Initialize(PluginInterface, "DragoonMayCry", () => Configuration?.Save());
        PlayerState = PlayerState.GetInstance();
        PlayerState.RegisterJobChangeHandler(OnJobChange);

        Configuration = InitConfig();
        Configuration.Save();
        AudioService = AudioService.Instance;

        PlayerActionTracker = new();
        StyleRankHandler = new(PlayerActionTracker);
        StyleAnnouncerService = new(StyleRankHandler, PlayerActionTracker);
        DynamicBgmService = new DynamicBgmService(StyleRankHandler);
        ScoreManager = new(StyleRankHandler, PlayerActionTracker);

        PlayerActionTracker.SetJobModuleFactory(new(ScoreManager));

        ScoreProgressBar = new(ScoreManager, StyleRankHandler, PlayerActionTracker, PlayerState);
        FinalRankCalculator = new(PlayerState, PlayerActionTracker);

        RecordService = new(FinalRankCalculator);
        RecordService.Initialize();

        hitCounter = new HitCounter(PlayerActionTracker);
        pluginUi = new(ScoreProgressBar, StyleRankHandler, ScoreManager, FinalRankCalculator, StyleAnnouncerService,
                       DynamicBgmService, PlayerActionTracker, RecordService, hitCounter);


        ScoreProgressBar.DemotionApplied += StyleRankHandler.OnDemotion;
        ScoreProgressBar.Promotion += StyleRankHandler.OnPromotion;

        Service.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "opens record history"
        });

        Service.CommandManager.AddHandler("/dmc conf", new CommandInfo(OnCommand)
        {
            HelpMessage = "opens configuration menu"
        });
        
        Service.CommandManager.AddHandler("/dmc bgm", new CommandInfo(OnCommand)
        {
            HelpMessage = "toggles dynamic background music on/off"
        });
        
        Service.CommandManager.AddHandler("/dmc job", new CommandInfo(OnCommand)
        {
            HelpMessage = "toggles DmC on/off for the current job",
        });
        
        Service.CommandManager.AddHandler("/dmc next", new CommandInfo(OnCommand)
        {
            HelpMessage = "loads the next BGM in the randomized queue outside of combat",
        });
        AssetsManager.VerifyAndUpdateAssets();
    }

    public static bool CanRunDmc()
    {
        // A warning appears if PlayerState#IsCombatJob is used directly
        return JobHelper.IsCombatJob(CurrentJob)
               && PlayerState!.IsInCombat
               && !PlayerState.IsInPvp()
               && IsEnabledForCurrentJob()
               && (PlayerState.IsInsideInstance
                   || Configuration!.ActiveOutsideInstance);
    }

    public static bool IsMultiHitLoaded()
    {
        return PluginInterface.InstalledPlugins.FirstOrDefault(
                   plugin => plugin.IsLoaded && plugin.InternalName == "MultiHit") != null;
    }

    public static bool CanHandleEvents()
    {
        return JobHelper.IsCombatJob(CurrentJob)
               && !PlayerState!.IsInPvp()
               && IsEnabledForCurrentJob()
               && (PlayerState.IsInsideInstance
                   || Configuration!.ActiveOutsideInstance);
    }

    public static bool IsEnabledForCurrentJob()
    {
        return Configuration != null && Configuration!.JobConfiguration.ContainsKey(CurrentJob)
                                     && Configuration.JobConfiguration[CurrentJob].EnableDmc;
    }

    public void Dispose()
    {
        DynamicBgmService?.Dispose();
        AudioService?.Dispose();
        KamiCommon.Dispose();
        ScoreProgressBar?.Dispose();
        PlayerActionTracker?.Dispose();
        ScoreManager?.Dispose();
        PlayerState?.Dispose();
        pluginUi?.Dispose();
        Service.CommandManager.RemoveHandler(CommandName);
    }

    private void OnCommand(string command, string args)
    {
        if (args == "conf")
        {
            PluginUI.ToggleConfigUi();
        } 
        else if (args == "bgm" && Configuration != null && DynamicBgmService != null)
        {
            Configuration.EnableDynamicBgm.Value = !Configuration.EnableDynamicBgm.Value;
            KamiCommon.SaveConfiguration();
            DynamicBgmService.ToggleDynamicBgm(this, Configuration.EnableDynamicBgm);
            String bgmState = Configuration.EnableDynamicBgm ? "on" : "off";
            Service.ChatGui.Print($"[DragoonMayCry] Dynamic BGM turned {bgmState}");
        } 
        else if(args == "job" && Configuration?.JobConfiguration != null && DynamicBgmService != null)
        {
            if (Configuration.JobConfiguration.TryGetValue(CurrentJob, out var jobConfig))
            {
                jobConfig.EnableDmc.Value = !jobConfig.EnableDmc;
                KamiCommon.SaveConfiguration();
                DynamicBgmService.OnJobEnableChange(this, CurrentJob);
                String pluginState = jobConfig.EnableDmc ? "on" : "off";
                Service.ChatGui.Print($"[DragoonMayCry] DmC turned {pluginState} for {CurrentJob}");
            }
        } 
        else if (args == "next" && Configuration != null && DynamicBgmService != null)
        {
            var bgm = DynamicBgmService.PlayNextBgmInQueue();
            var message = "[DragoonMayCry] Cannot play next track in current state.";
            if (!bgm.IsNullOrEmpty())
            {
                message = $"[DragoonMayCry] Loading {bgm}";
            }
            Service.ChatGui.Print(message);
        }
        else if(args.IsNullOrEmpty())
        {
            PluginUI.ToggleCharacterRecords();
        }
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

    public static bool IsEmdModeEnabled()
    {
        if (Configuration == null || !Configuration.JobConfiguration.ContainsKey(CurrentJob))
        {
            return false;
        }

        return Configuration.JobConfiguration[CurrentJob].DifficultyMode == DifficultyMode.EstinienMustDie;
    }

    private static DmcConfiguration InitConfig()
    {
        var configFile = PluginInterface.ConfigFile.FullName;
        if (!File.Exists(configFile))
        {
            return new DmcConfiguration();
        }

        var configText = File.ReadAllText(configFile);
        try
        {
            var versionCheck = JsonSerializer.Deserialize<BaseConfiguration>(configText);
            if (versionCheck is null)
            {
                return new DmcConfiguration();
            }

            var version = versionCheck.Version;
            var config = version switch
            {
                1 => JsonConvert.DeserializeObject<DmcConfiguration>(configText)?.MigrateToVersionTwo() ??
                     new DmcConfiguration(),
                2 => JsonConvert.DeserializeObject<DmcConfiguration>(configText) ?? new DmcConfiguration(),
                _ => new DmcConfiguration()
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
            return new DmcConfiguration();
        }
    }

    private void OnJobChange(object? sender, JobId job)
    {
        CurrentJob = job;
    }

    [Conditional("DEBUG")]
    public static void StartBgm()
    {
        DynamicBgmService?.GetFsm().Start();
    }

    [Conditional("DEBUG")]
    public static void StopBgm()
    {
        DynamicBgmService?.GetFsm().ResetToIntro();
        AudioService.Instance.StopBgm();
    }

    [Conditional("DEBUG")]
    public static void SimulateBgmRankChanges(StyleType previous, StyleType newStyle)
    {
        DynamicBgmService?.GetFsm().OnRankChange(null, new StyleRankHandler.RankChangeData(previous, newStyle, false));
    }

    [Conditional("DEBUG")]
    public static void BgmTransitionNext()
    {
        DynamicBgmService?.GetFsm().Promote();
    }

    [Conditional("DEBUG")]
    public static void BgmEndCombat()
    {
        DynamicBgmService?.GetFsm().LeaveCombat();
    }

    [Conditional("DEBUG")]
    public static void BgmDemotion()
    {
        DynamicBgmService?.GetFsm().Demote();
    }
}
