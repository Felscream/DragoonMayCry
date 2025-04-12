using DragoonMayCry.Audio.Engine;
using NAudio.Wave;
using System.Collections.Generic;
using System.Diagnostics;

namespace DragoonMayCry.Audio.BGM.FSM.States.DevilsNeverCry
{
    internal class DncChorus : IFsmState
    {
        enum PeakState
        {
            Loop,
            LeavingStateOutOfCombat,
            LeavingStateDemotion,
            CleaningUpDemotion
        }

        public BgmState ID => BgmState.CombatPeak;
        private readonly Dictionary<BgmId, BgmTrackData> transitionTimePerId = new()
        {
            { BgmId.ChorusIntro1, new BgmTrackData(0, 23700) },
            { BgmId.Chorus, new BgmTrackData(0, 13500) },
            { BgmId.Chorus2, new BgmTrackData(0, 25630) },
            { BgmId.Chorus3, new BgmTrackData(0, 26800) },
            { BgmId.ChorusTransition1, new BgmTrackData(0, 3750) },
            { BgmId.Demotion, new BgmTrackData(0, 4510) },
        };

        private readonly Dictionary<BgmId, int> possibleTransitionTimesToNewState = new()
        {
            { BgmId.ChorusIntro1, 24500 },
            { BgmId.Chorus, 13800 },
            { BgmId.Chorus2, 25800 },
            { BgmId.Chorus3, 26200 },
            { BgmId.ChorusTransition1, 3000 },
        };

        private readonly Dictionary<BgmId, string> bgmPaths = new()
        {
            { BgmId.ChorusIntro1, DynamicBgmService.GetPathToAudio("DevilsNeverCry\\Chorus\\04.ogg") },
            { BgmId.Chorus, DynamicBgmService.GetPathToAudio("DevilsNeverCry\\Chorus\\22.ogg") },
            { BgmId.Chorus2, DynamicBgmService.GetPathToAudio("DevilsNeverCry\\Chorus\\16.ogg") },
            { BgmId.Chorus3, DynamicBgmService.GetPathToAudio("DevilsNeverCry\\Chorus\\26.ogg") },
            { BgmId.Demotion, DynamicBgmService.GetPathToAudio("DevilsNeverCry\\Chorus\\05.ogg") },
            { BgmId.ChorusTransition1, DynamicBgmService.GetPathToAudio("DevilsNeverCry\\Chorus\\01.ogg") },
        };

        private readonly AudioService audioService;

        private readonly Stopwatch currentTrackStopwatch;
        private readonly Queue<ISampleProvider> samples;
        private readonly LinkedList<BgmId> completeSequence = new();
        private LinkedListNode<BgmId>? currentTrack;

        private PeakState currentState;
        // indicates when we can change tracks in this state
        private int transitionTime = 0;
        // indicates when it is appropriate to transition to a new FSM state
        private int nextPosibleStateTransitionTime = 0;
        //indicates when the next FSM state transition will take place
        private int nextStateTransitionTime;
        private int elapsedStopwatchTimeBeforeDemotion;

        public DncChorus(AudioService audioService)
        {
            this.audioService = audioService;
            currentTrackStopwatch = new Stopwatch();
            samples = new Queue<ISampleProvider>();
            
            completeSequence.AddLast(BgmId.ChorusIntro1);
            completeSequence.AddLast(BgmId.Chorus);
            completeSequence.AddLast(BgmId.Chorus2);
            completeSequence.AddLast(BgmId.Chorus3);
            completeSequence.AddLast(BgmId.ChorusTransition1);
        }

        public void Enter(bool fromVerse)
        {
            currentTrack = completeSequence.First!;
            currentState = PeakState.Loop;
            var sample = audioService.PlayBgm(currentTrack.Value);
            if (sample != null)
            {
                samples.Enqueue(sample);
            }
            transitionTime = ComputeNextTransitionTiming();
            currentTrackStopwatch.Restart();
            nextPosibleStateTransitionTime = ComputeNextStateTransitionTime();

        }

        public void Update()
        {
            if (!currentTrackStopwatch.IsRunning)
            {
                return;
            }

            if (currentTrackStopwatch.ElapsedMilliseconds >= transitionTime)
            {
                elapsedStopwatchTimeBeforeDemotion = 0;
                switch (currentState)
                {
                    case PeakState.LeavingStateOutOfCombat:
                        LeaveStateOutOfCombat();
                        break;
                    case PeakState.LeavingStateDemotion:
                        StartDemotionTransition();
                        break;
                    case PeakState.CleaningUpDemotion:
                        while (samples.Count > 1)
                        {
                            audioService.RemoveBgmPart(samples.Dequeue());
                        }
                        currentTrackStopwatch.Reset();
                        break;
                    default:
                        PlayNextPart();
                        break;
                }
            }
        }

        private void StartDemotionTransition()
        {
            currentState = PeakState.CleaningUpDemotion;
            while (samples.Count > 1)
            {
                audioService.RemoveBgmPart(samples.Dequeue());
            }
            var sample = audioService.PlayBgm(BgmId.Demotion);
            if (sample != null)
            {
                samples.Enqueue(sample);
            }
            currentState = PeakState.CleaningUpDemotion;
            nextStateTransitionTime = 1290;
            currentTrackStopwatch.Restart();
        }

        private int ComputeNextTransitionTiming()
        {
            return transitionTimePerId[currentTrack!.Value].TransitionStart;
        }

        private void LeaveStateOutOfCombat()
        {
            while (samples.Count > 1)
            {
                audioService.RemoveBgmPart(samples.Dequeue());
            }
            if (samples.TryDequeue(out var sample))
            {
                if (sample is ExposedFadeInOutSampleProvider)
                {
                    ((ExposedFadeInOutSampleProvider)sample).BeginFadeOut(1300);
                }
            }

            currentTrackStopwatch.Reset();

        }

        private void PlayNextPart()
        {
            if (currentState == PeakState.Loop)
            {
                if (currentTrack!.Next == null)
                {
                    currentTrack = completeSequence!.First!;
                }
                else
                {
                    currentTrack = currentTrack.Next;
                }
            }

            if (samples.Count > 3)
            {
                samples.Dequeue();
            }

            var sample = audioService.PlayBgm(currentTrack!.Value);
            if (sample != null)
            {
                samples.Enqueue(sample);
            }
            transitionTime = ComputeNextTransitionTiming();
            currentTrackStopwatch.Restart();
            nextPosibleStateTransitionTime = ComputeNextStateTransitionTime();
        }

        private int ComputeNextStateTransitionTime()
        {
            return possibleTransitionTimesToNewState[currentTrack!.Value];
        }

        public int Exit(ExitType exit)
        {
            nextStateTransitionTime = 0;
            if (exit == ExitType.EndOfCombat && currentState != PeakState.LeavingStateOutOfCombat)
            {
                transitionTime = 1300;
                nextPosibleStateTransitionTime = 6000;
                nextStateTransitionTime = 6000;
                audioService.PlayBgm(BgmId.CombatEnd, 0, 9000, 6000);
                currentState = PeakState.LeavingStateOutOfCombat;
            }
            else if (currentState == PeakState.LeavingStateDemotion)
            {
                return nextStateTransitionTime - (int)currentTrackStopwatch.Elapsed.TotalMilliseconds;
            }
            else if (exit == ExitType.Demotion)
            {
                elapsedStopwatchTimeBeforeDemotion += (int)currentTrackStopwatch.Elapsed.TotalMilliseconds;
                transitionTime = nextPosibleStateTransitionTime - elapsedStopwatchTimeBeforeDemotion;
                nextStateTransitionTime = transitionTime + transitionTimePerId[BgmId.Demotion].TransitionStart;
                currentState = PeakState.LeavingStateDemotion;
            }
            currentTrackStopwatch.Restart();

            return nextStateTransitionTime;
        }

        public Dictionary<BgmId, string> GetBgmPaths()
        {
            return bgmPaths;
        }

        public void Reset()
        {
            while (samples.TryDequeue(out var sample))
            {
                if (sample is ExposedFadeInOutSampleProvider provider)
                {
                    if (provider.fadeState == ExposedFadeInOutSampleProvider.FadeState.FullVolume)
                    {
                        provider.BeginFadeOut(1700);
                        continue;
                    }
                }
                audioService.RemoveBgmPart(sample);
            }
            currentTrackStopwatch.Reset();
            currentState = PeakState.Loop;
        }

        public bool CancelExit()
        {
            if (currentState != PeakState.LeavingStateDemotion)
            {
                return false;
            }
            currentState = PeakState.Loop;
            transitionTime = ComputeNextTransitionTiming() - elapsedStopwatchTimeBeforeDemotion;
            nextStateTransitionTime = ComputeNextStateTransitionTime() - elapsedStopwatchTimeBeforeDemotion;
            return true;
        }
    }
}
