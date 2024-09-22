using DragoonMayCry.Audio.Engine;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DragoonMayCry.Audio.BGM.FSM.States.BuryTheLight
{
    internal class BTLVerse : IFsmState
    {
        // loop between verse and riff
        enum CombatLoopState
        {
            Intro,
            CoreLoop,
            Exit
        }
        public BgmState ID { get { return BgmState.CombatLoop; } }

        private readonly Dictionary<BgmId, BgmTrackData> transitionTimePerId = new()
        {
            { BgmId.CombatEnter1, new BgmTrackData(0, 3200) },
            { BgmId.CombatEnter2, new BgmTrackData(0, 12800) },
            { BgmId.CombatVerse1, new BgmTrackData(0, 25600) },
            { BgmId.CombatVerse2, new BgmTrackData(0, 25600) },
            { BgmId.CombatCoreLoop, new BgmTrackData(0, 80000) },
            { BgmId.CombatCoreLoopTransition1, new BgmTrackData(0, 12800) },
            { BgmId.CombatCoreLoopTransition2, new BgmTrackData(0, 12800) },
            { BgmId.CombatCoreLoopExit1, new BgmTrackData(1590, 1600) },
            { BgmId.CombatCoreLoopExit2, new BgmTrackData(1590, 1600) },
            { BgmId.CombatCoreLoopExit3, new BgmTrackData(1590, 1600) },
        };

        private readonly Dictionary<BgmId, string> bgmPaths = new()
        {
            { BgmId.CombatEnter1, DynamicBgmService.GetPathToAudio("BuryTheLight\\CombatLoop\\111.ogg") },
            { BgmId.CombatEnter2, DynamicBgmService.GetPathToAudio("BuryTheLight\\CombatLoop\\029.ogg") },
            { BgmId.CombatVerse1, DynamicBgmService.GetPathToAudio("BuryTheLight\\CombatLoop\\017.ogg") },
            { BgmId.CombatVerse2, DynamicBgmService.GetPathToAudio("BuryTheLight\\CombatLoop\\040.ogg") },
            { BgmId.CombatCoreLoop, DynamicBgmService.GetPathToAudio("BuryTheLight\\CombatLoop\\coreloop.ogg") },
            { BgmId.CombatCoreLoopTransition1, DynamicBgmService.GetPathToAudio("BuryTheLight\\CombatLoop\\029.ogg") },
            { BgmId.CombatCoreLoopTransition2, DynamicBgmService.GetPathToAudio("BuryTheLight\\CombatLoop\\064.ogg") },
            { BgmId.CombatCoreLoopExit1, DynamicBgmService.GetPathToAudio("BuryTheLight\\CombatLoop\\039.ogg") },
            { BgmId.CombatCoreLoopExit2, DynamicBgmService.GetPathToAudio("BuryTheLight\\CombatLoop\\092.ogg") },
            { BgmId.CombatCoreLoopExit3, DynamicBgmService.GetPathToAudio("BuryTheLight\\CombatLoop\\093.ogg") },
        };

        private LinkedList<BgmId> combatLoop = new();
        private readonly LinkedList<BgmId> combatIntro = new();
        private LinkedListNode<BgmId>? currentTrack;
        private readonly AudioService audioService;
        private readonly Stopwatch currentTrackStopwatch;
        private CombatLoopState currentState;
        private int transitionTime = 0;
        private readonly Queue<ISampleProvider> samples;
        private readonly Random rand;
        public BTLVerse(AudioService audioService)
        {
            rand = new Random();
            this.audioService = audioService;
            currentTrackStopwatch = new Stopwatch();
            samples = new Queue<ISampleProvider>();

            combatIntro.AddLast(BgmId.CombatEnter1);
            combatIntro.AddLast(BgmId.CombatEnter2);

        }

        public void Enter(bool fromVerse)
        {
            combatLoop = GenerateCombatLoop();
            currentTrackStopwatch.Restart();
            ISampleProvider? sample;
            if (fromVerse)
            {
                currentTrack = combatLoop.First!;
                sample = audioService.PlayBgm(currentTrack.Value);
                currentState = CombatLoopState.CoreLoop;
            }
            else
            {
                currentTrack = combatIntro.First!;
                sample = audioService.PlayBgm(currentTrack.Value);
                currentState = CombatLoopState.Intro;
            }
            if (sample != null)
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

            if (currentTrackStopwatch.IsRunning && currentTrackStopwatch.ElapsedMilliseconds >= transitionTime)
            {
                if (currentState != CombatLoopState.Exit)
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
                if (currentTrack!.Next == null)
                {
                    currentTrack = combatLoop.First!;
                    currentState = CombatLoopState.CoreLoop;
                }
                else
                {
                    currentTrack = currentTrack.Next;
                }
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
            return transitionTimePerId[currentTrack!.Value].TransitionStart;
        }

        private void LeaveState()
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
            currentState = CombatLoopState.CoreLoop;
            currentTrackStopwatch.Reset();
        }

        public int Exit(ExitType exit)
        {
            transitionTime = exit == ExitType.EndOfCombat ? 1600 : transitionTimePerId[BgmId.CombatCoreLoopExit1].EffectiveStart;
            var nextTransitionTime = transitionTimePerId[BgmId.CombatCoreLoopExit1].TransitionStart;
            if (currentState == CombatLoopState.Exit)
            {
                nextTransitionTime = (int)Math.Max(nextTransitionTime - currentTrackStopwatch.ElapsedMilliseconds, 0);
            }
            else if (exit == ExitType.ImmediateExit)
            {
                transitionTime = 0;
                LeaveState();
                nextTransitionTime = 0;
            }
            else
            {
                if (exit == ExitType.Promotion)
                {

                    audioService.PlayBgm(SelectRandom(BgmId.CombatCoreLoopExit1, BgmId.CombatCoreLoopExit2, BgmId.CombatCoreLoopExit3));
                }
                else if (exit == ExitType.EndOfCombat && currentState != CombatLoopState.Exit)
                {
                    audioService.PlayBgm(BgmId.CombatEnd);
                    nextTransitionTime = 8000;
                }
                currentTrackStopwatch.Restart();
            }
            currentState = CombatLoopState.Exit;


            return nextTransitionTime;
        }

        public Dictionary<BgmId, string> GetBgmPaths()
        {
            return bgmPaths;
        }

        public void Reset()
        {
            LeaveState();
        }

        private BgmId SelectRandom(params BgmId[] bgmIds)
        {
            var index = rand.Next(0, bgmIds.Length);
            return bgmIds[index];
        }

        private Queue<BgmId> RandomizeQueue(params BgmId[] bgmIds)
        {
            var k = rand.Next(2);
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

        private LinkedList<BgmId> GenerateCombatLoop()
        {
            var verseQueue = RandomizeQueue(BgmId.CombatVerse1, BgmId.CombatVerse2);
            var loopTransitionQueue = RandomizeQueue(BgmId.CombatCoreLoopTransition1, BgmId.CombatCoreLoopTransition2);
            var loop = new LinkedList<BgmId>();
            loop.AddLast(verseQueue.Dequeue());
            loop.AddLast(BgmId.CombatCoreLoop);
            loop.AddLast(loopTransitionQueue.Dequeue());
            loop.AddLast(verseQueue.Dequeue());
            loop.AddLast(BgmId.CombatCoreLoop);
            loop.AddLast(loopTransitionQueue.Dequeue());
            return loop;
        }

        public bool CancelExit()
        {
            return false;
        }
    }
}
