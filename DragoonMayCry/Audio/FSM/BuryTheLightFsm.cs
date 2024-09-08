using Dalamud.Plugin.Services;
using DragoonMayCry.Audio.FSM.States;
using DragoonMayCry.Audio.FSM.States.BuryTheLight;
using DragoonMayCry.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
namespace DragoonMayCry.Audio.FSM
{
    public class BuryTheLightFsm : IDisposable
    {
        private Dictionary<BgmState, FsmState> states;
        // The current state.
        private FsmState currentState;
        private FsmState? candidateState;
        private AudioService audioService;
        private IFramework framework;
        private bool isActive;
        private Stopwatch stateTransitionStopwatch;
        private int nextTransitionTime = -1;
        private bool isGameBgmActive;
        private DoubleLinkedList<BgmState> bgmStates;
        private DoubleLinkedNode<BgmState> currentStateNode;
        public BuryTheLightFsm(AudioService audioService, IFramework framework)
        {
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
            if(Service.GameConfig.TryGet(Dalamud.Game.Config.SystemConfigOption.IsSndBgm, out bool val))
            {
                isGameBgmActive = val;
            }
            currentStateNode = bgmStates.Head!;
            currentState = states[currentStateNode.Value];
            DisableGameBgm();
            isActive = true;
            currentState.Enter();
        }

        public void Update(IFramework framework)
        {
            currentState.Update();

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

        public void Dispose()
        {
            //ResetGameBgm();
            this.framework.Update -= Update;
        }

        public void Reset()
        {
            isActive = false;
            foreach(KeyValuePair<BgmState, FsmState> entry in states)
            {
                entry.Value.Reset();
            }
            //ResetGameBgm();
        }

        public void ResetToIntro()
        {
            isActive = false;
            foreach (KeyValuePair<BgmState, FsmState> entry in states)
            {
                entry.Value.Reset();
            }
            currentState = states[BgmState.Intro];
        }

        private void DisableGameBgm()
        {
            Service.GameConfig.Set(Dalamud.Game.Config.SystemConfigOption.IsSndBgm, true);
        }

        private void ResetGameBgm()
        {
            Service.GameConfig.Set(Dalamud.Game.Config.SystemConfigOption.IsSndBgm, isGameBgmActive);
        }
        private void GoToNextState()
        {
            if(candidateState == null)
            {
                return;
            }
            stateTransitionStopwatch.Reset();
            if(candidateState.ID == BgmState.Intro)
            {
                currentStateNode = bgmStates.Head!;
            } else if (currentStateNode.Next != null)
            {
                currentStateNode = currentStateNode.Next!;
            }
            currentState = candidateState;
            currentState.Enter();
            candidateState = null;
            
        }
        private void GoToPreviousState()
        {
            if (candidateState == null || currentStateNode.Previous == null || currentStateNode.Previous.Value == BgmState.Intro)
            {
                return;
            }
        }

        public void TriggerTransition()
        {
            if (currentStateNode.Next == null || candidateState?.ID == currentStateNode.Value)
            {
                return;
            }

            candidateState = states[currentStateNode.Next.Value];
            stateTransitionStopwatch.Restart();
            nextTransitionTime = currentState.Exit(false);
        }

        public void LeaveCombat()
        {
            nextTransitionTime = currentState.Exit(true);
            candidateState = states[BgmState.Intro];
            stateTransitionStopwatch.Restart();
        }
    }


}
