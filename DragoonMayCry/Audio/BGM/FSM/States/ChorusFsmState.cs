using DragoonMayCry.Audio.Engine;
using FFXIVClientStructs.Havok.Common.Base.System.IO.IStream;
using NAudio.Wave;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DragoonMayCry.Audio.BGM.FSM.States
{
    internal class ExitTimings(int baseTransitionTime, 
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
        public double FadeOutDuration { get; }= fadeOutDuration;
    }
    internal abstract class ChorusFsmState(
        AudioService audioService,
        int nextStateTransitionTimeOnDemotion,
        ExitTimings exitTimings,
        int combatEndFadeOutTime = 1300,
        int samplesToKeepInCache = 4)
        : IFsmState
    {
        protected enum PeakState
        {
            Loop,
            LeavingStateOutOfCombat,
            LeavingStateDemotion,
            CleaningUpDemotion,
            VerseIntro,
        }

        
        
        public BgmState ID => BgmState.CombatPeak;
        protected abstract Dictionary<string, BgmTrackData> Stems { get; }
        protected LinkedList<string> CompleteSequence =
            [];
        
        protected LinkedListNode<string>? CurrentTrack;
        protected PeakState CurrentState;
        protected readonly AudioService AudioService = audioService;
        protected readonly Stopwatch CurrentTrackStopwatch = new();
        protected readonly Queue<ISampleProvider> Samples = new();
        // indicates when we can change tracks in this state
        protected int TransitionTime = 0;
        // indicates when it is appropriate to transition to a new FSM state
        protected int NextPosibleStateTransitionTime = 0;
        //indicates when the next FSM state transition will take place
        protected int NextStateTransitionTime;
        protected int ElapsedStopwatchTimeBeforeDemotion;
        protected readonly ExitTimings ExitTimings = exitTimings;
        protected readonly int CombatEndFadeOutTime = combatEndFadeOutTime;
        protected readonly int SamplesToKeepInCache = samplesToKeepInCache;
        protected readonly int NextStateTimeOnDemotion = nextStateTransitionTimeOnDemotion;

        public virtual Dictionary<string, string> GetBgmPaths()
        {
            return Stems.ToDictionary(entry => entry.Key, entry => entry.Value.AudioPath);
        }
        
        public virtual void Enter(bool fromLoop)
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
        public virtual void Update()
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
        public virtual void Reset()
        {
            while (Samples.TryDequeue(out var sample))
            {
                if (sample is ExposedFadeInOutSampleProvider provider)
                {
                    if (provider.fadeState == ExposedFadeInOutSampleProvider.FadeState.FullVolume)
                    {
                        provider.BeginFadeOut(1500);
                        continue;
                    }
                }
                AudioService.RemoveBgmPart(sample);
            }
            CurrentTrackStopwatch.Reset();
            CurrentState = PeakState.Loop;
        }
        public virtual int Exit(ExitType exit)
        {
            NextStateTransitionTime = 0;
            if (exit == ExitType.EndOfCombat && CurrentState != PeakState.LeavingStateOutOfCombat)
            {
                TransitionTime = ExitTimings.BaseTransitionTime;
                NextPosibleStateTransitionTime = ExitTimings.NextPossibleStateTransitionTime;
                NextStateTransitionTime = ExitTimings.NextStateTransitionTime;
                AudioService.PlayBgm(BgmStemIds.CombatEnd, ExitTimings.FadingDuration, ExitTimings.FadeOutDelay, ExitTimings.FadeOutDuration);
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
        public virtual bool CancelExit()
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
    }
}
