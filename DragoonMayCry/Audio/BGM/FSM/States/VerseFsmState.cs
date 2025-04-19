#region

using NAudio.Wave;
using System;
using System.Collections.Generic;

#endregion

namespace DragoonMayCry.Audio.BGM.FSM.States
{
    internal abstract class VerseFsmState(
        AudioService audioService,
        int cachedSampleFadeOutDuration,
        CombatEndTransitionTimings combatEndTransitionTimings,
        int sampleCacheSize = 4)
        : BaseFsmState(audioService, cachedSampleFadeOutDuration)
    {
        protected LinkedList<string> CombatIntro = new();
        protected LinkedList<string> CombatLoop = new();
        protected CombatLoopState CurrentState;

        public override BgmState Id => BgmState.CombatLoop;
        public override void Enter(bool fromVerse)
        {
            CombatLoop = GenerateCombatLoop();
            CurrentTrackStopwatch.Reset();
            ISampleProvider? sample;
            if (fromVerse)
            {
                CurrentTrack = CombatLoop.First!;
                sample = AudioService.PlayBgm(CurrentTrack.Value);
                CurrentState = CombatLoopState.CoreLoop;
            }
            else
            {
                CurrentTrack = CombatIntro.First!;
                sample = AudioService.PlayBgm(CurrentTrack.Value);
                CurrentState = CombatLoopState.Intro;
            }
            if (sample != null)
            {
                Samples.Enqueue(sample);
            }
            TransitionTime = ComputeNextTransitionTiming();
            CurrentTrackStopwatch.Restart();
        }
        public override void Update()
        {
            switch (CurrentTrackStopwatch.IsRunning)
            {
                case false:
                    return;
                case true when CurrentTrackStopwatch.ElapsedMilliseconds >= TransitionTime:
                {
                    if (CurrentState != CombatLoopState.Exit)
                    {
                        PlayNextPart();
                    }
                    else
                    {
                        LeaveState();
                    }
                    break;
                }
            }

        }
        public override void Reset()
        {
            LeaveState();
        }
        public override int Exit(ExitType exit)
        {
            var selectedTransition = SelectChorusTransitionStem();
            var nextTransitionTime = Stems[selectedTransition].TransitionStart;

            if (CurrentState == CombatLoopState.Exit)
            {
                nextTransitionTime = (int)Math.Max(nextTransitionTime - CurrentTrackStopwatch.ElapsedMilliseconds, 0);
            }
            else
            {
                switch (exit)
                {
                    case ExitType.Promotion:
                        AudioService.PlayBgm(selectedTransition);
                        nextTransitionTime = Stems[selectedTransition].TransitionStart;
                        break;
                    case ExitType.EndOfCombat when CurrentState != CombatLoopState.Exit:
                        AudioService.PlayBgm(BgmStemIds.CombatEnd, combatEndTransitionTimings.FadingDuration,
                                             combatEndTransitionTimings.FadeOutDelay,
                                             combatEndTransitionTimings.FadeOutDuration);
                        nextTransitionTime = combatEndTransitionTimings.NextStateTransitionTime;
                        break;
                }
                CurrentTrackStopwatch.Restart();
            }
            CurrentState = CombatLoopState.Exit;
            TransitionTime = exit == ExitType.EndOfCombat ? combatEndTransitionTimings.TransitionTime
                                 : Stems[selectedTransition].EffectiveStart;

            return nextTransitionTime;
        }
        public override bool CancelExit()
        {
            return false;
        }

        protected int ComputeNextTransitionTiming()
        {
            return Stems[CurrentTrack!.Value].TransitionStart;
        }

        protected abstract LinkedList<string> GenerateCombatLoop();
        protected abstract LinkedList<string> GenerateCombatIntro();
        protected abstract string SelectChorusTransitionStem();

        protected virtual void PlayNextPart()
        {
            if (CurrentTrack!.Next == null)
            {
                CurrentTrack = CombatLoop.First!;
                CurrentState = CombatLoopState.CoreLoop;
            }
            else
            {
                CurrentTrack = CurrentTrack.Next;
            }

            PlayBgmPart();
            TransitionTime = ComputeNextTransitionTiming();
            CurrentTrackStopwatch.Restart();
        }

        private void PlayBgmPart()
        {
            if (Samples.Count > sampleCacheSize)
            {
                Samples.Dequeue();
            }

            var sample = AudioService.PlayBgm(CurrentTrack!.Value);
            if (sample != null)
            {
                Samples.Enqueue(sample);
            }
        }

        private void LeaveState()
        {
            StopCachedSamples();
            CurrentState = CombatLoopState.CoreLoop;
        }

        protected enum CombatLoopState
        {
            Intro,
            CoreLoop,
            Exit,
        }
    }
}
