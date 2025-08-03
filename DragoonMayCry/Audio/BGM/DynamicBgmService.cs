#region

using Dalamud.Game.Config;
using DragoonMayCry.Audio.BGM.CustomBgm;
using DragoonMayCry.Audio.BGM.FSM;
using DragoonMayCry.Audio.BGM.FSM.States;
using DragoonMayCry.Audio.BGM.FSM.States.BuryTheLight;
using DragoonMayCry.Audio.BGM.FSM.States.CrimsonCloud;
using DragoonMayCry.Audio.BGM.FSM.States.DevilsNeverCry;
using DragoonMayCry.Audio.BGM.FSM.States.DevilTrigger;
using DragoonMayCry.Audio.BGM.FSM.States.Subhuman;
using DragoonMayCry.Data;
using DragoonMayCry.Score.Rank;
using DragoonMayCry.State;
using DragoonMayCry.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

#endregion

namespace DragoonMayCry.Audio.BGM
{
    public class DynamicBgmService : IDisposable
    {
        private const long MaxDefaultBgmConfigurationId = 6;
        private static readonly CustomBgmManager CustomBgmManager = CustomBgmManager.Instance;
        private readonly AudioService audioService;
        private readonly DynamicBgmFsm bgmFsm;
        private readonly Dictionary<long, Dictionary<BgmState, IFsmState>> bgmFsmStates = new();

        private readonly Dictionary<BgmState, IFsmState> buryTheLightStates;
        private readonly Dictionary<BgmState, IFsmState> crimsonCloudStates;
        private readonly Dictionary<BgmState, IFsmState> devilsNeverCryStates;
        private readonly Dictionary<BgmState, IFsmState> devilTriggerStates;
        private readonly PlayerState playerState;
        private readonly Dictionary<BgmState, IFsmState> subhumanStates;
        private long currentBgmKey = -1L;
        private uint currentTerritory;
        private bool gameBgmState;

        private Queue<long> randomBgmQueue = new();
        private bool soundFilesLoaded;

        public DynamicBgmService(StyleRankHandler styleRankHandler)
        {
            bgmFsm = new DynamicBgmFsm(styleRankHandler);
            AssetsManager.AssetsReady += OnAssetsAvailable;
            playerState = PlayerState.GetInstance();
            playerState.RegisterInstanceChangeHandler(OnInstanceChange);
            playerState.RegisterJobChangeHandler(OnJobChange);
            playerState.RegisterPvpStateChangeHandler(OnPvpStateChange);
            playerState.RegisterDeathStateChangeHandler(OnDeath);

            gameBgmState = Service.GameConfig.System.GetBool("IsSndBgm");
            audioService = AudioService.Instance;

            IFsmState btlIntro = new BtlIntro(audioService);
            IFsmState btlCombat = new BtlVerse(audioService);
            IFsmState btlPeak = new BtlChorus(audioService);
            buryTheLightStates = new Dictionary<BgmState, IFsmState>
            {
                { BgmState.Intro, btlIntro },
                { BgmState.CombatLoop, btlCombat },
                { BgmState.CombatPeak, btlPeak },
            };

            IFsmState dtIntro = new DtIntro(audioService);
            IFsmState dtCombat = new DtVerse(audioService);
            IFsmState dtPeak = new DtChorus(audioService);
            devilTriggerStates = new Dictionary<BgmState, IFsmState>
            {
                { BgmState.Intro, dtIntro },
                { BgmState.CombatLoop, dtCombat },
                { BgmState.CombatPeak, dtPeak },
            };

            IFsmState ccIntro = new CCIntro(audioService);
            IFsmState ccCombat = new CcVerse(audioService);
            IFsmState ccPeak = new CcChorus(audioService);
            crimsonCloudStates = new Dictionary<BgmState, IFsmState>
            {
                { BgmState.Intro, ccIntro },
                { BgmState.CombatLoop, ccCombat },
                { BgmState.CombatPeak, ccPeak },
            };

            IFsmState subIntro = new SubIntro(audioService);
            IFsmState subCombat = new SubVerse(audioService);
            IFsmState subPeak = new SubChorus(audioService);
            subhumanStates = new Dictionary<BgmState, IFsmState>
            {
                { BgmState.Intro, subIntro },
                { BgmState.CombatLoop, subCombat },
                { BgmState.CombatPeak, subPeak },
            };

            IFsmState dncIntro = new DncIntro(audioService);
            IFsmState dncCombat = new DncVerse(audioService);
            IFsmState dncPeak = new DncChorus(audioService);
            devilsNeverCryStates = new Dictionary<BgmState, IFsmState>
            {
                { BgmState.Intro, dncIntro },
                { BgmState.CombatLoop, dncCombat },
                { BgmState.CombatPeak, dncPeak },
            };

            bgmFsmStates.Add(BgmKeys.BuryTheLight, buryTheLightStates);
            bgmFsmStates.Add(BgmKeys.DevilTrigger, devilTriggerStates);
            bgmFsmStates.Add(BgmKeys.CrimsonCloud, crimsonCloudStates);
            bgmFsmStates.Add(BgmKeys.Subhuman, subhumanStates);
            bgmFsmStates.Add(BgmKeys.DevilsNeverCry, devilsNeverCryStates);
        }

        public void Dispose()
        {
            bgmFsm.Dispose();
        }

        public DynamicBgmFsm GetFsm()
        {
            return bgmFsm;
        }

        private void OnInstanceChange(object? sender, bool insideInstance)
        {
            currentTerritory = playerState.GetCurrentTerritoryId();
            audioService.RemoveDeathEffect();
            if (!insideInstance && bgmFsm.IsActive)
            {
                bgmFsm.Disable();
                ResetGameBgm();
            }
            else
            {
                gameBgmState = Service.GameConfig.System.GetBool("IsSndBgm");
            }
        }

        // Is triggered when player in instanciated in a new territory
        // Grab the current job here and play
        private void OnJobChange(object? sender, JobId job)
        {
            var insideInstance = playerState.IsInsideInstance;
            var currentJob = playerState.GetCurrentJob();
            if (!CanPlayDynamicBgm(insideInstance, currentJob))
            {
                if (ShouldDisableFsm())
                {
                    bgmFsm.Disable();
                    ResetGameBgm();
                }
                return;
            }
            DisableGameBgm();
            PrepareBgm(currentJob);
        }

        private void OnPvpStateChange(object? sender, bool inPvp)
        {
            if (inPvp)
            {
                bgmFsm.Disable();
                ResetGameBgm();
                return;
            }

            // when entering an instance from wolve's den pier,
            // the PvP flag is removed after other events (instance change / job change)
            var insideInstance = playerState.IsInsideInstance;
            var currentJob = playerState.GetCurrentJob();
            if (!CanPlayDynamicBgm(insideInstance, currentJob))
            {
                if (ShouldDisableFsm())
                {
                    bgmFsm.Disable();
                    ResetGameBgm();
                }
                return;
            }
            DisableGameBgm();
            PrepareBgm(currentJob);
        }

        public void OnJobEnableChange(object? sender, JobId job)
        {
            if (job != playerState.GetCurrentJob())
            {
                return;
            }
            var isInsideInstance = playerState.IsInsideInstance;
            var currentJob = playerState.GetCurrentJob();
            if (CanPlayDynamicBgm(isInsideInstance, currentJob) && !bgmFsm.IsActive)
            {
                gameBgmState = Service.GameConfig.System.GetBool("IsSndBgm");
                DisableGameBgm();
                PrepareBgm(currentJob);
            }
            else if (ShouldDisableFsm())
            {
                bgmFsm.Disable();
                audioService.RemoveDeathEffect();
                ResetGameBgm();
            }
        }

        private bool ShouldDisableFsm()
        {
            return bgmFsm.IsActive;
        }

        private bool CanPlayDynamicBgm(bool isInInstance, JobId job)
        {
            return playerState.Player != null
                   && !playerState.IsInPvp()
                   && isInInstance
                   && Plugin.Configuration!.EnableDynamicBgm
                   && Plugin.Configuration.JobConfiguration.ContainsKey(job)
                   && Plugin.Configuration.JobConfiguration[job].EnableDmc
                   && Plugin.Configuration.JobConfiguration[job].Bgm.Value != BgmKeys.Off
                   && !TerritoryIds.NoBgmInstances.Contains(currentTerritory)
                   && !IsCurrentDutyBlacklisted();
        }

        private bool IsCurrentDutyBlacklisted()
        {
            return Plugin.Configuration != null
                   && Plugin.Configuration.DynamicBgmBlacklistDuties.Value.Contains(playerState.GetCurrentContentId());
        }

        private void OnAssetsAvailable(object? sender, bool loaded)
        {
            if (!loaded || !bgmFsmStates.ContainsKey(currentBgmKey))
            {
                return;
            }

            var currentJob = playerState.GetCurrentJob();
            if (!CanPlayDynamicBgm(playerState.IsInsideInstance, currentJob))
            {
                return;
            }
            DisableGameBgm();
            PrepareBgm(currentJob);
        }

        private void PrepareBgm(JobId jobId)
        {
            bgmFsm.Disable();
            if (!Plugin.Configuration!.JobConfiguration.TryGetValue(jobId, out var jobConfiguration))
            {
                return;
            }
            var bgmConfigurationSelected = jobConfiguration.Bgm.Value;

            if (bgmConfigurationSelected == BgmKeys.Off)
            {
                return;
            }

            if (playerState.IsDead)
            {
                audioService.ApplyDeathEffect();
            }

            if (bgmConfigurationSelected == BgmKeys.Randomize)
            {
                bgmFsm.LoadNewBgm = LoadNextBgmInQueue;
                Task.Run(() =>
                {
                    var loadedBgms = CacheAllBgm(jobConfiguration.BgmRandomSelection.Value);
                    randomBgmQueue = GenerateRandomBgmQueue(loadedBgms);
                    LoadNextBgmInQueue();
                });

                return;
            }

            if (bgmFsm.LoadNewBgm != null)
            {
                bgmFsm.LoadNewBgm -= LoadNextBgmInQueue;
            }

            LoadBgm(bgmConfigurationSelected);
        }

        private bool IsCustomBgmId(long bgmId)
        {
            return bgmId > 5;
        }

        private Queue<long> GenerateRandomBgmQueue(List<long> loadedBgm)
        {
            var randomQueue = new Queue<long>();
            var shuffledItems = loadedBgm.Shuffle();
            foreach (var item in shuffledItems)
            {
                randomQueue.Enqueue(item);
            }
            return randomQueue;
        }

        public string PlayNextBgmInQueue()
        {
            var currentJob = playerState.GetCurrentJob();
            if (!CanPlayDynamicBgm(playerState.IsInsideInstance, currentJob) || playerState.IsInCombat)
            {
                return "";
            }

            if (!bgmFsm.IsActive || Plugin.Configuration!.JobConfiguration[currentJob].Bgm.Value
                != BgmKeys.Randomize)
            {
                return "";
            }

            LoadNextBgmInQueue();
            return GetBgmLabel(currentBgmKey);
        }

        private void LoadNextBgmInQueue()
        {
            bgmFsm.ResetToIntro();
            if (randomBgmQueue.Count == 0 || !AssetsManager.IsReady)
            {
                return;
            }

            var selectedBgm = randomBgmQueue.Dequeue();
            randomBgmQueue.Enqueue(selectedBgm);
            if (!bgmFsmStates.ContainsKey(selectedBgm))
            {
                return;
            }

            currentBgmKey = selectedBgm;

            PlayDynamicBgm();
        }

        private static void DisableGameBgm()
        {
            Service.GameConfig.Set(SystemConfigOption.IsSndBgm, true);
        }

        private void ResetGameBgm()
        {
            Service.GameConfig.Set(SystemConfigOption.IsSndBgm, gameBgmState);
        }

        private void LoadBgm(long bgm)
        {
            audioService.StopBgm();
            if (!bgmFsmStates.ContainsKey(bgm) || !AssetsManager.IsReady)
            {
                return;
            }
            Task.Run(() =>
            {
                CacheBgm(bgm);
                currentBgmKey = bgm;

                PlayDynamicBgm();
            });
        }

        private void PlayDynamicBgm()
        {
            soundFilesLoaded = audioService.LoadRegisteredBgm(currentBgmKey);
            bgmFsm.LoadBgmStates(bgmFsmStates[currentBgmKey]);

            if (soundFilesLoaded)
            {
                bgmFsm.Start();
            }
            else
            {
                Service.Log.Warning($"Could not start {currentBgmKey}, files are not ready");
            }
        }

        private void CacheBgm(long bgm)
        {
            if (!bgmFsmStates.TryGetValue(bgm, out var currentBgmStates))
            {
                return;
            }

            var bgmParts = currentBgmStates.SelectMany(entry => entry.Value.GetBgmPaths()).ToDictionary();
            audioService.RegisterBgmParts(bgm, bgmParts);
        }

        private List<long> CacheAllBgm(ISet<long> bgmKeysToLoad)
        {
            var bgmList = new List<long>();
            foreach (var states in bgmFsmStates)
            {
                if (bgmKeysToLoad.Count > 0 && !bgmKeysToLoad.Contains(states.Key))
                {
                    continue;
                }
                var bgmParts = states.Value.SelectMany(entry => entry.Value.GetBgmPaths()).ToDictionary();
                try
                {
                    var registered = audioService.RegisterBgmParts(states.Key, bgmParts);
                    if (registered)
                    {
                        bgmList.Add(states.Key);
                    }
                }
                catch (Exception e)
                {
                    Service.Log.Error(e, $"Error while loading [{states.Key}] with other BGMs");
                }
            }
            return bgmList;
        }

        private void OnDeath(object? sender, bool isDead)
        {
            if (isDead && bgmFsm.IsActive)
            {
                audioService.ApplyDeathEffect();
            }
            else
            {
                audioService.RemoveDeathEffect();
            }
        }

        public static string GetPathToAudio(string name)
        {
            return Path.Combine(AssetsManager.GetAssetsDirectory(), $"Audio\\{name}");
        }

        public void ToggleDynamicBgm(object? sender, bool dynamicBgmEnabled)
        {
            var currentJob = playerState.GetCurrentJob();
            if (CanPlayDynamicBgm(playerState.IsInsideInstance, currentJob))
            {
                DisableGameBgm();
                PrepareBgm(currentJob);
                if (playerState.IsDead)
                {
                    audioService.ApplyDeathEffect();
                }
            }
            else if (bgmFsm.IsActive)
            {
                bgmFsm.Disable();
                ResetGameBgm();
                audioService.RemoveDeathEffect();
            }
        }

        public void OnBgmBlacklistChanged(object? sender, EventArgs e)
        {
            var currentJob = playerState.GetCurrentJob();
            if (bgmFsm.IsActive && !CanPlayDynamicBgm(playerState.IsInsideInstance, currentJob))
            {
                bgmFsm.Disable();
                ResetGameBgm();
                audioService.RemoveDeathEffect();
            }
            else if (!bgmFsm.IsActive && CanPlayDynamicBgm(playerState.IsInsideInstance, currentJob))
            {
                DisableGameBgm();
                PrepareBgm(currentJob);
                if (playerState.IsDead)
                {
                    audioService.ApplyDeathEffect();
                }
            }
        }

        public void OnMuffledOnDeathChange(object? sender, bool muffledOnDeath)
        {
            if (muffledOnDeath && bgmFsm.IsActive && playerState.IsDead)
            {
                audioService.ApplyDeathEffect();
            }
            else
            {
                audioService.RemoveDeathEffect();
            }
        }

        public static string GetBgmLabel(long bgmKey)
        {
            var customBgmName = CustomBgmManager.GetProjectName(bgmKey);
            if (customBgmName != null)
            {
                return customBgmName;
            }
            return bgmKey switch
            {
                BgmKeys.Off => "Off",
                BgmKeys.Randomize => "Randomize",
                BgmKeys.BuryTheLight => "Bury the Light",
                BgmKeys.DevilTrigger => "Devil Trigger",
                BgmKeys.CrimsonCloud => "Crimson Cloud",
                BgmKeys.DevilsNeverCry => "Devils Never Cry - by InfamousDork04",
                BgmKeys.Subhuman => "Subhuman",
                _ => "Unknown",
            };
        }
    }
}
