#region

using DragoonMayCry.Audio.Engine;
using System.Collections.Generic;

#endregion

namespace DragoonMayCry.Audio.BGM.FSM.States
{
    internal class ExitTimings(
        int baseTransitionTime,
        int nextPossibleStateTransitionTime,
        int nextStateTransitionTime,
        int fadingDuration = 0,
        int fadeOutDelay = 0,
        double fadeOutDuration = 0)
    {
        public int BaseTransitionTime { get; } = baseTransitionTime;
        public int NextPossibleStateTransitionTime { get; } = nextPossibleStateTransitionTime;
        public int NextStateTransitionTime { get; } = nextStateTransitionTime;
        public int FadingDuration { get; } = fadingDuration;
        public int FadeOutDelay { get; } = fadeOutDelay;
        public double FadeOutDuration { get; } = fadeOutDuration;
    }

    internal abstract class ChorusFsmState(
        AudioService audioService,
        int nextStateTransitionTimeOnDemotion,
        ExitTimings exitTimings,
        int combatEndFadeOutTime = 1300,
        int samplesToKeepInCache = 4,
        int cachedSampleFadeOutDuration = 1500)
        : BaseFsmState(audioService, cachedSampleFadeOutDuration)
    {
        protected readonly int CombatEndFadeOutTime = combatEndFadeOutTime;
        protected readonly ExitTimings ExitTimings = exitTimings;
        protected readonly int NextStateTimeOnDemotion = nextStateTransitionTimeOnDemotion;
        protected readonly int SamplesToKeepInCache = samplesToKeepInCache;
        protected LinkedList<string> CompleteSequence =
            [];
        protected PeakState CurrentState;

        protected LinkedListNode<string>? CurrentTrack;

        protected int ElapsedStopwatchTimeBeforeDemotion;
        // indicates when it is appropriate to transition to a new FSM state
        protected int NextPosibleStateTransitionTime;


        public override BgmState Id => BgmState.CombatPeak;

        public override void Enter(bool fromLoop)
        {
            CompleteSequence = GenerateChorusLoop();
            CurrentTrack = CompleteSequence.First!;
            CurrentState = PeakState.Loop;
            var sample = AudioService.PlayBgm(CurrentTrack.Value);
            if (sample != null)
            {
                Samples.Enqueue(sample);
            }
            TransitionTime = ComputeNextTransitionTiming();
            CurrentTrackStopwatch.Restart();
            NextPosibleStateTransitionTime = ComputeNextStateTransitionTime();
        }
        public override void Update()
        {
            if (!CurrentTrackStopwatch.IsRunning)
            {
                return;
            }
            if (CurrentTrackStopwatch.ElapsedMilliseconds >= TransitionTime)
            {
                ElapsedStopwatchTimeBeforeDemotion = 0;
                switch (CurrentState)
                {
                    case PeakState.LeavingStateOutOfCombat:
                        LeaveStateOutOfCombat();
                        break;
                    case PeakState.LeavingStateDemotion:
                        StartDemotionTransition();
                        break;
                    case PeakState.CleaningUpDemotion:
                        while (Samples.Count > 1)
                        {
                            AudioService.RemoveBgmPart(Samples.Dequeue());
                        }
                        CurrentTrackStopwatch.Reset();
                        break;
                    default:
                        PlayNextPart();
                        break;
                }
            }
        }
        public override void Reset()
        {
            base.StopCachedSamples();
            CurrentState = PeakState.Loop;
        }
        public override int Exit(ExitType exit)
        {
            NextStateTransitionTime = 0;
            if (exit == ExitType.EndOfCombat && CurrentState != PeakState.LeavingStateOutOfCombat)
            {
                TransitionTime = ExitTimings.BaseTransitionTime;
                NextPosibleStateTransitionTime = ExitTimings.NextPossibleStateTransitionTime;
                NextStateTransitionTime = ExitTimings.NextStateTransitionTime;
                AudioService.PlayBgm(BgmStemIds.CombatEnd, ExitTimings.FadingDuration, ExitTimings.FadeOutDelay,
                                     ExitTimings.FadeOutDuration);
                CurrentState = PeakState.LeavingStateOutOfCombat;
            }
            else if (CurrentState == PeakState.LeavingStateDemotion)
            {
                return NextStateTransitionTime - (int)CurrentTrackStopwatch.Elapsed.TotalMilliseconds;
            }
            else if (exit == ExitType.Demotion)
            {
                ElapsedStopwatchTimeBeforeDemotion += (int)CurrentTrackStopwatch.Elapsed.TotalMilliseconds;
                TransitionTime = NextPosibleStateTransitionTime - ElapsedStopwatchTimeBeforeDemotion;
                NextStateTransitionTime = TransitionTime + Stems[BgmStemIds.Demotion].TransitionStart;
                CurrentState = PeakState.LeavingStateDemotion;
            }
            CurrentTrackStopwatch.Restart();

            return NextStateTransitionTime;
        }
        public override bool CancelExit()
        {
            if (CurrentState != PeakState.LeavingStateDemotion)
            {
                return false;
            }
            CurrentState = PeakState.Loop;
            TransitionTime = ComputeNextTransitionTiming() - ElapsedStopwatchTimeBeforeDemotion;
            NextStateTransitionTime = ComputeNextStateTransitionTime() - ElapsedStopwatchTimeBeforeDemotion;
            return true;
        }

        protected abstract LinkedList<string> GenerateChorusLoop();
        protected virtual int ComputeNextStateTransitionTime()
        {
            //var time = possibleTransitionTimesToNewState[currentTrack!.Value];
            return Stems[CurrentTrack!.Value].PossibleTransitionTimeToNewState;
        }

        protected virtual int ComputeNextTransitionTiming()
        {
            return Stems[CurrentTrack!.Value].TransitionStart;
        }

        protected virtual void LeaveStateOutOfCombat()
        {
            while (Samples.Count > 1)
            {
                AudioService.RemoveBgmPart(Samples.Dequeue());
            }
            if (Samples.TryDequeue(out var sample))
            {
                if (sample is ExposedFadeInOutSampleProvider provider)
                {
                    provider.BeginFadeOut(CombatEndFadeOutTime);
                }
            }
            CurrentTrackStopwatch.Reset();
        }

        protected virtual void StartDemotionTransition()
        {
            CurrentState = PeakState.CleaningUpDemotion;
            while (Samples.Count > 1)
            {
                AudioService.RemoveBgmPart(Samples.Dequeue());
            }
            var sample = AudioService.PlayBgm(BgmStemIds.Demotion);
            if (sample != null)
            {
                Samples.Enqueue(sample);
            }
            CurrentState = PeakState.CleaningUpDemotion;
            NextStateTransitionTime = NextStateTimeOnDemotion;
            CurrentTrackStopwatch.Restart();
        }

        protected virtual void PlayNextPart()
        {
            if (CurrentState == PeakState.Loop)
            {
                CurrentTrack = CurrentTrack!.Next == null ? CompleteSequence!.First!
                                   : CurrentTrack.Next;
            }

            if (Samples.Count > SamplesToKeepInCache)
            {
                Samples.Dequeue();
            }

            var sample = AudioService.PlayBgm(CurrentTrack!.Value);
            if (sample != null)
            {
                Samples.Enqueue(sample);
            }
            TransitionTime = ComputeNextTransitionTiming();
            CurrentTrackStopwatch.Restart();
            NextPosibleStateTransitionTime = ComputeNextStateTransitionTime();
        }

        protected enum PeakState
        {
            Loop,
            LeavingStateOutOfCombat,
            LeavingStateDemotion,
            CleaningUpDemotion,
            VerseIntro,
        }
    }
}
