using DragoonMayCry.Audio.Engine;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DragoonMayCry.Audio.BGM.FSM.States.DevilTrigger
{
    internal class DTChorus : IFsmState
    {
        enum PeakState
        {
            Loop,
            LeavingStateOutOfCombat,
            LeavingStateDemotion,
            CleaningUpDemotion
        }

        public BgmState ID { get { return BgmState.CombatPeak; } }
        private readonly Dictionary<BgmId, BgmTrackData> transitionTimePerId = new()
        {
            { BgmId.ChorusIntro1, new BgmTrackData(0, 12000) },
            { BgmId.ChorusIntro2, new BgmTrackData(0, 12000) },
            { BgmId.Riff, new BgmTrackData(0, 24000) },
            { BgmId.ChorusTransition1, new BgmTrackData(0, 24000) },
            { BgmId.ChorusTransition2, new BgmTrackData(0, 24000) },
            { BgmId.ChorusTransition3, new BgmTrackData(0, 24000) },
            { BgmId.Demotion, new BgmTrackData(0, 1500) },
        };

        private readonly Dictionary<BgmId, int> possibleTransitionTimesToNewState = new()
        {
            { BgmId.ChorusIntro1, 13500 },
            { BgmId.ChorusIntro2, 13500 },
            { BgmId.Riff, 25500 },
            { BgmId.ChorusTransition1, 25500 },
            { BgmId.ChorusTransition2,  25500 },
            { BgmId.ChorusTransition3,  25500 },
        };

        private readonly Dictionary<BgmId, string> bgmPaths = new()
        {
            { BgmId.ChorusIntro1, DynamicBgmService.GetPathToAudio("DevilTrigger\\Chorus\\058.ogg") },
            { BgmId.ChorusIntro2, DynamicBgmService.GetPathToAudio("DevilTrigger\\Chorus\\061.ogg") },
            { BgmId.Riff, DynamicBgmService.GetPathToAudio("DevilTrigger\\Chorus\\003.ogg") },
            { BgmId.Demotion, DynamicBgmService.GetPathToAudio("DevilTrigger\\Chorus\\030.ogg") },
            { BgmId.ChorusTransition1, DynamicBgmService.GetPathToAudio("DevilTrigger\\Chorus\\098.ogg") },
            { BgmId.ChorusTransition2, DynamicBgmService.GetPathToAudio("DevilTrigger\\Chorus\\072.ogg") },
            { BgmId.ChorusTransition3, DynamicBgmService.GetPathToAudio("DevilTrigger\\Chorus\\041.ogg") },
        };

        private readonly AudioService audioService;

        private readonly Stopwatch currentTrackStopwatch;
        private readonly Queue<ISampleProvider> samples;
        private readonly Random random = new();
        private LinkedList<BgmId>? completeSequence;
        private LinkedListNode<BgmId>? currentTrack;

        private PeakState currentState;
        // indicates when we can change tracks in this state
        private int transitionTime = 0;
        // indicates when it is appropriate to transition to a new FSM state
        private int nextPosibleStateTransitionTime = 0;
        //indicates when the next FSM state transition will take place
        private int nextStateTransitionTime;
        private int elapsedStopwatchTimeBeforeDemotion;

        public DTChorus(AudioService audioService)
        {
            this.audioService = audioService;
            currentTrackStopwatch = new Stopwatch();
            samples = new Queue<ISampleProvider>();
        }

        public void Enter(bool fromVerse)
        {
            completeSequence = GenerateChorusLoop();
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
            if (samples.TryDequeue(out var sample))
            {
                if (sample is ExposedFadeInOutSampleProvider)
                {
                    ((ExposedFadeInOutSampleProvider)sample).BeginFadeOut(500);
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
            var time = possibleTransitionTimesToNewState[currentTrack!.Value];
            return time;
        }

        private LinkedList<BgmId> GenerateChorusLoop()
        {
            var chorusIntro = RandomizeQueue(BgmId.ChorusIntro1, BgmId.ChorusIntro2);
            var chorus = RandomizeQueue(BgmId.ChorusTransition1, BgmId.ChorusTransition2);
            var loop = new LinkedList<BgmId>();
            loop.AddLast(chorusIntro.Dequeue());
            loop.AddLast(BgmId.Riff);
            loop.AddLast(chorus.Dequeue());
            loop.AddLast(BgmId.ChorusTransition3);
            loop.AddLast(chorusIntro.Dequeue());
            loop.AddLast(BgmId.Riff);
            loop.AddLast(chorus.Dequeue());
            loop.AddLast(BgmId.ChorusTransition3);
            return loop;
        }

        private Queue<BgmId> RandomizeQueue(params BgmId[] bgmIds)
        {
            var k = random.Next(2);
            var queue = new Queue<BgmId>();
            if (k < 1)
            {
                for (var i = 0; i < bgmIds.Length; i++)
                {
                    queue.Enqueue(bgmIds[i]);
                }
            }
            else
            {
                for (var i = bgmIds.Length - 1; i >= 0; i--)
                {
                    queue.Enqueue(bgmIds[i]);
                }
            }
            return queue;
        }

        public int Exit(ExitType exit)
        {
            nextStateTransitionTime = 0;
            if (exit == ExitType.EndOfCombat && currentState != PeakState.LeavingStateOutOfCombat)
            {
                transitionTime = 1;
                nextPosibleStateTransitionTime = 4500;
                nextStateTransitionTime = 4500;
                audioService.PlayBgm(BgmId.CombatEnd);
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
                        provider.BeginFadeOut(1500);
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
