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
            CoreLoop
        }
        public string Name { get; set; }
        public BgmState ID { get; set; }

        private readonly Dictionary<BgmId, BgmTrackData> transitionTimePerId = new Dictionary<BgmId, BgmTrackData> {
            { BgmId.CombatEnter, new BgmTrackData(1590, 12810) },
            { BgmId.CombatVerse1, new BgmTrackData(0, 25412) },
            { BgmId.CombatVerse2, new BgmTrackData(0, 25412) },
            { BgmId.CombatCoreLoop, new BgmTrackData(0, 91956) },
        };

        private readonly Dictionary<BgmId, string> BgmPaths = new Dictionary<BgmId, string> {
            { BgmId.CombatEnter, BuryTheLightFsm.GetPathToAudio("CombatLoop\\029.mp3") },
            { BgmId.CombatVerse1, BuryTheLightFsm.GetPathToAudio("CombatLoop\\017.mp3") },
            { BgmId.CombatVerse2, BuryTheLightFsm.GetPathToAudio("CombatLoop\\040.mp3") },
            { BgmId.CombatCoreLoop, BuryTheLightFsm.GetPathToAudio("CombatLoop\\coreloop.mp3") },
        };

        private LinkedList<BgmId> combatLoop = new LinkedList<BgmId>();
        private LinkedListNode<BgmId> currentTrack;
        private readonly AudioService audioService;
        private readonly Stopwatch currentTrackStopwatch;
        private CombatLoopState currentState;
        private decimal transitionTime = 0m;
        private readonly Queue<ISampleProvider> samples;
        public BTLCombatLoop(BgmState id, AudioService audioService)
        {
            this.audioService = audioService;
            ID = id;
            currentTrackStopwatch = new Stopwatch();
            samples = new Queue<ISampleProvider>();
        }

        public void Enter(bool fromVerse)
        {
            Service.Log.Debug("Entering Combat loop");
            combatLoop = GenerateCombatLoop();
            currentTrackStopwatch.Restart();
            if(fromVerse)
            {
                currentTrack = combatLoop.First!;
                audioService.PlayBgm(currentTrack.Value);
            }
            else
            {
                currentTrack = new LinkedListNode<BgmId>(BgmId.CombatEnter);
                audioService.PlayBgm(currentTrack.Value);
                currentState = CombatLoopState.Intro;
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
            Service.Log.Debug($"{currentTrackStopwatch.ElapsedMilliseconds} vs transition {transitionTime}");
            
            if(currentTrackStopwatch.IsRunning && currentTrackStopwatch.ElapsedMilliseconds > transitionTime)
            {
                PlayNextPart();
                currentTrackStopwatch.Restart();
            }
        }

        private void PlayNextPart()
        {
            Service.Log.Debug($"Current time {currentTrackStopwatch.Elapsed.TotalMilliseconds} vs expected transition time {transitionTime}");
            // transition to loop state if we reached the end of intro
            if (currentState == CombatLoopState.Intro)
            {
                currentTrack = combatLoop.First!;
                currentState = CombatLoopState.CoreLoop;
            } else if(currentState == CombatLoopState.CoreLoop)
            {
                if (currentTrack.Next != null)
                {
                    currentTrack = currentTrack.Next;

                }
                else
                {
                    currentTrack = combatLoop.First!;
                }
            }

            Service.Log.Debug($"Playing {currentTrack.Value}");

            if (samples.Count > 2)
            {
                samples.Dequeue();
            }

            var sample = audioService.PlayBgm(currentTrack.Value);
            if (sample != null)
            {
                samples.Enqueue(sample);
            }
            transitionTime = ComputeNextTransitionTiming();
            currentTrackStopwatch.Restart();
        }

        private decimal ComputeNextTransitionTiming()
        {
            decimal transitionUpdateDelay = Math.Max(new decimal(currentTrackStopwatch.Elapsed.TotalSeconds) - transitionTime, 0m);
            decimal timing = -1m;
            if (currentTrack.Next != null)
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

        public int Exit()
        {
            return 0;
        }

        public Dictionary<BgmId, string> GetBgmPaths()
        {
            return BgmPaths;
        }

        public void OnEnter()
        {
            
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

        public void Enter()
        {
            Enter(false);
        }
    }
}
