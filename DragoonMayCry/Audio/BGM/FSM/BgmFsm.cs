using Dalamud.Plugin.Services;
using DragoonMayCry.Audio.BGM;
using DragoonMayCry.Audio.BGM.FSM.States;
using DragoonMayCry.Audio.BGM.FSM.States.BuryTheLight;
using DragoonMayCry.Score.Style;
using DragoonMayCry.State;
using DragoonMayCry.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

//This whole namespace is a landmine
namespace DragoonMayCry.Audio.BGM.FSM
{
    public class BgmFsm : IDisposable
    {
        enum BgmName{
            BuryTheLight
        }

        private readonly PlayerState playerState;
        private readonly AudioService audioService;
        private readonly Dictionary<BgmState, FsmState> states;
        private readonly IFramework framework;
        private readonly Stopwatch stateTransitionStopwatch;
        private readonly StyleRankHandler styleRankHandler;

        // The current state.
        private FsmState currentState;
        private FsmState? candidateState;
        private bool isActive;
        private int nextTransitionTime = -1;
        private DoubleLinkedList<BgmState> bgmStates;
        private DoubleLinkedNode<BgmState> currentStateNode;
        private bool isInCombat = false;
        private bool soundFilesLoaded = false;
        private static BgmName currentBgm = BgmName.BuryTheLight;
        public BgmFsm(AudioService audioService, PlayerState playerState, StyleRankHandler styleRankHandler, IFramework framework)
        {
            AssetsManager.AssetsReady += OnAssetsAvailable;
            this.styleRankHandler = styleRankHandler;
            this.styleRankHandler.StyleRankChange += OnRankChange;
            this.playerState = playerState;
            playerState.RegisterCombatStateChangeHandler(OnCombatChange);
            playerState.RegisterInstanceChangeHandler(OnInstanceChange);
            this.framework = framework;
            this.audioService = audioService;
            stateTransitionStopwatch = new Stopwatch();
            FsmState intro = new BTLIntro(audioService);
            FsmState combat = new BTLVerse(audioService);
            FsmState peak = new BTLChorus(audioService);
            bgmStates = new DoubleLinkedList<BgmState>(BgmState.Intro, BgmState.CombatLoop, BgmState.CombatPeak);
            currentStateNode = bgmStates.Head!;
            states = new Dictionary<BgmState, FsmState>
            {
                { BgmState.Intro, intro },
                { BgmState.CombatLoop, combat },
                { BgmState.CombatPeak, peak },
            };
            currentState = intro;

            this.framework.Update += Update;
        }

        public static string GetPathToAudio(string name)
        {
            return Path.Combine(AssetsManager.GetAssetsDirectory(), $"Audio\\{currentBgm}\\{name}");
        }

        public void Start()
        {
            if (isActive)
            {
                return;
            }

            currentStateNode = bgmStates.Head!;
            currentState = states[currentStateNode.Value];
            DisableGameBgm();
            isActive = true;
            currentState.Enter(false);
        }

        public void Update(IFramework framework)
        {
            if (!isActive)
            {
                return;
            }

            currentState.Update();
            if (stateTransitionStopwatch.IsRunning && stateTransitionStopwatch.Elapsed.TotalMilliseconds > nextTransitionTime)
            {
                GoToNextState();
            }
        }

        public void OnAssetsAvailable(object? sender, bool loaded)
        {
            if (!loaded)
            {
                return;
            }
            CacheBgm();
        }

        public void CacheBgm()
        {
            int loadedStates = 0;
            foreach (var state in states.Values)
            {
                var registered = audioService.RegisterBgmParts(state.GetBgmPaths());
                if (registered)
                {
                    loadedStates++;
                }
            }
            soundFilesLoaded = loadedStates == states.Count;
        }

        public void CheckCurrentRank()
        {
            if (styleRankHandler.CurrentStyle.Value >= Score.Model.StyleType.S && currentState.ID == BgmState.CombatLoop)
            {
                Promotion();
            }
            else if (styleRankHandler.CurrentStyle.Value < Score.Model.StyleType.S && currentState.ID == BgmState.CombatPeak)
            {
                Demotion();
            }
        }

        public void Dispose()
        {
            audioService.StopBgm();
            framework.Update -= Update;
        }

        public void ResetToIntro()
        {
            isActive = false;
            foreach (var entry in states)
            {
                entry.Value.Reset();
            }
            currentState = states[BgmState.Intro];
            currentStateNode = bgmStates.Head!;
            stateTransitionStopwatch.Reset();
        }

        private void DisableGameBgm()
        {
            Service.GameConfig.Set(Dalamud.Game.Config.SystemConfigOption.IsSndBgm, true);
        }

        private void ResetGameBgm()
        {
            Service.GameConfig.Set(Dalamud.Game.Config.SystemConfigOption.IsSndBgm, false);
        }

        private void GoToNextState()
        {
            stateTransitionStopwatch.Reset();
            if (candidateState == null)
            {
                return;
            }

            if (currentState.ID == BgmState.Intro && candidateState.ID == BgmState.CombatLoop)
            {
                // clear the intro here cause it's whack
                states[BgmState.Intro].Reset();
            }
            if (candidateState.ID == BgmState.Intro)
            {
                currentStateNode = bgmStates.Head!;
            }
            else if (currentStateNode.Next != null && currentStateNode.Next.Value == candidateState.ID)
            {
                currentStateNode = currentStateNode.Next!;
            }
            else
            {
                currentStateNode = bgmStates.Find(candidateState.ID)!;
            }
            var fromVerse = currentState.ID == BgmState.CombatPeak && candidateState.ID == BgmState.CombatLoop;
            currentState = candidateState;
            currentState.Enter(fromVerse);
            candidateState = null;
        }

        public void Promotion()
        {
            if (currentStateNode.Next == null || candidateState?.ID == currentStateNode.Value)
            {
                return;
            }

            candidateState = states[currentStateNode.Next.Value];
            stateTransitionStopwatch.Restart();
            nextTransitionTime = currentState.Exit(ExitType.Promotion);
        }

        public void LeaveCombat()
        {
            nextTransitionTime = currentState.Exit(ExitType.EndOfCombat);
            candidateState = states[BgmState.Intro];
            stateTransitionStopwatch.Restart();
        }

        public void Demotion()
        {
            // we can only go to the previous state for ranks S and above
            if (currentState.ID != BgmState.CombatPeak || candidateState != null)
            {
                return;
            }

            candidateState = states[currentStateNode.Previous!.Value];
            nextTransitionTime = currentState.Exit(ExitType.Demotion);
            stateTransitionStopwatch.Restart();
        }

        private void OnInstanceChange(object? sender, bool isInInstance)
        {
            ResetToIntro();
            var canStart = playerState.Player != null
                && !playerState.IsInPvp()
                && isInInstance
                && Plugin.Configuration!.EnableDynamicBgm
                && soundFilesLoaded;

            audioService.StopBgm();
            if (canStart)
            {
                Start();
            }
            else
            {
                ResetGameBgm();
            }
        }

        private void OnCombatChange(object? sender, bool isInCombat)
        {
            if (!isActive)
            {
                return;
            }
            if (isInCombat && currentState.ID == BgmState.Intro)
            {
                Promotion();
            }
            else
            {
                LeaveCombat();
            }
        }

        public void OnRankChange(object? sender, StyleRankHandler.RankChangeData rankChangeData)
        {
            if (rankChangeData.NewRank >= Score.Model.StyleType.S && currentState.ID == BgmState.CombatLoop)
            {
                Promotion();
            }
            else if (rankChangeData.NewRank < Score.Model.StyleType.S && currentState.ID == BgmState.CombatPeak)
            {
                Demotion();
            } else if(rankChangeData.NewRank >= Score.Model.StyleType.S && currentState.ID == BgmState.CombatPeak && candidateState?.ID == BgmState.CombatLoop)
            {
                // Cancel demotion if player was being demoted but went back to S before the demotion transition started.
                bool canCancel = currentState.CancelExit();
                if(!canCancel)
                {
                    return;
                }
                candidateState = null;
                stateTransitionStopwatch.Reset();
            }
        }
    }


}
