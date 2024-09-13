using NAudio.Codecs;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DragoonMayCry.Audio.BGM.FSM.States.DevilTrigger
{
    internal class CCChorus : IFsmState
    {
        enum PeakState
        {
            Loop,
            LeavingStateOutOfCombat,
            LeavingStateDemotion,
            CleaningUpDemotion
        }

        public BgmState ID { get { return BgmState.CombatPeak; } }
        private readonly Dictionary<BgmId, BgmTrackData> transitionTimePerId = new Dictionary<BgmId, BgmTrackData> {
            { BgmId.ChorusIntro1, new BgmTrackData(0, 20605) },
            { BgmId.ChorusIntro2, new BgmTrackData(0, 10300) },
            { BgmId.Riff, new BgmTrackData(0, 19350) },
            { BgmId.ChorusTransition1, new BgmTrackData(0, 20650) },
            { BgmId.ChorusTransition2, new BgmTrackData(0, 11600) },
            { BgmId.ChorusTransition3, new BgmTrackData(0, 2600) },
            { BgmId.Demotion, new BgmTrackData(0, 10300) },
        };

        private readonly Dictionary<BgmId, int> possibleTransitionTimesToNewState = new Dictionary<BgmId, int> {
            { BgmId.ChorusIntro1, 20605 },
            { BgmId.ChorusIntro2, 10300 },
            { BgmId.Riff, 20600 },
            { BgmId.ChorusTransition1, 21900 },
            { BgmId.ChorusTransition2,  11600 },
            { BgmId.ChorusTransition3,  2600 },
        };

        private readonly Dictionary<BgmId, string> bgmPaths = new Dictionary<BgmId, string> {
            { BgmId.ChorusIntro1, DynamicBgmService.GetPathToAudio("CrimsonCloud\\Chorus\\086.ogg") },
            { BgmId.ChorusIntro2, DynamicBgmService.GetPathToAudio("CrimsonCloud\\Chorus\\080.ogg") },
            { BgmId.Riff, DynamicBgmService.GetPathToAudio("CrimsonCloud\\Chorus\\103.ogg") },
            { BgmId.ChorusTransition1, DynamicBgmService.GetPathToAudio("CrimsonCloud\\Chorus\\059.ogg") },
            { BgmId.ChorusTransition2, DynamicBgmService.GetPathToAudio("CrimsonCloud\\Chorus\\031.ogg") },
            { BgmId.ChorusTransition3, DynamicBgmService.GetPathToAudio("CrimsonCloud\\Chorus\\082.ogg") },
            { BgmId.Demotion, DynamicBgmService.GetPathToAudio("CrimsonCloud\\Chorus\\016.ogg") },
        };

        private readonly AudioService audioService;

        private readonly Stopwatch currentTrackStopwatch;
        private readonly Queue<ISampleProvider> samples;
        private readonly Random random = new Random();
        private readonly LinkedList<BgmId> completeSequence;
        private LinkedListNode<BgmId>? currentTrack;

        private PeakState currentState;
        // indicates when we can change tracks in this state
        private int transitionTime = 0;
        // indicates when it is appropriate to transition to a new FSM state
        private int nextPosibleStateTransitionTime = 0;
        //indicates when the next FSM state transition will take place
        private int nextStateTransitionTime;
        private int elapsedStopwatchTimeBeforeDemotion;

        public CCChorus(AudioService audioService)
        {
            this.audioService = audioService;
            currentTrackStopwatch = new Stopwatch();
            samples = new Queue<ISampleProvider>();
            completeSequence = GenerateVerseLoop();
        }

        public void Enter(bool fromVerse)
        {
            currentTrack = completeSequence.First!;
            currentState = PeakState.Loop;
            var sample = audioService.PlayBgm(currentTrack.Value, 1);
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
            var sample = audioService.PlayBgm(BgmId.Demotion, 1);
            if (sample != null)
            {
                samples.Enqueue(sample);
            }
            currentState = PeakState.CleaningUpDemotion;
            nextStateTransitionTime = 1590;
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
            if(samples.TryDequeue(out var sample))
            {
                if(sample is FadeInOutSampleProvider)
                {
                    ((FadeInOutSampleProvider)sample).BeginFadeOut(500);
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

            if (samples.Count > 4)
            {
                samples.Dequeue();
            }

            var sample = audioService.PlayBgm(currentTrack!.Value, 1);
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
            var time = possibleTransitionTimesToNewState[currentTrack!.Value];
            return time;
        }

        private LinkedList<BgmId> GenerateVerseLoop()
        {
            var loop = new LinkedList<BgmId>();
            loop.AddLast(BgmId.ChorusIntro1);
            loop.AddLast(BgmId.ChorusIntro2);
            loop.AddLast(BgmId.Riff);
            loop.AddLast(BgmId.ChorusTransition1);
            loop.AddLast(BgmId.ChorusTransition2);
            loop.AddLast(BgmId.ChorusTransition3);
            return loop;
        }

        public int Exit(ExitType exit)
        {
            nextStateTransitionTime = 0;
            if (exit == ExitType.EndOfCombat)
            {
                transitionTime = 1;
                nextPosibleStateTransitionTime = 5200;
                nextStateTransitionTime = 5200;
                audioService.PlayBgm(BgmId.CombatEnd, 1);
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
            while (samples.Count > 0)
            {
                audioService.RemoveBgmPart(samples.Dequeue());
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
