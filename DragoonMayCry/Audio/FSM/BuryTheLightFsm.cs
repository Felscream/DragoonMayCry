using Dalamud.Plugin.Services;
using DragoonMayCry.Audio.FSM.States;
using DragoonMayCry.Audio.FSM.States.BuryTheLight;
using DragoonMayCry.Score.Style;
using DragoonMayCry.State;
using DragoonMayCry.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

//This whole namespace is a landmine
namespace DragoonMayCry.Audio.FSM
{
    public class BuryTheLightFsm : IDisposable
    {
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
        public BuryTheLightFsm(AudioService audioService, PlayerState playerState, StyleRankHandler styleRankHandler, IFramework framework)
        {
            this.styleRankHandler = styleRankHandler;
            this.styleRankHandler.StyleRankChange += OnRankChange;
            this.playerState = playerState;
            playerState.RegisterCombatStateChangeHandler(OnCombatChange);
            playerState.RegisterInstanceChangeHandler(OnInstanceChange);
            this.framework = framework;
            this.audioService = audioService;
            stateTransitionStopwatch = new Stopwatch();
            FsmState intro = new BTLIntro(audioService);
            FsmState combat = new BTLCombatLoop(audioService);
            FsmState peak = new BTLPeak(audioService);
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
            return Path.Combine(
                Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!,
                $"Assets\\Audio\\bury_the_light\\{name}");
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
            if(!isActive)
            {
                return;
            }



            currentState.Update();
            if(candidateState?.ID == BgmState.CombatLoop && currentState.ID == BgmState.CombatPeak)
            {
                Service.Log.Debug($"State demotion {stateTransitionStopwatch.ElapsedMilliseconds} vs {nextTransitionTime}");
            }
            if(stateTransitionStopwatch.IsRunning && stateTransitionStopwatch.Elapsed.TotalMilliseconds > nextTransitionTime)
            {
                GoToNextState();
            }
        }

        public void CacheBgm()
        {
            foreach(KeyValuePair<BgmState, FsmState> entry in states){
                var bgmsParts = entry.Value.GetBgmPaths();
                foreach(KeyValuePair<BgmId, string> parts in bgmsParts)
                {
                    audioService.RegisterBgmPart(parts.Key, parts.Value);
                }
            }
            audioService.RegisterBgmPart(BgmId.CombatEnd, GetPathToAudio("end.mp3"));
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
            this.framework.Update -= Update;
        }

        public void ResetToIntro()
        {
            isActive = false;
            foreach (KeyValuePair<BgmState, FsmState> entry in states)
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
            
            if(currentState.ID == BgmState.Intro && candidateState.ID == BgmState.CombatLoop) {
                // clear the intro here cause it's whack
                states[BgmState.Intro].Reset();
            }
            if(candidateState.ID == BgmState.Intro)
            {
                currentStateNode = bgmStates.Head!;
            } else if (currentStateNode.Next != null && currentStateNode.Next.Value == candidateState.ID)
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
                && Plugin.Configuration!.EnableDynamicBgm;

            audioService.StopBgm();
            if (canStart)
            {
                Start();
            } else {
                ResetGameBgm();
            }
        }

        private void OnCombatChange(object? sender, bool isInCombat)
        {
            if (!isActive)
            {
                return;
            }
            if(isInCombat && currentState.ID == BgmState.Intro) {
                Promotion();
            } else
            {
                LeaveCombat();
            }
        }

        private void OnRankChange(object? sender, StyleRankHandler.RankChangeData rankChangeData)
        {
            if(rankChangeData.NewRank >= Score.Model.StyleType.S && currentState.ID == BgmState.CombatLoop)
            {
                Promotion();
            } else if(rankChangeData.NewRank < Score.Model.StyleType.S && currentState.ID == BgmState.CombatPeak)
            {
                Demotion();
            }
        }
    }


}
