using Dalamud.Game.Config;
using DragoonMayCry.Audio.BGM.FSM;
using DragoonMayCry.Audio.BGM.FSM.States;
using DragoonMayCry.Audio.BGM.FSM.States.BuryTheLight;
using DragoonMayCry.Audio.BGM.FSM.States.CrimsonCloud;
using DragoonMayCry.Audio.BGM.FSM.States.DevilsNeverCry;
using DragoonMayCry.Audio.BGM.FSM.States.DevilTrigger;
using DragoonMayCry.Audio.BGM.FSM.States.Subhuman;
using DragoonMayCry.Configuration;
using DragoonMayCry.Data;
using DragoonMayCry.Score.Rank;
using DragoonMayCry.State;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DragoonMayCry.Audio.BGM
{
    public class DynamicBgmService : IDisposable
    {
        public enum Bgm
        {
            BuryTheLight,
            DevilTrigger,
            CrimsonCloud,
            Subhuman,
            DevilsNeverCry,
            None
        }


        private readonly Dictionary<BgmState, IFsmState> buryTheLightStates;
        private readonly Dictionary<BgmState, IFsmState> devilTriggerStates;
        private readonly Dictionary<BgmState, IFsmState> crimsonCloudStates;
        private readonly Dictionary<BgmState, IFsmState> subhumanStates;
        private readonly Dictionary<BgmState, IFsmState> devilsNeverCryStates;
        private readonly Dictionary<Bgm, Dictionary<BgmState, IFsmState>> bgmFsmStates = new();
        private readonly DynamicBgmFsm bgmFsm;
        private readonly PlayerState playerState;
        private readonly AudioService audioService;

        private Queue<Bgm> randomBgmQueue = new();
        private bool gameBgmState;
        private Bgm currentBgm = Bgm.None;
        private bool soundFilesLoaded;
        private uint currentTerritory;

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

            IFsmState btlIntro = new BTLIntro(audioService);
            IFsmState btlCombat = new BTLVerse(audioService);
            IFsmState btlPeak = new BtlChorus(audioService);
            buryTheLightStates = new Dictionary<BgmState, IFsmState>
            {
                { BgmState.Intro, btlIntro },
                { BgmState.CombatLoop, btlCombat },
                { BgmState.CombatPeak, btlPeak },
            };

            IFsmState dtIntro = new DTIntro(audioService);
            IFsmState dtCombat = new DTVerse(audioService);
            IFsmState dtPeak = new DTChorus(audioService);
            devilTriggerStates = new Dictionary<BgmState, IFsmState>
            {
                { BgmState.Intro, dtIntro },
                { BgmState.CombatLoop, dtCombat },
                { BgmState.CombatPeak, dtPeak },
            };

            IFsmState ccIntro = new CCIntro(audioService);
            IFsmState ccCombat = new CCVerse(audioService);
            IFsmState ccPeak = new CCChorus(audioService);
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

            bgmFsmStates.Add(Bgm.BuryTheLight, buryTheLightStates);
            bgmFsmStates.Add(Bgm.DevilTrigger, devilTriggerStates);
            bgmFsmStates.Add(Bgm.CrimsonCloud, crimsonCloudStates);
            bgmFsmStates.Add(Bgm.Subhuman, subhumanStates);
            bgmFsmStates.Add(Bgm.DevilsNeverCry, devilsNeverCryStates);
        }

        public DynamicBgmFsm GetFsm()
        {
            return bgmFsm;
        }

        public void Dispose()
        {
            bgmFsm.Dispose();
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
                   && Plugin.Configuration.JobConfiguration[job].Bgm.Value != JobConfiguration.BgmConfiguration.Off
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
            if (!loaded || !bgmFsmStates.ContainsKey(currentBgm))
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
            var configuration = jobConfiguration.Bgm.Value;

            if (configuration == JobConfiguration.BgmConfiguration.Off)
            {
                return;
            }

            if (playerState.IsDead)
            {
                audioService.ApplyDeathEffect();
            }

            if (configuration == JobConfiguration.BgmConfiguration.Randomize)
            {
                bgmFsm.loadNewBgm = LoadNextBgmInQueue;
                Task.Run(() =>
                {
                    var loadedBgm = CacheAllBgm();
                    randomBgmQueue = GenerateRandomBgmQueue(loadedBgm);
                    LoadNextBgmInQueue();
                });

                return;
            }

            if (bgmFsm.loadNewBgm != null)
            {
                bgmFsm.loadNewBgm -= LoadNextBgmInQueue;
            }

            var selectedBgm = configuration switch
            {
                JobConfiguration.BgmConfiguration.BuryTheLight => Bgm.BuryTheLight,
                JobConfiguration.BgmConfiguration.DevilTrigger => Bgm.DevilTrigger,
                JobConfiguration.BgmConfiguration.CrimsonCloud => Bgm.CrimsonCloud,
                JobConfiguration.BgmConfiguration.Subhuman => Bgm.Subhuman,
                JobConfiguration.BgmConfiguration.DevilsNeverCry => Bgm.DevilsNeverCry,
                _ => Bgm.BuryTheLight,
            };
            LoadBgm(selectedBgm);
        }

        private Queue<Bgm> GenerateRandomBgmQueue(List<Bgm> loadedBgm)
        {
            var bgmTemp = new List<Bgm>(loadedBgm);
            var randomQueue = new Queue<Bgm>();
            var random = new Random();
            while (bgmTemp.Count > 0)
            {
                var index = random.Next(0, bgmTemp.Count);
                randomQueue.Enqueue(bgmTemp[index]);
                bgmTemp.RemoveAt(index);
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

            if (!bgmFsm.IsActive || Plugin.Configuration!.JobConfiguration[currentJob].Bgm.Value != JobConfiguration.BgmConfiguration.Randomize)
            {
                return "";
            }

            LoadNextBgmInQueue();
            return GetBgmLabel(currentBgm);
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

            currentBgm = selectedBgm;

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

        private void LoadBgm(Bgm bgm)
        {
            audioService.StopBgm();
            if (!bgmFsmStates.ContainsKey(bgm) || !AssetsManager.IsReady)
            {
                return;
            }
            Task.Run(() =>
            {
                CacheBgm(bgm);
                currentBgm = bgm;

                PlayDynamicBgm();
            });
        }

        private void PlayDynamicBgm()
        {
            soundFilesLoaded = audioService.LoadRegisteredBgm(currentBgm);
            bgmFsm.LoadBgmStates(bgmFsmStates[currentBgm]);

            if (soundFilesLoaded)
            {
                bgmFsm.Start();
            }
            else
            {
                Service.Log.Warning($"Could not start {currentBgm}, files are not ready");
            }
        }

        private void CacheBgm(Bgm bgm)
        {
            if (!bgmFsmStates.TryGetValue(bgm, out var currentBgmStates))
            {
                return;
            }

            var bgmParts = currentBgmStates.SelectMany(entry => entry.Value.GetBgmPaths()).ToDictionary();
            audioService.RegisterBgmParts(bgm, bgmParts);
        }

        private List<Bgm> CacheAllBgm()
        {
            var bgmList = new List<Bgm>();
            foreach (var states in bgmFsmStates)
            {
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

        public static string GetBgmLabel(JobConfiguration.BgmConfiguration bgm)
        {
            return bgm switch
            {
                JobConfiguration.BgmConfiguration.Off => "Off",
                JobConfiguration.BgmConfiguration.BuryTheLight => "Bury the Light",
                JobConfiguration.BgmConfiguration.DevilsNeverCry => "Devils Never Cry",
                JobConfiguration.BgmConfiguration.DevilTrigger => "Devil Trigger",
                JobConfiguration.BgmConfiguration.CrimsonCloud => "Crimson Cloud",
                JobConfiguration.BgmConfiguration.Subhuman => "Subhuman",
                JobConfiguration.BgmConfiguration.Randomize => "Randomize",
                _ => "Unknown"
            };
        }
        
        private static string GetBgmLabel(Bgm bgm)
        {
            return bgm switch
            {
                Bgm.BuryTheLight => "Bury the Light",
                Bgm.DevilTrigger => "Devil Trigger",
                Bgm.CrimsonCloud => "Crimson Cloud",
                Bgm.DevilsNeverCry => "Devils Never Cry",
                Bgm.Subhuman => "Subhuman",
                _ => "Unknown"
            };
        }
    }
}
