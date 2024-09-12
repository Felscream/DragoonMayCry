using Dalamud.Game.Config;
using DragoonMayCry.Audio.BGM.FSM;
using DragoonMayCry.Audio.BGM.FSM.States;
using DragoonMayCry.Audio.BGM.FSM.States.BuryTheLight;
using DragoonMayCry.Score.Style.Rank;
using DragoonMayCry.State;
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
            DevilTrigger
        }
        private readonly Dictionary<BgmState, IFsmState> buryTheLightStates;
        private readonly Dictionary<BgmState, IFsmState> devilTriggerStates;
        private readonly Dictionary<Bgm, Dictionary<BgmState, IFsmState>> bgmStates = new();
        private readonly DynamicBgmFsm bgmFsm;
        private readonly PlayerState playerState;
        private readonly AudioService audioService;
        private bool gameBgmState;
        private Bgm currentBgm = Bgm.BuryTheLight;
        public DynamicBgmService(StyleRankHandler styleRankHandler)
        {
            bgmFsm = new DynamicBgmFsm(styleRankHandler);
            AssetsManager.AssetsReady += OnAssetsAvailable;
            Service.GameConfig.SystemChanged += OnGameSystemConfigChange;
            playerState = PlayerState.GetInstance();
            playerState.RegisterInstanceChangeHandler(OnInstanceChange);
            
            gameBgmState = Service.GameConfig.System.GetBool("IsSndBgm");
            audioService = AudioService.Instance;

            IFsmState intro = new BTLIntro(audioService);
            IFsmState combat = new BTLVerse(audioService);
            IFsmState peak = new BTLChorus(audioService);
            buryTheLightStates = new Dictionary<BgmState, IFsmState>
            {
                { BgmState.Intro, intro },
                { BgmState.CombatLoop, combat },
                { BgmState.CombatPeak, peak },
            };

            bgmStates.Add(Bgm.BuryTheLight, buryTheLightStates);
        }


        public DynamicBgmFsm GetFsm()
        {
            return bgmFsm;
        }

        public void Dispose()
        {
            bgmFsm.Dispose();
        }

        private void OnGameSystemConfigChange(object? sender, ConfigChangeEvent e)
        {
            var configOption = e.Option.ToString();
            if(configOption == "IsSndBgm")
            {
                gameBgmState = Service.GameConfig.System.GetBool("IsSndBgm");
            }
        }

        private void OnInstanceChange(object? sender, bool insideInstance)
        {
            if(insideInstance)
            {
                gameBgmState = Service.GameConfig.System.GetBool("IsSndBgm");
            }

            if (CanPlayDynamicBgm(insideInstance) && bgmFsm.SoundFilesLoaded)
            {
                DisableGameBgm();
            } else if(!insideInstance)
            {
                ResetGameBgm();
            }
        }

        private bool CanPlayDynamicBgm(bool isInInstance)
        {
            return playerState.Player != null
                            && !playerState.IsInPvp()
                            && isInInstance
                            && Plugin.Configuration!.EnableDynamicBgm
                            && playerState.IsCombatJob();
        }

        private void OnAssetsAvailable(object? sender, bool loaded)
        {
            if (!loaded || !bgmStates.ContainsKey(currentBgm))
            {
                return;
            }

            bgmFsm.LoadBgmStates(bgmStates[currentBgm]);
        }

        private void DisableGameBgm()
        {
            Service.GameConfig.Set(SystemConfigOption.IsSndBgm, true);
        }

        private void ResetGameBgm()
        {
            Service.GameConfig.Set(SystemConfigOption.IsSndBgm, gameBgmState);
        }

        private void LoadBgm(Bgm bgm)
        {
            if(!bgmStates.ContainsKey(currentBgm) || !AssetsManager.IsReady)
            {
                return;
            }
            currentBgm = bgm;
            audioService.StopBgm();
            audioService.ClearBgmCache();
            bgmFsm.LoadBgmStates(bgmStates[currentBgm]);
        }

        public static string GetPathToAudio(string name)
        {
            return Path.Combine(AssetsManager.GetAssetsDirectory(), $"Audio\\{name}");
        }

        public void ToggleDynamicBgm(object? sender, bool dynamicBgmEnabled)
        {

        }
    }
}
