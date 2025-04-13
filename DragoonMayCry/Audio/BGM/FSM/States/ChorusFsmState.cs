using DragoonMayCry.Audio.Engine;
using NAudio.Wave;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DragoonMayCry.Audio.BGM.FSM.States
{
    internal abstract class ChorusFsmState(
        AudioService audioService,
        int nextStateTransitionTimeOnTransitionTriggered,
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
        
        public abstract BgmState ID { get; }
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
        protected readonly int CombatEndFadeOutTime = combatEndFadeOutTime;
        protected readonly int SamplesToKeepInCache = samplesToKeepInCache;
        protected readonly int NextStateTimeOnTransitionTriggered = nextStateTransitionTimeOnTransitionTriggered;
        
        private static readonly string DemotionStemId = "demotion";

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
        public abstract void Reset();
        public abstract int Exit(ExitType exit);
        public abstract bool CancelExit();

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
            var sample = AudioService.PlayBgm(DemotionStemId);
            if (sample != null)
            {
                Samples.Enqueue(sample);
            }
            CurrentState = PeakState.CleaningUpDemotion;
            NextStateTransitionTime = NextStateTimeOnTransitionTriggered;
            CurrentTrackStopwatch.Restart();
        }

        protected virtual void PlayNextPart()
        {
            if (CurrentState == PeakState.Loop)
            {
                if (CurrentTrack!.Next == null)
                {
                    CurrentTrack = CompleteSequence!.First!;
                }
                else
                {
                    CurrentTrack = CurrentTrack.Next;
                }
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
