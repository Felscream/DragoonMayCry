using Dalamud.Plugin.Services;
using DragoonMayCry.Audio.FSM.States;
using DragoonMayCry.Audio.FSM.States.BuryTheLight;
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
        public BuryTheLightFsm(AudioService audioService, IFramework framework)
        {
            this.framework = framework;
            this.audioService = audioService;
            stateTransitionStopwatch = new Stopwatch();
            FsmState intro = new BTLIntro(BgmState.Intro, audioService);
            FsmState combat = new BTLCombatLoop(BgmState.CombatLoop, audioService);
            states = new Dictionary<BgmState, FsmState>();
            states.Add(BgmState.Intro, intro);
            states.Add(BgmState.CombatLoop, combat);
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
            DisableGameBgm();
            isActive = true;
            currentState.Enter();
        }

        public void Update(IFramework framework)
        {
            currentState.Update();

            if(stateTransitionStopwatch.IsRunning && new decimal(stateTransitionStopwatch.Elapsed.TotalMilliseconds) > nextTransitionTime)
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
            currentState = candidateState;
            currentState.Enter();
            candidateState = null;
            
        }

        public void TriggerTransition(BgmState bgmState)
        {
            if (!states.ContainsKey(bgmState) || currentState.ID == bgmState || candidateState?.ID == bgmState)
            {
                return;
            }
            candidateState = states[bgmState];
            stateTransitionStopwatch.Restart();
            nextTransitionTime = currentState.Exit();
        }
    }


}
