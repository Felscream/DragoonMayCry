using Dalamud.Game.Config;
using DragoonMayCry.Audio.BGM.FSM;
using DragoonMayCry.Audio.BGM.FSM.States;
using DragoonMayCry.Audio.BGM.FSM.States.BuryTheLight;
using DragoonMayCry.Audio.BGM.FSM.States.DevilTrigger;
using DragoonMayCry.Configuration;
using DragoonMayCry.Data;
using DragoonMayCry.Score.Rank;
using DragoonMayCry.State;
using FFXIVClientStructs.FFXIV.Common.Lua;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FFXIVClientStructs.FFXIV.Client.System.String.Utf8String.Delegates;

namespace DragoonMayCry.Audio.BGM
{
    public class DynamicBgmService : IDisposable
    {
        public enum Bgm
        {
            BuryTheLight,
            DevilTrigger,
            CrimsonCloud,
            None
        }

        
        private readonly Dictionary<BgmState, IFsmState> buryTheLightStates;
        private readonly Dictionary<BgmState, IFsmState> devilTriggerStates;
        private readonly Dictionary<BgmState, IFsmState> crimsonCloudStates;
        private readonly Dictionary<Bgm, Dictionary<BgmState, IFsmState>> bgmStates = new();
        private readonly List<Bgm> randomBgmList = new();
        private readonly DynamicBgmFsm bgmFsm;
        private readonly PlayerState playerState;
        private readonly AudioService audioService;
        private bool gameBgmState;
        private Bgm currentBgm = Bgm.None;
        private JobIds currentJob = JobIds.OTHER;
        private bool soundFilesLoaded;
        private Random random = new Random();

        public DynamicBgmService(StyleRankHandler styleRankHandler)
        {
            bgmFsm = new DynamicBgmFsm(styleRankHandler);
            AssetsManager.AssetsReady += OnAssetsAvailable;
            playerState = PlayerState.GetInstance();
            playerState.RegisterInstanceChangeHandler(OnInstanceChange);
            playerState.RegisterJobChangeHandler(OnJobChange);
            playerState.RegisterPvpStateChangeHandler(OnPvpStateChange);
            
            gameBgmState = Service.GameConfig.System.GetBool("IsSndBgm");
            audioService = AudioService.Instance;

            IFsmState btlIntro = new BTLIntro(audioService);
            IFsmState btlCombat = new BTLVerse(audioService);
            IFsmState btlPeak = new BTLChorus(audioService);
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

            bgmStates.Add(Bgm.BuryTheLight, buryTheLightStates);
            bgmStates.Add(Bgm.DevilTrigger, devilTriggerStates);
            bgmStates.Add(Bgm.CrimsonCloud, crimsonCloudStates);
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
            if(!insideInstance)
            {
                bgmFsm.Deactivate();
                ResetGameBgm();
            } else
            {
                gameBgmState = Service.GameConfig.System.GetBool("IsSndBgm");
            }
        }

        // Is triggered when player in instanciated in a new territory
        // Grab the current job here and play
        private void OnJobChange(object? sender, JobIds job)
        {
            var insideInstance = playerState.IsInsideInstance;
            currentJob = job;
            if (!CanPlayDynamicBgm(insideInstance))
            {
                if (insideInstance && bgmFsm.IsActive)
                {
                    bgmFsm.Deactivate();
                    ResetGameBgm();
                }
                return;
            }
            DisableGameBgm();
            PrepareBgm(currentJob);
        }

        private void OnPvpStateChange(object? sender, bool inPvp)
        {
            var insideInstance = playerState.IsInsideInstance;
            if (!CanPlayDynamicBgm(insideInstance))
            {
                if (insideInstance && bgmFsm.IsActive)
                {
                    bgmFsm.Deactivate();
                    ResetGameBgm();
                }
                return;
            }
            DisableGameBgm();
            PrepareBgm(currentJob);
        }

        public void OnJobEnableChange(object? sender, JobIds job)
        {
            if(job != currentJob)
            {
                return;
            }
            var isInsideInstance = playerState.IsInsideInstance;
            if (CanPlayDynamicBgm(isInsideInstance) && !bgmFsm.IsActive)
            {
                gameBgmState = Service.GameConfig.System.GetBool("IsSndBgm");
                DisableGameBgm();
                PrepareBgm(currentJob);
            } else if (isInsideInstance && bgmFsm.IsActive)
            {
                bgmFsm.Deactivate();
                ResetGameBgm();
            }
        }

        private bool CanPlayDynamicBgm(bool isInInstance)
        {
            return playerState.Player != null
                            && !playerState.IsInPvp()
                            && isInInstance
                            && Plugin.Configuration!.EnableDynamicBgm
                            && Plugin.Configuration.JobConfiguration.ContainsKey(currentJob)
                            && Plugin.Configuration.JobConfiguration[currentJob].EnableDmc
                            && Plugin.Configuration.JobConfiguration[currentJob].Bgm.Value != JobConfiguration.BgmConfiguration.Off;
        }

        private void OnAssetsAvailable(object? sender, bool loaded)
        {
            if (!loaded || !bgmStates.ContainsKey(currentBgm))
            {
                return;
            }
            if (!CanPlayDynamicBgm(playerState.IsInsideInstance))
            {
                return;
            }
            bgmFsm.Deactivate();
            DisableGameBgm();
            PrepareBgm(GetCurrentJob());
        }

        private JobIds GetCurrentJob()
        {
            return currentJob != JobIds.OTHER ? currentJob : playerState.GetCurrentJob();
        }

        private void PrepareBgm(JobIds job)
        {
            bgmFsm.Deactivate();
            if (!Plugin.Configuration!.JobConfiguration.ContainsKey(job))
            {
                return;
            }
            JobConfiguration.BgmConfiguration configuration = Plugin.Configuration!.JobConfiguration[job].Bgm.Value;
            
            if(configuration == JobConfiguration.BgmConfiguration.Off)
            {
                return;
            }
            
            if(configuration == JobConfiguration.BgmConfiguration.Randomize)
            {
                bgmFsm.loadNewBgm = LoadRandomBgm;
                Task.Run(() =>
                {
                    CacheAllBgm();
                    LoadRandomBgm();
                });
                
                return;
            }

            if(bgmFsm.loadNewBgm != null)
            {
                bgmFsm.loadNewBgm -= LoadRandomBgm;
            }
                 
            var selectedBgm = configuration switch
            {
                JobConfiguration.BgmConfiguration.BuryTheLight => Bgm.BuryTheLight,
                JobConfiguration.BgmConfiguration.DevilTrigger => Bgm.DevilTrigger,
                JobConfiguration.BgmConfiguration.CrimsonCloud => Bgm.CrimsonCloud,
                _ => Bgm.BuryTheLight,
            };
            LoadBgm(selectedBgm);
        }

        private void LoadRandomBgm()
        {
            bgmFsm.ResetToIntro();
            if (randomBgmList.Count == 0)
            {
                return;
            }
            var selectionPool = new List<Bgm>(randomBgmList);
            selectionPool.Remove(currentBgm);
            var rand = random.Next(selectionPool.Count);
            var selectedBgm = selectionPool[rand];
            if (!bgmStates.ContainsKey(selectedBgm) || !AssetsManager.IsReady)
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
            if (!bgmStates.ContainsKey(bgm) || !AssetsManager.IsReady)
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
            bgmFsm.LoadBgmStates(bgmStates[currentBgm]);

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
            if (!bgmStates.ContainsKey(bgm))
            {
                return;
            }
            var currentBgmStates = bgmStates[bgm];
            if (currentBgmStates == null)
            {
                return;
            }

            var bgmParts = currentBgmStates.SelectMany(entry => entry.Value.GetBgmPaths()).ToDictionary();
            audioService.RegisterBgmParts(bgm, bgmParts);
        }

        private void CacheAllBgm()
        {
            foreach(KeyValuePair<Bgm, Dictionary<BgmState, IFsmState>> entry in bgmStates)
            {
                var bgmParts = entry.Value.SelectMany(entry => entry.Value.GetBgmPaths()).ToDictionary();
                try
                {
                    var registered = audioService.RegisterBgmParts(entry.Key, bgmParts);
                    if (registered)
                    {
                        randomBgmList.Add(entry.Key);
                    }
                    
                } catch(Exception e)
                {
                    Service.Log.Error(e, $"Error while loading [{entry.Key}] with other BGMs");
                }
                
            }
        }

        public static string GetPathToAudio(string name)
        {
            return Path.Combine(AssetsManager.GetAssetsDirectory(), $"Audio\\{name}");
        }

        public void ToggleDynamicBgm(object? sender, bool dynamicBgmEnabled)
        {
            bgmFsm.Deactivate();

            if (CanPlayDynamicBgm(playerState.IsInsideInstance))
            {
                DisableGameBgm();
                PrepareBgm(GetCurrentJob());
            }
            else if (!playerState.IsInsideInstance)
            {
                ResetGameBgm();
            }
        }
        public static string GetBgmLabel(JobConfiguration.BgmConfiguration bgm)
        {
            return bgm switch
            {
                JobConfiguration.BgmConfiguration.Off => "Off",
                JobConfiguration.BgmConfiguration.BuryTheLight => "Bury the Light",
                JobConfiguration.BgmConfiguration.DevilTrigger => "Devil Trigger",
                JobConfiguration.BgmConfiguration.CrimsonCloud => "Crimson Cloud",
                JobConfiguration.BgmConfiguration.Randomize => "Randomize",
                _ => "Unknown"
            };
        }
    }
}
