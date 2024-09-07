using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DragoonMayCry.Audio.FSM.States.BuryTheLight
{
    public class BTLIntro : FsmState
    {
        enum IntroStates
        {
            Intro,
            Loop,
            TransitionToCombat
        }

        public string Name { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public BgmState ID { get; set; }

        private Dictionary<BgmId, BgmTrackData> transitionTimePerId = new Dictionary<BgmId, BgmTrackData> {
            { BgmId.IntroOne, new BgmTrackData(1.6d, 14.5d) },
            { BgmId.IntroTwo, new BgmTrackData(1.6d, 14.5d) },
            { BgmId.IntroThree, new BgmTrackData(1.6d, 14.5d) },
            { BgmId.IntroLoopOne, new BgmTrackData(1.59d, 14.4d) },
            { BgmId.IntroLoopTwo, new BgmTrackData(1.59d, 14.41d) }
        };

        public Dictionary<BgmId, string> BgmPaths = new Dictionary<BgmId, string> {
            { BgmId.IntroOne, BuryTheLightFsm.GetPathToAudio("Intro\\083.mp3") },
            { BgmId.IntroTwo, BuryTheLightFsm.GetPathToAudio("Intro\\012.mp3") },
            { BgmId.IntroThree, BuryTheLightFsm.GetPathToAudio("Intro\\048.mp3") },
            { BgmId.IntroLoopOne, BuryTheLightFsm.GetPathToAudio("Intro\\033.mp3") },
            { BgmId.IntroLoopTwo, BuryTheLightFsm.GetPathToAudio("Intro\\055.mp3") }
        };

        private LinkedList<BgmId> outOfCombatLoop;
        private LinkedList<BgmId> introSequence;
        private LinkedList<BgmId> currentSequence;
        private AudioService audioService;
        private Stopwatch currentTrackStopwatch;
        private LinkedListNode<BgmId> currentBgmNode;
        private const float MaxTime = 30f;
        private double transitionTime = 0f;
        private IntroStates currentState;

        public BTLIntro(BgmState id, AudioService audioService)
        {
            ID = id;
            currentTrackStopwatch = new Stopwatch();
            introSequence = new LinkedList<BgmId>();
            introSequence.AddLast(BgmId.IntroOne);
            introSequence.AddLast(BgmId.IntroTwo);
            introSequence.AddLast(BgmId.IntroThree);

            outOfCombatLoop = new LinkedList<BgmId>();
            outOfCombatLoop.AddLast(BgmId.IntroLoopOne);
            outOfCombatLoop.AddLast(BgmId.IntroLoopTwo);

            currentSequence = new LinkedList<BgmId>();
            this.audioService = audioService;
        }

        public void Start()
        {
            currentState = IntroStates.Intro;
            currentSequence = introSequence;
            currentBgmNode = currentSequence.First!;
#if DEBUG
            currentState = IntroStates.Loop;
            currentSequence = outOfCombatLoop;
            currentBgmNode = currentSequence.First!;
#endif
            Service.Log.Debug($"Playing {currentBgmNode.Value}");
            audioService.PlayBgm(currentBgmNode.Value);
            currentTrackStopwatch.Restart();
            ComputeNextTransitionTiming();
        }

        private void ComputeNextTransitionTiming()
        {
            if (currentBgmNode.Next != null)
            {
                var currentBgmData = transitionTimePerId[currentBgmNode.Value];
                var nextBgmData = transitionTimePerId[currentBgmNode.Next.Value];
                transitionTime = currentBgmData.TransitionStart - nextBgmData.EffectiveStart;
            }
            else if(currentState == IntroStates.Intro)
            {
                var currentBgmData = transitionTimePerId[currentBgmNode.Value];
                var nextBgmData = transitionTimePerId[outOfCombatLoop.First!.Value];
                transitionTime = currentBgmData.TransitionStart - nextBgmData.EffectiveStart;
            }
            else if(currentState == IntroStates.Loop) {
                var currentBgmData = transitionTimePerId[currentBgmNode.Value];
                var nextBgmData = transitionTimePerId[currentSequence.First!.Value];
                // the loop isn't perfect, the substraction is an adjustment
                transitionTime = currentBgmData.TransitionStart - nextBgmData.EffectiveStart - 0.05f;
            } else
            {
                Service.Log.Warning($"Cannot continue current BGM sequence");
                transitionTime = 0;
            }
        }

        public void Update()
        {
            if(!currentTrackStopwatch.IsRunning)
            {
                return;
            }

            Service.Log.Debug($"Current ID {currentBgmNode.Value} - elasped : {currentTrackStopwatch.Elapsed.TotalSeconds}");
            if(currentTrackStopwatch.Elapsed.TotalSeconds > transitionTime && transitionTime != 0)
            {
                // transition to loop state if we reached the end of intro
                if(currentState == IntroStates.Intro && currentBgmNode.Next == null) {
                    currentSequence = outOfCombatLoop;
                    currentState = IntroStates.Loop;
                } 
                
                if(currentBgmNode.Next != null)
                {
                    currentBgmNode = currentBgmNode.Next;
                    
                } else if(currentState == IntroStates.Loop)
                {
                    currentBgmNode = currentSequence.First!;
                }

                Service.Log.Debug($"Playing {currentBgmNode.Value}");
                audioService.PlayBgm(currentBgmNode.Value);
                currentTrackStopwatch.Restart();
                ComputeNextTransitionTiming();
            }

            if(currentTrackStopwatch.Elapsed.TotalSeconds > MaxTime)
            {
                currentTrackStopwatch.Reset();
            }
        }

        public void Reset()
        {
            currentBgmNode = introSequence.First!;
            currentTrackStopwatch.Reset();
        }

        public Dictionary<BgmId, string> GetBgmPaths()
        {
            return BgmPaths;
        }
    }
}
