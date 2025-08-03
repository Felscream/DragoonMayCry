using Dalamud.Plugin.Services;
using DragoonMayCry.Audio.BGM.FSM.States;
using DragoonMayCry.Score.Model;
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
        public delegate void LoadNewBgmDelegate();
        private readonly AudioService audioService;
        private readonly LinkedList<BgmState> bgmStates;


        private readonly IFramework framework;

        private readonly PlayerState playerState;
        private readonly Stopwatch stateTransitionStopwatch;
        private readonly StyleRankHandler styleRankHandler;
        private IFsmState? candidateState;


        private Dictionary<BgmState, IFsmState>? currentBgmStates;

        // The current state.
        private IFsmState? currentState;
        private LinkedListNode<BgmState> currentStateNode;
        public LoadNewBgmDelegate? LoadNewBgm;

        private int nextTransitionTime = -1;
        public DynamicBgmFsm(StyleRankHandler styleRankHandler)
        {
            this.styleRankHandler = styleRankHandler;
            this.styleRankHandler.StyleRankChange += OnRankChange;
            playerState = PlayerState.GetInstance();
            playerState.RegisterCombatStateChangeHandler(OnCombatChange);
            framework = Service.Framework;
            audioService = AudioService.Instance;
            stateTransitionStopwatch = new Stopwatch();

            bgmStates = new LinkedList<BgmState>(new[] { BgmState.Intro, BgmState.CombatLoop, BgmState.CombatPeak });

            currentStateNode = bgmStates.First!;

            framework.Update += Update;
        }
        public bool IsActive { get; private set; }

        public void Dispose()
        {
            audioService.StopBgm();
            framework.Update -= Update;
        }

        public void Start()
        {
            if (IsActive)
            {
                return;
            }

            currentStateNode = bgmStates.First!;
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

            if (playerState.IsInCombat && currentState?.Id == BgmState.Intro && candidateState == null)
            {
                audioService.FadeOutBgm(1600);
                currentState.Exit(ExitType.ImmediateExit);
                currentStateNode = bgmStates.Find(BgmState.CombatLoop)!;
                currentState = currentBgmStates![currentStateNode.Value];
                currentState?.Enter(false);

            }

            if (stateTransitionStopwatch.IsRunning
                && stateTransitionStopwatch.Elapsed.TotalMilliseconds > nextTransitionTime)
            {
                GoToNextState();
            }
            else if (playerState.IsInCombat && currentState?.Id == BgmState.Intro && candidateState == null)
            {
                Promote();
            }
        }

        public void ResetToIntro()
        {
            IsActive = false;
            stateTransitionStopwatch.Reset();
            currentStateNode = bgmStates.First!;
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

            if (candidateState.Id == BgmState.Intro)
            {
                currentStateNode = bgmStates.First!;
            }
            else if (currentStateNode.Next != null && currentStateNode.Next.Value == candidateState.Id)
            {
                currentStateNode = currentStateNode.Next!;
            }
            else
            {
                currentStateNode = bgmStates.Find(candidateState.Id)!;
            }
            var fromVerse = currentState?.Id == BgmState.CombatPeak && candidateState.Id == BgmState.CombatLoop;


            currentState = candidateState;
            if (candidateState.Id == BgmState.Intro)
            {
                LoadNewBgm?.Invoke();
            }
            currentState.Enter(fromVerse);
            candidateState = null;
        }

        public void Promote()
        {
            if (currentStateNode.Next == null || candidateState?.Id == currentStateNode.Value || currentState == null)
            {
                return;
            }

            candidateState = currentBgmStates?[currentStateNode.Next.Value];
            stateTransitionStopwatch.Restart();
            nextTransitionTime = currentState.Exit(ExitType.Promotion);
        }

        public void LeaveCombat()
        {
            if (currentState == null || candidateState?.Id == BgmState.Intro)
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
            if (currentState?.Id != BgmState.CombatPeak || candidateState != null)
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

            if (isInCombat && currentState?.Id == BgmState.Intro)
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
            if (rankChangeData.NewRank >= StyleType.S && currentState?.Id == BgmState.CombatLoop
                                                      && candidateState?.Id != BgmState.CombatPeak)
            {
                Promote();
            }
            else if (rankChangeData.NewRank < StyleType.S && currentState?.Id == BgmState.CombatPeak
                                                          && candidateState?.Id != BgmState.CombatLoop)
            {
                Demote();
            }
            else if (rankChangeData.NewRank >= StyleType.S && currentState?.Id == BgmState.CombatPeak
                                                           && candidateState?.Id == BgmState.CombatLoop)
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
