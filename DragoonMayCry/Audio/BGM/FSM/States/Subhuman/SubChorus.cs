using DragoonMayCry.Audio.Engine;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DragoonMayCry.Audio.BGM.FSM.States.DevilTrigger
{
    internal class SubChorus : IFsmState
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
            { BgmId.ChorusIntro1, new BgmTrackData(0, 20650) },
            { BgmId.ChorusIntro2, new BgmTrackData(0, 20650) },
            { BgmId.Riff, new BgmTrackData(0, 23250) },
            { BgmId.Chorus2, new BgmTrackData(0, 23250) },
            { BgmId.Chorus3, new BgmTrackData(0, 23250) },
            { BgmId.Chorus, new BgmTrackData(0, 20650) },
            { BgmId.ChorusTransition1, new BgmTrackData(0, 2650) },
            { BgmId.ChorusTransition2, new BgmTrackData(0, 2550) },
            { BgmId.ChorusTransition3, new BgmTrackData(0, 2600) },
            { BgmId.Demotion, new BgmTrackData(0, 2600) },
        };

        private readonly Dictionary<BgmId, int> possibleTransitionTimesToNewState = new()
        {
            { BgmId.ChorusIntro1, 20650 },
            { BgmId.ChorusIntro2, 20650 },
            { BgmId.Riff, 23200 },
            { BgmId.Chorus2, 23250 },
            { BgmId.Chorus3, 23250 },
            { BgmId.Chorus, 20650 },
            { BgmId.ChorusTransition1, 2650 },
            { BgmId.ChorusTransition2,  2550 },
            { BgmId.ChorusTransition3,  2600 },
        };

        private readonly Dictionary<BgmId, string> bgmPaths = new()
        {
            { BgmId.ChorusIntro1, DynamicBgmService.GetPathToAudio("Subhuman\\Chorus\\009.ogg") },
            { BgmId.ChorusIntro2, DynamicBgmService.GetPathToAudio("Subhuman\\Chorus\\050.ogg") },
            { BgmId.Riff, DynamicBgmService.GetPathToAudio("Subhuman\\Chorus\\090.ogg") },
            { BgmId.Chorus2, DynamicBgmService.GetPathToAudio("Subhuman\\Chorus\\104.ogg") },
            { BgmId.Chorus3, DynamicBgmService.GetPathToAudio("Subhuman\\Chorus\\051.ogg") },
            { BgmId.Demotion, DynamicBgmService.GetPathToAudio("Subhuman\\Chorus\\013.ogg") },
            { BgmId.ChorusTransition1, DynamicBgmService.GetPathToAudio("Subhuman\\Verse\\053.ogg") },
            { BgmId.ChorusTransition2, DynamicBgmService.GetPathToAudio("Subhuman\\Verse\\065.ogg") },
            { BgmId.ChorusTransition3, DynamicBgmService.GetPathToAudio("Subhuman\\Verse\\001.ogg") },
            { BgmId.Chorus, DynamicBgmService.GetPathToAudio("Subhuman\\Chorus\\105.ogg") },
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

        public SubChorus(AudioService audioService)
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
            var chorus = RandomizeQueue(BgmId.Chorus2, BgmId.Chorus3);
            var loop = new LinkedList<BgmId>();
            loop.AddLast(chorusIntro.Dequeue());
            loop.AddLast(BgmId.Riff);
            loop.AddLast(chorus.Dequeue());
            loop.AddLast(BgmId.Chorus);
            loop.AddLast(SelectRandom(BgmId.ChorusTransition1, BgmId.ChorusTransition2, BgmId.ChorusTransition3));
            loop.AddLast(BgmId.Riff);
            loop.AddLast(chorus.Dequeue());
            loop.AddLast(BgmId.Chorus);
            loop.AddLast(SelectRandom(BgmId.ChorusTransition1, BgmId.ChorusTransition2, BgmId.ChorusTransition3));
            return loop;
        }

        private BgmId SelectRandom(params BgmId[] bgmIds)
        {
            var index = random.Next(bgmIds.Length);
            return bgmIds[index];
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
