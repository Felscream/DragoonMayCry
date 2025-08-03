#region

using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Utility;
using DragoonMayCry.Audio;
using DragoonMayCry.Audio.BGM;
using DragoonMayCry.Audio.BGM.CustomBgm;
using DragoonMayCry.Audio.StyleAnnouncer;
using DragoonMayCry.Configuration;
using DragoonMayCry.Data;
using DragoonMayCry.Record;
using DragoonMayCry.Score;
using DragoonMayCry.Score.Action;
using DragoonMayCry.Score.Action.JobModule;
using DragoonMayCry.Score.Model;
using DragoonMayCry.Score.Rank;
using DragoonMayCry.UI;
using DragoonMayCry.Util;
using KamiLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using JsonSerializer = System.Text.Json.JsonSerializer;
using PlayerState = DragoonMayCry.State.PlayerState;

#endregion

namespace DragoonMayCry
{
    public class Plugin : IDalamudPlugin
    {

        private const string CommandName = "/dmc";

        private static ScoreManager? ScoreManager;
        private static PlayerState? PlayerState;
        private static ScoreProgressBar? ScoreProgressBar;
        private static PlayerActionTracker? PlayerActionTracker;
        private static StyleRankHandler? StyleRankHandler;
        private static FinalRankCalculator? FinalRankCalculator;
        private static AudioService? AudioService;
        private static DynamicBgmService? DynamicBgmService;
        private static RecordService? RecordService;
        public static StyleAnnouncerService? StyleAnnouncerService;
        private static JobId CurrentJob = JobId.OTHER;

        private readonly HitCounter hitCounter;
        private readonly PluginUI pluginUi;

        public Plugin()
        {
            PluginInterface.Create<Service>();

            KamiCommon.Initialize(PluginInterface, "DragoonMayCry", () => Configuration?.Save());
            PlayerState = PlayerState.GetInstance();
            PlayerState.RegisterJobChangeHandler(OnJobChange);

            Configuration = InitConfig();
            Configuration.Save();
            AudioService = AudioService.Instance;

            PlayerActionTracker = new PlayerActionTracker();
            StyleRankHandler = new StyleRankHandler(PlayerActionTracker);
            StyleAnnouncerService = new StyleAnnouncerService(StyleRankHandler, PlayerActionTracker);
            DynamicBgmService = new DynamicBgmService(StyleRankHandler);
            ScoreManager = new ScoreManager(StyleRankHandler, PlayerActionTracker);

            PlayerActionTracker.SetJobModuleFactory(new JobModuleFactory(ScoreManager));

            ScoreProgressBar = new ScoreProgressBar(ScoreManager, StyleRankHandler, PlayerActionTracker, PlayerState);
            FinalRankCalculator = new FinalRankCalculator(PlayerState, PlayerActionTracker);

            RecordService = new RecordService(FinalRankCalculator);
            RecordService.Initialize();

            hitCounter = new HitCounter(PlayerActionTracker);
            pluginUi = new PluginUI(ScoreProgressBar, StyleRankHandler, ScoreManager, FinalRankCalculator,
                                    StyleAnnouncerService,
                                    DynamicBgmService, PlayerActionTracker, RecordService, hitCounter);


            ScoreProgressBar.DemotionApplied += StyleRankHandler.OnDemotion;
            ScoreProgressBar.Promotion += StyleRankHandler.OnPromotion;

            Service.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "opens record history",
            });

            Service.CommandManager.AddHandler("/dmc conf", new CommandInfo(OnCommand)
            {
                HelpMessage = "opens configuration menu",
            });

            Service.CommandManager.AddHandler("/dmc bgm", new CommandInfo(OnCommand)
            {
                HelpMessage = "toggles dynamic background music on/off",
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
        [PluginService]
        public static IDalamudPluginInterface PluginInterface { get; private set; } = null!;

        public static DmcConfiguration? Configuration { get; private set; }

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
            return PluginInterface.InstalledPlugins.Any(plugin => plugin is
            {
                IsLoaded: true,
                InternalName: "MultiHit",
            });
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
                var bgmState = Configuration.EnableDynamicBgm ? "on" : "off";
                Service.ChatGui.Print($"[DragoonMayCry] Dynamic BGM turned {bgmState}");
            }
            else if (args == "job" && Configuration?.JobConfiguration != null && DynamicBgmService != null)
            {
                if (Configuration.JobConfiguration.TryGetValue(CurrentJob, out var jobConfig))
                {
                    jobConfig.EnableDmc.Value = !jobConfig.EnableDmc;
                    KamiCommon.SaveConfiguration();
                    DynamicBgmService.OnJobEnableChange(this, CurrentJob);
                    var pluginState = jobConfig.EnableDmc ? "on" : "off";
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
            else if (args.IsNullOrEmpty())
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
            DmcConfiguration config;
            try
            {
                var versionCheck = JsonSerializer.Deserialize<BaseConfiguration>(configText);
                if (versionCheck is null)
                {
                    return new DmcConfiguration();
                }

                var version = versionCheck.Version;
                config = version switch
                {
                    1 => JsonConvert.DeserializeObject<DmcConfiguration>(configText)?.MigrateToVersionTwo()
                                    .MigrateToVersionThree() ??
                         new DmcConfiguration(),
                    2 => JsonConvert.DeserializeObject<DmcConfiguration>(configText)?.MigrateToVersionThree()
                         ?? new DmcConfiguration(),
                    3 => JsonConvert.DeserializeObject<DmcConfiguration>(configText) ?? new DmcConfiguration(),
                    _ => new DmcConfiguration(),
                };
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

            RemoveUnknownCustomBgmIds(config);
            return config;
        }

        private static bool IsInvalidBgmKey(long bgmKey, HashSet<long> currentCustomBgmIds)
        {
            return bgmKey is > BgmKeys.MaxPreconfiguredBgmKey or < BgmKeys.Off
                   && !currentCustomBgmIds.Contains(bgmKey);
        }

        private static bool IsInvalidBgmSelectionKey(long bgmKey, HashSet<long> currentCustomBgmIds)
        {
            return (bgmKey < BgmKeys.MinPreconfiguredBgmKey || bgmKey > BgmKeys.MaxPreconfiguredBgmKey)
                   && !currentCustomBgmIds.Contains(bgmKey);
        }
        private static void RemoveUnknownCustomBgmIds(DmcConfiguration config)
        {
            var currentCustomBgmIds = CustomBgmManager.Instance.GetCustomBgmIds().ToHashSet();
            HashSet<long> removedCustomBgmIds = new();
            foreach (var jobConfig in config.JobConfiguration)
            {
                if (IsInvalidBgmKey(jobConfig.Value.Bgm.Value, currentCustomBgmIds))
                {
                    Service.Log.Warning(
                        $"Unknown custom bgm ID {jobConfig.Value.Bgm.Value}. The value has been reset.");
                    jobConfig.Value.Bgm.Value = BgmKeys.BuryTheLight;
                }

                foreach (var randomBgmId in jobConfig.Value.BgmRandomSelection.Value)
                {
                    if (IsInvalidBgmSelectionKey(randomBgmId, currentCustomBgmIds))
                    {
                        removedCustomBgmIds.Add(randomBgmId);
                    }
                }
                jobConfig.Value.BgmRandomSelection.Value.RemoveWhere(removedCustomBgmIds.Contains);

                if (jobConfig.Value.BgmRandomSelection.Value.Count < 2)
                {
                    jobConfig.Value.BgmRandomSelection.Value.Add(BgmKeys.BuryTheLight);
                }
                if (jobConfig.Value.BgmRandomSelection.Value.Count < 2)
                {
                    jobConfig.Value.BgmRandomSelection.Value.Add(BgmKeys.DevilTrigger);
                }

            }
            if (removedCustomBgmIds.Count > 0)
            {
                var removedIds = string.Join(", ", removedCustomBgmIds);
                Service.Log.Warning(
                    $"Could not find Custom BGMs with the following IDs {removedIds}. \nThey have been removed from your configuration.");
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
            DynamicBgmService?.GetFsm()
                             .OnRankChange(null, new StyleRankHandler.RankChangeData(previous, newStyle, false));
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
}
