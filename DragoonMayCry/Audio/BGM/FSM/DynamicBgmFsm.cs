using Dalamud.Plugin.Services;
using DragoonMayCry.Audio.BGM;
using DragoonMayCry.Audio.BGM.FSM.States;
using DragoonMayCry.Audio.BGM.FSM.States.BuryTheLight;
using DragoonMayCry.Score.Style.Rank;
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
    public class DynamicBgmFsm : IDisposable
    {
        public bool IsActive { get; private set; }

        private readonly PlayerState playerState;
        private readonly AudioService audioService;
        
        
        private readonly IFramework framework;
        private readonly Stopwatch stateTransitionStopwatch;
        private readonly StyleRankHandler styleRankHandler;

        // The current state.
        private IFsmState? currentState;
        private IFsmState? candidateState;
        
        private int nextTransitionTime = -1;
        private DoubleLinkedList<BgmState> bgmStates;
        private DoubleLinkedNode<BgmState> currentStateNode;
        private bool isInCombat = false;
        public bool SoundFilesLoaded { get; private set; } = false;
        
        private Dictionary<BgmState, IFsmState>? currentBgmStates;
        public DynamicBgmFsm(StyleRankHandler styleRankHandler)
        {
            this.styleRankHandler = styleRankHandler;
            this.styleRankHandler.StyleRankChange += OnRankChange;
            playerState = PlayerState.GetInstance();
            playerState.RegisterCombatStateChangeHandler(OnCombatChange);
            this.framework = Service.Framework;
            this.audioService = AudioService.Instance;
            stateTransitionStopwatch = new Stopwatch();

            bgmStates = new DoubleLinkedList<BgmState>(BgmState.Intro, BgmState.CombatLoop, BgmState.CombatPeak);
            
            
            currentStateNode = bgmStates.Head!;

            this.framework.Update += Update;
        }

        public void Start()
        {
            if (IsActive)
            {
                return;
            }

            currentStateNode = bgmStates.Head!;
            currentState = currentBgmStates?[currentStateNode.Value];
            IsActive = true;
            currentState?.Enter(false);
        }

        private void Update(IFramework framework)
        {
            if (!IsActive)
            {
                return;
            }

            currentState?.Update();

            if(playerState.IsInCombat && currentState?.ID == BgmState.Intro && candidateState == null)
            {
                currentStateNode = currentStateNode.Next;
            }

            if (stateTransitionStopwatch.IsRunning && stateTransitionStopwatch.Elapsed.TotalMilliseconds > nextTransitionTime)
            {
                GoToNextState();
            }
            else if (isInCombat && currentState?.ID == BgmState.Intro && candidateState == null)
            {
                Promote();
            }
        }

        private void CacheBgm()
        {
            if(currentBgmStates == null)
            {
                return;
            }
            int loadedStates = 0;
            foreach (var state in currentBgmStates.Values)
            {
                var registered = audioService.RegisterBgmParts(state.GetBgmPaths());
                if (registered)
                {
                    loadedStates++;
                }
            }
            SoundFilesLoaded = loadedStates == currentBgmStates.Count;
        }

        public void Dispose()
        {
            audioService.StopBgm();
            framework.Update -= Update;
        }

        public void ResetToIntro()
        {
            IsActive = false;
            
            currentStateNode = bgmStates.Head!;
            stateTransitionStopwatch.Reset();

            if (currentBgmStates == null)
            {
                return;
            }
            
            foreach (var entry in currentBgmStates)
            {
                entry.Value.Reset();
            }
            currentState = currentBgmStates[BgmState.Intro];
        }

        private void GoToNextState()
        {
            stateTransitionStopwatch.Reset();
            if (candidateState == null)
            {
                return;
            }

            if (currentState?.ID == BgmState.Intro && candidateState.ID == BgmState.CombatLoop)
            {
                // clear the intro here cause it's whack
                currentBgmStates?[BgmState.Intro].Reset();
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
            var fromVerse = currentState?.ID == BgmState.CombatPeak && candidateState.ID == BgmState.CombatLoop;
            currentState = candidateState;
            currentState.Enter(fromVerse);
            candidateState = null;
        }

        public void Promote()
        {
            if (currentStateNode.Next == null || candidateState?.ID == currentStateNode.Value || currentState == null)
            {
                return;
            }

            candidateState = currentBgmStates?[currentStateNode.Next.Value];
            stateTransitionStopwatch.Restart();
            nextTransitionTime = currentState.Exit(ExitType.Promotion);
        }

        public void LeaveCombat()
        {
            if(currentState == null)
            {
                return;
            }
            nextTransitionTime = currentState.Exit(ExitType.EndOfCombat);
            candidateState = currentBgmStates?[BgmState.Intro];
            stateTransitionStopwatch.Restart();
        }

        public void Demote()
        {
            // we can only go to the previous state for ranks S and above
            if (currentState?.ID != BgmState.CombatPeak || candidateState != null)
            {
                return;
            }

            candidateState = currentBgmStates?[currentStateNode.Previous!.Value];
            nextTransitionTime = currentState.Exit(ExitType.Demotion);
            stateTransitionStopwatch.Restart();
        }

        private void OnCombatChange(object? sender, bool isInCombat)
        {
            if (!IsActive)
            {
                return;
            }
            this.isInCombat = isInCombat; 
            
            if (isInCombat && currentState?.ID == BgmState.Intro)
            {
                Promote();
            }
            else
            {
                LeaveCombat();
            }
        }

        public void OnRankChange(object? sender, StyleRankHandler.RankChangeData rankChangeData)
        {
            if (rankChangeData.NewRank >= Score.Model.StyleType.S && currentState?.ID == BgmState.CombatLoop)
            {
                Promote();
            }
            else if (rankChangeData.NewRank < Score.Model.StyleType.S && currentState?.ID == BgmState.CombatPeak)
            {
                Demote();
            } else if(rankChangeData.NewRank >= Score.Model.StyleType.S && currentState?.ID == BgmState.CombatPeak && candidateState?.ID == BgmState.CombatLoop)
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

        public void LoadBgmStates(Dictionary<BgmState, IFsmState> states)
        {
            SoundFilesLoaded = false;
            currentBgmStates = states;
            CacheBgm();
        }

        public void Deactivate()
        {
            ResetToIntro();
            audioService.StopBgm();
        }
    }


}
