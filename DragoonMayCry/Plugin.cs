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
using PlayerState = DragoonMayCry.State.PlayerState;

#endregion

namespace DragoonMayCry
{
    public class Plugin : IDalamudPlugin
    {

        private const string CommandName = "/dmc";

        private static Plugin? instance;
        private readonly AudioService audioService;
        private readonly DynamicBgmService dynamicBgmService;
        private readonly FinalRankCalculator finalRankCalculator;

        private readonly HitCounter hitCounter;
        private readonly PlayerActionTracker playerActionTracker;
        private readonly PlayerState playerState;
        private readonly PluginUI pluginUi;
        private readonly RecordService recordService;

        private readonly ScoreManager scoreManager;
        private readonly ScoreProgressBar scoreProgressBar;
        private readonly StyleAnnouncerService styleAnnouncerService;
        private readonly StyleRankHandler styleRankHandler;
        private JobId currentJob = JobId.OTHER;

        public Plugin()
        {
            instance = this;

            PluginInterface.Create<Service>();

            KamiCommon.Initialize(PluginInterface, "DragoonMayCry", () => Configuration?.Save());

            Configuration = InitConfig();
            Configuration.Save();

            playerState = PlayerState.GetInstance();
            playerState.RegisterJobChangeHandler(OnJobChange);

            audioService = AudioService.Instance;

            playerActionTracker = new PlayerActionTracker();
            styleRankHandler = new StyleRankHandler(playerActionTracker);
            styleAnnouncerService = new StyleAnnouncerService(styleRankHandler, playerActionTracker);
            dynamicBgmService = new DynamicBgmService(styleRankHandler);
            scoreManager = new ScoreManager(styleRankHandler, playerActionTracker);

            playerActionTracker.SetJobModuleFactory(new JobModuleFactory(scoreManager));

            scoreProgressBar = new ScoreProgressBar(scoreManager, styleRankHandler, playerActionTracker, playerState);
            finalRankCalculator = new FinalRankCalculator(playerState, playerActionTracker);

            recordService = new RecordService(finalRankCalculator);
            recordService.Initialize();

            hitCounter = new HitCounter(playerActionTracker);
            pluginUi = new PluginUI(scoreProgressBar, styleRankHandler, scoreManager, finalRankCalculator,
                                    styleAnnouncerService,
                                    dynamicBgmService, playerActionTracker, recordService, hitCounter);


            scoreProgressBar.DemotionApplied += styleRankHandler.OnDemotion;
            scoreProgressBar.Promotion += styleRankHandler.OnPromotion;

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
        public static ScoreManager ScoreManager =>
            instance?.scoreManager ?? throw new InvalidOperationException("Plugin not initialized");
        public static PlayerState PlayerState =>
            instance?.playerState ?? throw new InvalidOperationException("Plugin not initialized");
        public static ScoreProgressBar ScoreProgressBar =>
            instance?.scoreProgressBar ?? throw new InvalidOperationException("Plugin not initialized");
        public static PlayerActionTracker PlayerActionTracker =>
            instance?.playerActionTracker ?? throw new InvalidOperationException("Plugin not initialized");
        public static StyleRankHandler StyleRankHandler =>
            instance?.styleRankHandler ?? throw new InvalidOperationException("Plugin not initialized");
        public static FinalRankCalculator FinalRankCalculator =>
            instance?.finalRankCalculator ?? throw new InvalidOperationException("Plugin not initialized");
        public static AudioService AudioService =>
            instance?.audioService ?? throw new InvalidOperationException("Plugin not initialized");
        public static DynamicBgmService DynamicBgmService =>
            instance?.dynamicBgmService ?? throw new InvalidOperationException("Plugin not initialized");
        public static RecordService RecordService =>
            instance?.recordService ?? throw new InvalidOperationException("Plugin not initialized");
        public static StyleAnnouncerService StyleAnnouncerService =>
            instance?.styleAnnouncerService ?? throw new InvalidOperationException("Plugin not initialized");
        public static JobId CurrentJob => instance?.currentJob ?? JobId.OTHER;
        [PluginService]
        public static IDalamudPluginInterface PluginInterface { get; private set; } = null!;

        public static DmcConfiguration? Configuration { get; private set; }

        public void Dispose()
        {
            instance = null;

            dynamicBgmService.Dispose();
            audioService.Dispose();
            KamiCommon.Dispose();
            scoreProgressBar.Dispose();
            playerActionTracker.Dispose();
            scoreManager.Dispose();
            playerState.Dispose();
            pluginUi.Dispose();
            Service.CommandManager.RemoveHandler(CommandName);
        }

        public static bool CanRunDmc()
        {
            return JobHelper.IsCombatJob(CurrentJob)
                   && PlayerState.IsInCombat
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
                   && !PlayerState.IsInPvp()
                   && IsEnabledForCurrentJob()
                   && (PlayerState.IsInsideInstance
                       || Configuration!.ActiveOutsideInstance);
        }

        public static bool IsEnabledForCurrentJob()
        {
            return Configuration?.JobConfiguration.ContainsKey(CurrentJob) == true &&
                   Configuration.JobConfiguration[CurrentJob].EnableDmc;
        }

        private void OnCommand(string command, string args)
        {
            if (args == "conf")
            {
                PluginUI.ToggleConfigUi();
            }
            else if (args == "bgm" && Configuration != null)
            {
                Configuration.EnableDynamicBgm.Value = !Configuration.EnableDynamicBgm.Value;
                KamiCommon.SaveConfiguration();
                dynamicBgmService.ToggleDynamicBgm(this, Configuration.EnableDynamicBgm);
                var bgmState = Configuration.EnableDynamicBgm ? "on" : "off";
                Service.ChatGui.Print($"[DragoonMayCry] Dynamic BGM turned {bgmState}");
            }
            else if (args == "job" && Configuration?.JobConfiguration != null)
            {
                if (Configuration.JobConfiguration.TryGetValue(CurrentJob, out var jobConfig))
                {
                    jobConfig.EnableDmc.Value = !jobConfig.EnableDmc.Value;
                    KamiCommon.SaveConfiguration();
                    dynamicBgmService.OnJobEnableChange(this, CurrentJob);
                    var pluginState = jobConfig.EnableDmc.Value ? "on" : "off";
                    Service.ChatGui.Print($"[DragoonMayCry] DmC turned {pluginState} for {CurrentJob}");
                }
            }
            else if (args == "next" && Configuration != null)
            {
                var bgm = dynamicBgmService.PlayNextBgmInQueue();
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
            if (PlayerState.IsInsideInstance || activeOutsideInstance)
            {
                return;
            }

            ScoreProgressBar.Reset();
            ScoreManager.Reset();
            FinalRankCalculator.Reset();
            StyleRankHandler.Reset();
        }

        public static bool IsEmdModeEnabled()
        {
            return Configuration?.JobConfiguration.TryGetValue(CurrentJob, out var jobConfig) == true &&
                   jobConfig.DifficultyMode == DifficultyMode.EstinienMustDie;
        }

        private static DmcConfiguration InitConfig()
        {
            var configPath = PluginInterface.ConfigFile;
            if (!File.Exists(configPath.FullName))
            {
                return new DmcConfiguration();
            }

            try
            {
                var configText = File.ReadAllText(configPath.FullName);
                var config = JsonConvert.DeserializeObject<DmcConfiguration>(configText);
                if (config == null)
                {
                    return new DmcConfiguration();
                }

                if (config.Version < 2)
                {
                    config = config.MigrateToVersionTwo();
                }
                if (config.Version < 3)
                {
                    config = config.MigrateToVersionThree();
                }

                RemoveUnknownCustomBgmIds(config);

                return config;
            }
            catch (Exception e)
            {
                return new DmcConfiguration();
            }
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
            currentJob = job;
        }

        [Conditional("DEBUG")]
        public static void StartBgm()
        {
            DynamicBgmService.GetFsm().Start();
        }

        [Conditional("DEBUG")]
        public static void StopBgm()
        {
            DynamicBgmService.GetFsm().ResetToIntro();
            AudioService.Instance.StopBgm();
        }

        [Conditional("DEBUG")]
        public static void SimulateBgmRankChanges(StyleType previous, StyleType newStyle)
        {
            DynamicBgmService.GetFsm()
                             .OnRankChange(null, new StyleRankHandler.RankChangeData(previous, newStyle, false));
        }

        [Conditional("DEBUG")]
        public static void BgmTransitionNext()
        {
            DynamicBgmService.GetFsm().Promote();
        }

        [Conditional("DEBUG")]
        public static void BgmEndCombat()
        {
            DynamicBgmService.GetFsm().LeaveCombat();
        }

        [Conditional("DEBUG")]
        public static void BgmDemotion()
        {
            DynamicBgmService.GetFsm().Demote();
        }
    }
}
