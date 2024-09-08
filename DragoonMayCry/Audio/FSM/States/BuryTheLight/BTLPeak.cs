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
            Transition,
            LeavingState
        }

        public BgmState ID { get { return BgmState.CombatPeak; } }
        private readonly Dictionary<BgmId, BgmTrackData> transitionTimePerId = new Dictionary<BgmId, BgmTrackData> {
            { BgmId.ChorusIntro1, new BgmTrackData(0, 25600) },
            { BgmId.ChorusIntro2, new BgmTrackData(0, 51200) },
            { BgmId.Chorus1, new BgmTrackData(0, 52800) },
            { BgmId.Chorus2, new BgmTrackData(0, 129400)},
            { BgmId.Chorus3, new BgmTrackData(0, 78200)},
            { BgmId.ChorusTransition1, new BgmTrackData(0, 1550) },
            { BgmId.ChorusTransition2, new BgmTrackData(0, 1550) },
        };

        private readonly Dictionary<BgmId, List<int>> possibleTransitionTimesToNewState = new Dictionary<BgmId, List<int>> {
            { BgmId.ChorusIntro1, new List<int>{ 25600 } },
            { BgmId.ChorusIntro2, new List<int>{ 51200 } },
            { BgmId.Chorus1, new List<int>{ 25600, 52800} },
            { BgmId.Chorus2, new List<int>{ 25600, 78200, 102400, 129400} },
            { BgmId.Chorus3, new List<int>{ 25600, 78200} },
            { BgmId.ChorusTransition1, new List<int>{ 1550 } },
            { BgmId.ChorusTransition2, new List<int>{ 1550 } },
        };

        private readonly Dictionary<BgmId, string> BgmPaths = new Dictionary<BgmId, string> {
            { BgmId.ChorusIntro1, BuryTheLightFsm.GetPathToAudio("CombatChorus\\025.mp3") },
            { BgmId.ChorusIntro2, BuryTheLightFsm.GetPathToAudio("CombatChorus\\094.mp3") },
            { BgmId.Chorus1, BuryTheLightFsm.GetPathToAudio("CombatChorus\\chorus1.mp3") },
            { BgmId.Chorus2, BuryTheLightFsm.GetPathToAudio("CombatChorus\\chorus2.mp3") },
            { BgmId.Chorus3, BuryTheLightFsm.GetPathToAudio("CombatChorus\\chorus3.mp3") },
            { BgmId.ChorusTransition1, BuryTheLightFsm.GetPathToAudio("CombatChorus\\transition1.mp3") },
            { BgmId.ChorusTransition2, BuryTheLightFsm.GetPathToAudio("CombatChorus\\transition2.mp3") },
        };

        private readonly AudioService audioService;
        
        private readonly Stopwatch currentTrackStopwatch;
        private readonly Queue<ISampleProvider> samples;

        private readonly LinkedList<BgmId> loopSequence;

        private LinkedListNode<BgmId> currentTrack;

        private PeakState currentState;
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

            loopSequence = new LinkedList<BgmId>();
            loopSequence.AddLast(BgmId.Chorus1);
            loopSequence.AddLast(BgmId.Chorus2);
            loopSequence.AddLast(BgmId.Chorus3);
        }

        public void Enter()
        {
            BgmId chorus = SelectRandomChorus();
            currentTrack = new LinkedListNode<BgmId>(chorus);
            currentState = PeakState.VerseIntro;
            var sample = audioService.PlayBgm(chorus);
            if(sample != null)
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
                if(currentState != PeakState.LeavingState)
                {
                    PlayNextPart();
                }
                else
                {
                    LeaveState();
                }
                
            }

            if (currentTrackStopwatch.ElapsedMilliseconds > nextPosibleStateTransitionTime)
            {
                nextPosibleStateTransitionTime = ComputeNextStateTransitionTime();
            }
        }

        private int ComputeNextTransitionTiming()
        {
            var currentBgmData = transitionTimePerId[currentTrack.Value];
            if (currentState != PeakState.Transition)
            {
                return currentBgmData.TransitionStart;
            }
            
            return transitionTimePerId[BgmId.ChorusTransition1].TransitionStart;
        }

        private void LeaveState()
        {
            while(samples.Count > 0)
            {
                audioService.RemoveBgmPart(samples.Dequeue());
            }
            Reset();
        }

        private void PlayNextPart()
        {
            BgmId toPlay = BgmId.ChorusIntro1;
            if(currentState == PeakState.VerseIntro)
            {
                currentTrack = loopSequence.First!;
                currentState = PeakState.Loop;
                toPlay = currentTrack.Value;
            } 
            else if(currentState == PeakState.Loop)
            {
                // swap to transition
                currentState = PeakState.Transition;
                toPlay = SelectRandomTransition();
                
            } else if(currentState == PeakState.Transition)
            {
                // swap to loop
                currentState = PeakState.Loop;
                if (currentTrack.Next != null)
                {
                    currentTrack = currentTrack.Next;
                }
                else
                {
                    currentTrack = loopSequence.First!;
                }
                toPlay = currentTrack.Value;
            }

            
            if (samples.Count > 4)
            {
                samples.Dequeue();
            }

            var sample = audioService.PlayBgm(toPlay);
            if (sample != null)
            {
                samples.Enqueue(sample);
            }
            transitionTime = ComputeNextTransitionTiming();
            
            currentTrackStopwatch.Restart();
            Service.Log.Debug($"Playing {toPlay} current state {currentState}");
            Service.Log.Debug($"Next transition time at {transitionTime}");
            nextPosibleStateTransitionTime = ComputeNextStateTransitionTime();
        }

        private int ComputeNextStateTransitionTime()
        {
            List<int> possibleTransitionTimes = currentState != PeakState.Transition ? possibleTransitionTimesToNewState[currentTrack.Value] : possibleTransitionTimesToNewState[BgmId.ChorusTransition1];
            int transition = possibleTransitionTimes[0];
            for (int i = 0; i < possibleTransitionTimes.Count; i++)
            {
                if (currentTrackStopwatch.ElapsedMilliseconds < possibleTransitionTimes[i])
                {
                    transition = possibleTransitionTimes[i];
                    break;
                }
            }
            Service.Log.Debug($"Next state possible transition at {transition}");
            return transition;
        }

        public int Exit(bool outOfCombat)
        {
            nextStateTransitionTime = 0;
            if (outOfCombat)
            {
                transitionTime = 1600;
                nextPosibleStateTransitionTime = 8000;
                nextStateTransitionTime = 8000;
                audioService.PlayBgm(BgmId.CombatEnd);
            }
            currentTrackStopwatch.Restart();
            currentState = PeakState.LeavingState;
            return nextStateTransitionTime;
        }

        private BgmId SelectRandomChorus()
        {
            var random = new Random();
            return random.Next(2) < 1 ? BgmId.ChorusIntro1 : BgmId.ChorusIntro2;
        }

        private BgmId SelectRandomTransition()
        {
            var random = new Random();
            return random.Next(2) < 1 ? BgmId.ChorusTransition1 : BgmId.ChorusTransition2;
        }

        public Dictionary<BgmId, string> GetBgmPaths()
        {
            return BgmPaths;
        }

        public void Reset()
        {
            currentTrackStopwatch.Reset();
        }
    }
}
