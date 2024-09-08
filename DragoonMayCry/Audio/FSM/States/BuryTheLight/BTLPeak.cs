using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DragoonMayCry.Audio.FSM.States.BuryTheLight
{
    internal class BTLPeak : FsmState
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
            { BgmId.Riff1, new BgmTrackData(0, 27200) },
            { BgmId.Chorus1, new BgmTrackData(0, 27200) },
            { BgmId.ChorusTransition1, new BgmTrackData(0, 1600) },
            { BgmId.ChorusTransition2, new BgmTrackData(0, 1600) },
            { BgmId.Demotion, new BgmTrackData(0, 25600) },
        };

        private readonly Dictionary<BgmId, int> possibleTransitionTimesToNewState = new Dictionary<BgmId, int> {
            { BgmId.ChorusIntro1, 27200  },
            { BgmId.ChorusIntro2, 52800 },
            { BgmId.Riff1, 27200 },
            { BgmId.Chorus1, 27200 },
            { BgmId.ChorusTransition1, 3220 },
            { BgmId.ChorusTransition2,  3220 },
        };

        private readonly Dictionary<BgmId, string> bgmPaths = new Dictionary<BgmId, string> {
            { BgmId.ChorusIntro1, BuryTheLightFsm.GetPathToAudio("CombatChorus\\025.mp3") },
            { BgmId.ChorusIntro2, BuryTheLightFsm.GetPathToAudio("CombatChorus\\094.mp3") },
            { BgmId.Riff1, BuryTheLightFsm.GetPathToAudio("CombatChorus\\062.mp3") },
            { BgmId.Chorus1, BuryTheLightFsm.GetPathToAudio("CombatChorus\\112.mp3") },
            { BgmId.ChorusTransition1, BuryTheLightFsm.GetPathToAudio("CombatChorus\\transition1.mp3") },
            { BgmId.ChorusTransition2, BuryTheLightFsm.GetPathToAudio("CombatChorus\\transition2.mp3") },
            { BgmId.Demotion, BuryTheLightFsm.GetPathToAudio("CombatChorus\\demotion.mp3") },
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

        public BTLPeak(AudioService audioService)
        {
            this.audioService = audioService;
            currentTrackStopwatch = new Stopwatch();
            samples = new Queue<ISampleProvider>();

            firstLoop = new LinkedList<BgmId>();
            firstLoop.AddLast(BgmId.Riff1);
            firstLoop.AddLast(BgmId.Chorus1);

            secondLoop = new LinkedList<BgmId>();
            secondLoop.AddLast(BgmId.ChorusIntro1);
            secondLoop.AddLast(BgmId.ChorusIntro2);
            secondLoop.AddLast(BgmId.Riff1);
            secondLoop.AddLast(BgmId.Chorus1);

            thirdLoop = new LinkedList<BgmId>();
            thirdLoop.AddLast(BgmId.ChorusIntro1);
            thirdLoop.AddLast(BgmId.ChorusIntro2);
        }

        public void Enter(bool fromVerse)
        {
            BgmId chorus = SelectRandomChorus();
            currentTrack = new LinkedListNode<BgmId>(chorus);
            currentState = PeakState.VerseIntro;
            var sample = audioService.PlayBgm(chorus);
            if(sample != null)
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
            if(currentState == PeakState.LeavingStateDemotion || currentState == PeakState.CleaningUpDemotion)
            {
                Service.Log.Debug($"Demotion in progress {currentTrackStopwatch.ElapsedMilliseconds} vs {transitionTime} sample count {samples.Count}");
            }
            if (currentTrackStopwatch.ElapsedMilliseconds >= transitionTime)
            {
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
                        Reset();
                        break;
                    default:
                        PlayNextPart();
                        break;
                }
            }

            /*if (currentTrackStopwatch.ElapsedMilliseconds > nextPosibleStateTransitionTime && currentState != PeakState.LeavingStateDemotion)
            {
                nextPosibleStateTransitionTime = ComputeNextStateTransitionTime();
            }*/
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
            LinkedList<BgmId> sequence = new LinkedList<BgmId>();
            var node = firstLoop.First!;
            while(node != null)
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
            int val = random.Next(2);
            var res = new Queue<BgmId>();
            if(val < 1)
            {
                res.Enqueue(BgmId.ChorusTransition1);
                res.Enqueue(BgmId.ChorusTransition2);
            } else
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
            while(samples.Count > 0)
            {
                audioService.RemoveBgmPart(samples.Dequeue());
            }
            Reset();
        }

        private void PlayNextPart()
        {
            if(currentState == PeakState.VerseIntro)
            {
                currentTrack = completeSequence!.First!;
                currentState = PeakState.Loop;
            } 
            else if(currentState == PeakState.Loop)
            {
                if(currentTrack!.Next == null)
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
            Service.Log.Debug($"Playing {currentTrack.Value} current state {currentState}");
            Service.Log.Debug($"Next transition time at {transitionTime}");
            nextPosibleStateTransitionTime = ComputeNextStateTransitionTime();
        }

        private int ComputeNextStateTransitionTime()
        {
            var time = possibleTransitionTimesToNewState[currentTrack!.Value];
            Service.Log.Debug($"Next state possible transition at {time}");
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
                transitionTime = nextPosibleStateTransitionTime - (int)currentTrackStopwatch.Elapsed.TotalMilliseconds;
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
            currentTrackStopwatch.Reset();
        }

        public void CancelExit()
        {
            if(currentState != PeakState.LeavingStateDemotion)
            {
                return;
            }

            currentState = stateBeforeDemotion;
            transitionTime = transitionTimePerId[currentTrack!.Value].TransitionStart;

        }
    }
}
