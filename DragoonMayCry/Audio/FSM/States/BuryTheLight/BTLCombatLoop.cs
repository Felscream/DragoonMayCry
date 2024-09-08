using Lumina.Data.Parsing;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.TimeZoneInfo;

namespace DragoonMayCry.Audio.FSM.States.BuryTheLight
{
    internal class BTLCombatLoop : FsmState
    {
        // loop between verse and riff
        enum CombatLoopState
        {
            Intro,
            CoreLoop,
            Exit
        }
        public BgmState ID { get { return BgmState.CombatLoop; } }

        private readonly Dictionary<BgmId, BgmTrackData> transitionTimePerId = new Dictionary<BgmId, BgmTrackData> {
            { BgmId.CombatEnter, new BgmTrackData(1590, 12800) },
            { BgmId.CombatVerse1, new BgmTrackData(0, 25412) },
            { BgmId.CombatVerse2, new BgmTrackData(0, 25412) },
            { BgmId.CombatCoreLoop, new BgmTrackData(0, 91950) },
            { BgmId.CombatCoreLoopExit, new BgmTrackData(1590, 4780) },
        };

        private readonly Dictionary<BgmId, string> BgmPaths = new Dictionary<BgmId, string> {
            { BgmId.CombatEnter, BuryTheLightFsm.GetPathToAudio("CombatLoop\\029.mp3") },
            { BgmId.CombatVerse1, BuryTheLightFsm.GetPathToAudio("CombatLoop\\017.mp3") },
            { BgmId.CombatVerse2, BuryTheLightFsm.GetPathToAudio("CombatLoop\\040.mp3") },
            { BgmId.CombatCoreLoop, BuryTheLightFsm.GetPathToAudio("CombatLoop\\coreloop.mp3") },
            { BgmId.CombatCoreLoopExit, BuryTheLightFsm.GetPathToAudio("CombatLoop\\093.mp3") },
        };

        private LinkedList<BgmId> combatLoop = new LinkedList<BgmId>();
        private LinkedListNode<BgmId>? currentTrack;
        private readonly AudioService audioService;
        private readonly Stopwatch currentTrackStopwatch;
        private CombatLoopState currentState;
        private int transitionTime = 0;
        private readonly Queue<ISampleProvider> samples;
        public BTLCombatLoop(AudioService audioService)
        {
            this.audioService = audioService;
            currentTrackStopwatch = new Stopwatch();
            samples = new Queue<ISampleProvider>();
        }

        public void Enter(bool fromVerse)
        {
            Service.Log.Debug("Entering Combat loop");
            combatLoop = GenerateCombatLoop();
            currentTrackStopwatch.Restart();
            ISampleProvider? sample;
            if(fromVerse)
            {
                currentTrack = combatLoop.First!;
                sample = audioService.PlayBgm(currentTrack.Value);
                currentState = CombatLoopState.CoreLoop;
            }
            else
            {
                currentTrack = new LinkedListNode<BgmId>(BgmId.CombatEnter);
                sample = audioService.PlayBgm(currentTrack.Value);
                currentState = CombatLoopState.Intro;
            }
            if(sample != null)
            {
                samples.Enqueue(sample);
            }
            transitionTime = ComputeNextTransitionTiming();
            currentTrackStopwatch.Restart();
        }

        public void Update()
        {
            if (!currentTrackStopwatch.IsRunning)
            {
                return;
            }
            
            if(currentTrackStopwatch.IsRunning && currentTrackStopwatch.ElapsedMilliseconds >= transitionTime)
            {
                if(currentState != CombatLoopState.Exit)
                {
                    PlayNextPart();
                    currentTrackStopwatch.Restart();
                }
                else
                {
                    LeaveState();
                }
                
            }
        }

        private void PlayNextPart()
        {
            // transition to loop state if we reached the end of intro
            if (currentState == CombatLoopState.Intro)
            {
                currentTrack = combatLoop.First!;
                currentState = CombatLoopState.CoreLoop;
            }
            else if (currentState == CombatLoopState.CoreLoop)
            {
                if (currentTrack!.Next != null)
                {
                    currentTrack = currentTrack.Next;
                }
                else
                {
                    currentTrack = combatLoop.First!;
                }
            }

            Service.Log.Debug($"Playing {currentTrack!.Value}");
            PlayBgmPart();
            transitionTime = ComputeNextTransitionTiming();
            currentTrackStopwatch.Restart();
        }

        private void PlayBgmPart()
        {
            if (samples.Count > 4)
            {
                samples.Dequeue();
            }

            var sample = audioService.PlayBgm(currentTrack!.Value);
            if (sample != null)
            {
                samples.Enqueue(sample);
            }
        }

        private int ComputeNextTransitionTiming()
        {
            int transitionUpdateDelay = (int)Math.Max(currentTrackStopwatch.Elapsed.TotalMilliseconds - transitionTime, 0);
            int timing = -1;
            if (currentTrack!.Next != null)
            {
                var currentBgmData = transitionTimePerId[currentTrack.Value];
                var nextBgmData = transitionTimePerId[currentTrack.Next.Value];
                timing = currentBgmData.TransitionStart - nextBgmData.EffectiveStart;
            }
            else
            {
                var currentBgmData = transitionTimePerId[currentTrack.Value];
                var nextBgmData = transitionTimePerId[combatLoop.First!.Value];
                timing = currentBgmData.TransitionStart - nextBgmData.EffectiveStart;
            }

            return timing - transitionUpdateDelay;
        }

        private void LeaveState()
        {
            while (samples.Count > 0)
            {
                Service.Log.Debug("Removing samples");
                audioService.RemoveBgmPart(samples.Dequeue());
            }
            currentState = CombatLoopState.CoreLoop;
            currentTrackStopwatch.Reset();
        }

        public int Exit(ExitType exit)
        {
            var nextTransitionTime = transitionTimePerId[BgmId.CombatCoreLoopExit].TransitionStart;
            if(currentState == CombatLoopState.Exit)
            {
                nextTransitionTime = (int)Math.Max(nextTransitionTime - currentTrackStopwatch.ElapsedMilliseconds, 0);
            } 
            else
            {
                if (exit == ExitType.Promotion)
                {
                    audioService.PlayBgm(BgmId.CombatCoreLoopExit);
                } else if(exit == ExitType.EndOfCombat)
                {
                    audioService.PlayBgm(BgmId.CombatEnd);
                    nextTransitionTime = 8000;
                }
                currentTrackStopwatch.Restart();
            }
            currentState = CombatLoopState.Exit;
            transitionTime = exit == ExitType.EndOfCombat ? 1600 : transitionTimePerId[BgmId.CombatCoreLoopExit].EffectiveStart;
            
            return nextTransitionTime;
        }

        public Dictionary<BgmId, string> GetBgmPaths()
        {
            return BgmPaths;
        }

        public void Reset()
        {
            
        }

        private Queue<BgmId> RandomizeVerseQueue()
        {
            Random rand = new Random();
            int k = rand.Next(2);
            var queue = new Queue<BgmId>();
            if(k < 1)
            {
                queue.Enqueue(BgmId.CombatVerse1);
                queue.Enqueue(BgmId.CombatVerse2);
            } else
            {
                queue.Enqueue(BgmId.CombatVerse2);
                queue.Enqueue(BgmId.CombatVerse1);
            }
            return queue;
        }

        private LinkedList<BgmId> GenerateCombatLoop()
        {
            var verseQueue = RandomizeVerseQueue();
            LinkedList<BgmId> res = new LinkedList<BgmId>();
            res.AddLast(verseQueue.Dequeue());
            res.AddLast(BgmId.CombatCoreLoop);
            res.AddLast(verseQueue.Dequeue());
            res.AddLast(BgmId.CombatCoreLoop);
            return res;
        }

        public void CancelExit()
        {

        }
    }
}
