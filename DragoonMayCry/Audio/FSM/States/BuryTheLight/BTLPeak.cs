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
            LeavingStateOutOfCombat,
            LeavingStateDemotion,
            CleaningUpDemotion
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
            { BgmId.Demotion, new BgmTrackData(0, 25600) },
        };

        private readonly Dictionary<BgmId, List<int>> possibleTransitionTimesToNewState = new Dictionary<BgmId, List<int>> {
            { BgmId.ChorusIntro1, new List<int>{ 27200 } },
            { BgmId.ChorusIntro2, new List<int>{ 52800 } },
            { BgmId.Chorus1, new List<int>{ 27200, 52800} },
            { BgmId.Chorus2, new List<int>{ 27200, 78400, 104000, 129600} },
            { BgmId.Chorus3, new List<int>{ 27200, 78400 } },
            { BgmId.ChorusTransition1, new List<int>{ 3220 } },
            { BgmId.ChorusTransition2, new List<int>{ 3220 } },
        };

        private readonly Dictionary<BgmId, string> bgmPaths = new Dictionary<BgmId, string> {
            { BgmId.ChorusIntro1, BuryTheLightFsm.GetPathToAudio("CombatChorus\\025.mp3") },
            { BgmId.ChorusIntro2, BuryTheLightFsm.GetPathToAudio("CombatChorus\\094.mp3") },
            { BgmId.Chorus1, BuryTheLightFsm.GetPathToAudio("CombatChorus\\chorus1.mp3") },
            { BgmId.Chorus2, BuryTheLightFsm.GetPathToAudio("CombatChorus\\chorus2.mp3") },
            { BgmId.Chorus3, BuryTheLightFsm.GetPathToAudio("CombatChorus\\chorus3.mp3") },
            { BgmId.ChorusTransition1, BuryTheLightFsm.GetPathToAudio("CombatChorus\\transition1.mp3") },
            { BgmId.ChorusTransition2, BuryTheLightFsm.GetPathToAudio("CombatChorus\\transition2.mp3") },
            { BgmId.Demotion, BuryTheLightFsm.GetPathToAudio("CombatChorus\\demotion.mp3") },
        };

        private readonly AudioService audioService;
        
        private readonly Stopwatch currentTrackStopwatch;
        private readonly Queue<ISampleProvider> samples;

        
        private readonly LinkedList<BgmId> loopSequence;

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

            loopSequence = new LinkedList<BgmId>();
            loopSequence.AddLast(BgmId.Chorus1);
            loopSequence.AddLast(BgmId.Chorus2);
            loopSequence.AddLast(BgmId.Chorus3);
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

            if (currentTrackStopwatch.ElapsedMilliseconds > nextPosibleStateTransitionTime && currentState != PeakState.LeavingStateDemotion)
            {
                nextPosibleStateTransitionTime = ComputeNextStateTransitionTime();
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
            var currentBgmData = transitionTimePerId[currentTrack!.Value];
            if (currentState != PeakState.Transition)
            {
                return currentBgmData.TransitionStart;
            }
            
            return transitionTimePerId[BgmId.ChorusTransition1].TransitionStart;
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
                if (currentTrack!.Next != null)
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
            List<int> possibleTransitionTimes = currentState != PeakState.Transition ? possibleTransitionTimesToNewState[currentTrack!.Value] : possibleTransitionTimesToNewState[BgmId.ChorusTransition1];
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

        private BgmId SelectRandomTransition()
        {
            var random = new Random();
            return random.Next(2) < 1 ? BgmId.ChorusTransition1 : BgmId.ChorusTransition2;
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
            transitionTime = transitionTimePerId[currentTrack.Value].TransitionStart;

        }
    }
}
