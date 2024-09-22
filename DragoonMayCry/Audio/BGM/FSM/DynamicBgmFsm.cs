using Dalamud.Plugin.Services;
using DragoonMayCry.Audio.BGM.FSM.States;
using DragoonMayCry.Score.Rank;
using DragoonMayCry.State;
using DragoonMayCry.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;

//This whole namespace is a landmine
namespace DragoonMayCry.Audio.BGM.FSM
{
    public class DynamicBgmFsm : IDisposable
    {
        public delegate void LoadNewBgm();
        public LoadNewBgm? loadNewBgm;
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
        private readonly DoubleLinkedList<BgmState> bgmStates;
        private DoubleLinkedNode<BgmState> currentStateNode;


        private Dictionary<BgmState, IFsmState>? currentBgmStates;
        public DynamicBgmFsm(StyleRankHandler styleRankHandler)
        {
            this.styleRankHandler = styleRankHandler;
            this.styleRankHandler.StyleRankChange += OnRankChange;
            playerState = PlayerState.GetInstance();
            playerState.RegisterCombatStateChangeHandler(OnCombatChange);
            framework = Service.Framework;
            audioService = AudioService.Instance;
            stateTransitionStopwatch = new Stopwatch();

            bgmStates = new DoubleLinkedList<BgmState>(BgmState.Intro, BgmState.CombatLoop, BgmState.CombatPeak);

            currentStateNode = bgmStates.Head!;

            framework.Update += Update;
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

            if (playerState.IsInCombat && currentState?.ID == BgmState.Intro && candidateState == null)
            {
                audioService.FadeOutBgm(1600);
                currentState.Exit(ExitType.ImmediateExit);
                currentStateNode = bgmStates.Find(BgmState.CombatLoop)!;
                currentState = currentBgmStates![currentStateNode.Value];
                currentState?.Enter(false);

            }

            if (stateTransitionStopwatch.IsRunning && stateTransitionStopwatch.Elapsed.TotalMilliseconds > nextTransitionTime)
            {
                GoToNextState();
            }
            else if (playerState.IsInCombat && currentState?.ID == BgmState.Intro && candidateState == null)
            {
                Promote();
            }
        }

        public void Dispose()
        {
            audioService.StopBgm();
            framework.Update -= Update;
        }

        public void ResetToIntro()
        {
            IsActive = false;
            stateTransitionStopwatch.Reset();
            currentStateNode = bgmStates.Head!;
            candidateState = null;

            if (currentBgmStates != null)
            {
                currentState = currentBgmStates[BgmState.Intro];
                foreach (var entry in currentBgmStates)
                {
                    entry.Value.Reset();
                }
            }

            audioService.FadeOutBgm(3000);
        }

        private void GoToNextState()
        {
            stateTransitionStopwatch.Reset();
            if (candidateState == null)
            {
                return;
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
            if (candidateState.ID == BgmState.Intro)
            {
                loadNewBgm?.Invoke();
            }
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
            if (currentState == null || candidateState?.ID == BgmState.Intro)
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
            if (rankChangeData.NewRank >= Score.Model.StyleType.S && currentState?.ID == BgmState.CombatLoop && candidateState?.ID != BgmState.CombatPeak)
            {
                Promote();
            }
            else if (rankChangeData.NewRank < Score.Model.StyleType.S && currentState?.ID == BgmState.CombatPeak && candidateState?.ID != BgmState.CombatLoop)
            {
                Demote();
            }
            else if (rankChangeData.NewRank >= Score.Model.StyleType.S && currentState?.ID == BgmState.CombatPeak && candidateState?.ID == BgmState.CombatLoop)
            {
                // Cancel demotion if player was being demoted but went back to S before the demotion transition started.
                var canCancel = currentState.CancelExit();
                if (!canCancel)
                {
                    return;
                }
                candidateState = null;
                stateTransitionStopwatch.Reset();
            }
        }

        public void LoadBgmStates(Dictionary<BgmState, IFsmState> states)
        {
            currentBgmStates = states;
        }

        public void Disable()
        {
            ResetToIntro();
        }
    }


}
