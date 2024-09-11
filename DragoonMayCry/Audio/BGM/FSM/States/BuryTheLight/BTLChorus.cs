using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DragoonMayCry.Audio.BGM.FSM.States.BuryTheLight
{
    internal class BTLChorus : IFsmState
    {
        enum PeakState
        {
            VerseIntro,
            Loop,
            LeavingStateOutOfCombat,
            LeavingStateDemotion,
            CleaningUpDemotion
        }

        public BgmState ID { get { return BgmState.CombatPeak; } }
        private readonly Dictionary<BgmId, BgmTrackData> transitionTimePerId = new Dictionary<BgmId, BgmTrackData> {
            { BgmId.ChorusIntro1, new BgmTrackData(0, 25600) },
            { BgmId.ChorusIntro2, new BgmTrackData(0, 51200) },
            { BgmId.Riff, new BgmTrackData(0, 27200) },
            { BgmId.Chorus, new BgmTrackData(0, 27200) },
            { BgmId.ChorusTransition1, new BgmTrackData(0, 1600) },
            { BgmId.ChorusTransition2, new BgmTrackData(0, 1600) },
            { BgmId.ChorusTransition3, new BgmTrackData(0, 1600) },
            { BgmId.Demotion, new BgmTrackData(0, 25600) },
        };

        private readonly Dictionary<BgmId, int> possibleTransitionTimesToNewState = new Dictionary<BgmId, int> {
            { BgmId.ChorusIntro1, 27200  },
            { BgmId.ChorusIntro2, 52800 },
            { BgmId.Riff, 27200 },
            { BgmId.Chorus, 27200 },
            { BgmId.ChorusTransition1, 3220 },
            { BgmId.ChorusTransition2,  3220 },
            { BgmId.ChorusTransition3,  3220 },
        };

        private readonly Dictionary<BgmId, string> bgmPaths = new Dictionary<BgmId, string> {
            { BgmId.ChorusIntro1, DynamicBgmFsm.GetPathToAudio("CombatChorus\\025.ogg") },
            { BgmId.ChorusIntro2, DynamicBgmFsm.GetPathToAudio("CombatChorus\\094.ogg") },
            { BgmId.Riff, DynamicBgmFsm.GetPathToAudio("CombatChorus\\062.ogg") },
            { BgmId.Chorus, DynamicBgmFsm.GetPathToAudio("CombatChorus\\112.ogg") },
            { BgmId.Demotion, DynamicBgmFsm.GetPathToAudio("CombatChorus\\087.ogg") },
            { BgmId.ChorusTransition1, DynamicBgmFsm.GetPathToAudio("CombatChorus\\transition1.ogg") },
            { BgmId.ChorusTransition2, DynamicBgmFsm.GetPathToAudio("CombatChorus\\transition2.ogg") },
            { BgmId.ChorusTransition3, DynamicBgmFsm.GetPathToAudio("CombatChorus\\transition3.ogg") },
        };

        private readonly AudioService audioService;

        private readonly Stopwatch currentTrackStopwatch;
        private readonly Queue<ISampleProvider> samples;

        private LinkedList<BgmId>? completeSequence;
        private readonly LinkedList<BgmId> firstLoop;
        private readonly LinkedList<BgmId> secondLoop;
        private readonly LinkedList<BgmId> thirdLoop;

        private LinkedListNode<BgmId>? currentTrack;

        private PeakState currentState;
        private PeakState stateBeforeDemotion;
        // indicates when we can change tracks in this state
        private int transitionTime = 0;
        // indicates when it is appropriate to transition to a new FSM state
        private int nextPosibleStateTransitionTime = 0;
        //indicates when the next FSM state transition will take place
        private int nextStateTransitionTime;
        private int elapsedStopwatchTimeBeforeDemotion;

        public BTLChorus(AudioService audioService)
        {
            this.audioService = audioService;
            currentTrackStopwatch = new Stopwatch();
            samples = new Queue<ISampleProvider>();

            firstLoop = new LinkedList<BgmId>();
            firstLoop.AddLast(BgmId.Riff);
            firstLoop.AddLast(BgmId.Chorus);

            secondLoop = new LinkedList<BgmId>();
            secondLoop.AddLast(BgmId.ChorusIntro1);
            secondLoop.AddLast(BgmId.ChorusIntro2);
            secondLoop.AddLast(BgmId.Riff);
            secondLoop.AddLast(BgmId.Chorus);

            thirdLoop = new LinkedList<BgmId>();
            thirdLoop.AddLast(BgmId.ChorusIntro1);
            thirdLoop.AddLast(BgmId.ChorusIntro2);
        }

        public void Enter(bool fromVerse)
        {
            var chorus = SelectRandomChorus();
            currentTrack = new LinkedListNode<BgmId>(chorus);
            currentState = PeakState.VerseIntro;
            var sample = audioService.PlayBgm(chorus);
            if (sample != null)
            {
                samples.Enqueue(sample);
            }
            completeSequence = GenerateCompleteSequence();
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

        private LinkedList<BgmId> GenerateCompleteSequence()
        {
            var transitions = GenerateTransitionOrder();
            var sequence = new LinkedList<BgmId>();
            var node = firstLoop.First!;
            while (node != null)
            {
                sequence.AddLast(node.Value);
                node = node.Next;
            }
            node = secondLoop.First!;
            sequence.AddLast(transitions.Dequeue());
            while (node != null)
            {
                sequence.AddLast(node.Value);
                node = node.Next;
            }
            sequence.AddLast(transitions.Dequeue());
            node = thirdLoop.First!;
            while (node != null)
            {
                sequence.AddLast(node.Value);
                node = node.Next;
            }
            return sequence;

        }

        private Queue<BgmId> GenerateTransitionOrder()
        {
            var random = new Random();
            var val = random.Next(2);
            var res = new Queue<BgmId>();
            if (val < 1)
            {
                res.Enqueue(BgmId.ChorusTransition1);
                res.Enqueue(BgmId.ChorusTransition2);
            }
            else
            {
                res.Enqueue(BgmId.ChorusTransition2);
                res.Enqueue(BgmId.ChorusTransition1);
            }
            return res;
        }

        private int ComputeNextTransitionTiming()
        {
            return transitionTimePerId[currentTrack!.Value].TransitionStart;
        }

        private void LeaveStateOutOfCombat()
        {
            while (samples.Count > 0)
            {
                audioService.RemoveBgmPart(samples.Dequeue());
            }
            currentTrackStopwatch.Reset();

        }

        private void PlayNextPart()
        {
            if (currentState == PeakState.VerseIntro)
            {
                currentTrack = completeSequence!.First!;
                currentState = PeakState.Loop;
            }
            else if (currentState == PeakState.Loop)
            {
                if (currentTrack!.Next == null)
                {
                    // swap to transition at the end of each loop
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

        public int Exit(ExitType exit)
        {
            nextStateTransitionTime = 0;
            if (exit == ExitType.EndOfCombat)
            {
                transitionTime = 1600;
                nextPosibleStateTransitionTime = 8000;
                nextStateTransitionTime = 8000;
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
                stateBeforeDemotion = currentState;
                currentState = PeakState.LeavingStateDemotion;
            }
            currentTrackStopwatch.Restart();

            return nextStateTransitionTime;
        }

        private BgmId SelectRandomChorus()
        {
            var random = new Random();
            return random.Next(2) < 1 ? BgmId.ChorusIntro1 : BgmId.ChorusIntro2;
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
            currentState = PeakState.VerseIntro;
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
