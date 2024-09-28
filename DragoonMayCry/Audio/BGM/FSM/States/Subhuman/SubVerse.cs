using DragoonMayCry.Audio.Engine;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DragoonMayCry.Audio.BGM.FSM.States.DevilTrigger
{
    internal class SubVerse : IFsmState
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
            { BgmId.CombatEnter1, new BgmTrackData(0, 1300) },
            { BgmId.CombatEnter2, new BgmTrackData(0, 20650) },
            { BgmId.CombatEnter3, new BgmTrackData(0, 5100) },
            { BgmId.CombatVerse1, new BgmTrackData(0, 20650) },
            { BgmId.CombatVerse2, new BgmTrackData(0, 20650) },
            { BgmId.CombatVerse3, new BgmTrackData(0, 20650) },
            { BgmId.CombatVerse4, new BgmTrackData(0, 20650) },
            { BgmId.CombatCoreLoopTransition1, new BgmTrackData(0, 20650) },
            { BgmId.CombatCoreLoopTransition2, new BgmTrackData(0, 20650) },
            { BgmId.CombatCoreLoopTransition3, new BgmTrackData(0, 20650) },
            { BgmId.CombatCoreLoopTransition4, new BgmTrackData(0, 20650) },
            { BgmId.CombatCoreLoopExit1, new BgmTrackData(1259, 2650) },
            { BgmId.CombatCoreLoopExit2, new BgmTrackData(1259, 2550) },
            { BgmId.CombatCoreLoopExit3, new BgmTrackData(1259, 2600) },
        };

        private readonly Dictionary<BgmId, string> bgmPaths = new()
        {
            { BgmId.CombatEnter1, DynamicBgmService.GetPathToAudio("Subhuman\\Verse\\014.ogg") },
            { BgmId.CombatEnter2, DynamicBgmService.GetPathToAudio("Subhuman\\Verse\\097.ogg") },
            { BgmId.CombatEnter3, DynamicBgmService.GetPathToAudio("Subhuman\\Verse\\022.ogg") },
            { BgmId.CombatVerse1, DynamicBgmService.GetPathToAudio("Subhuman\\Verse\\018.ogg") },
            { BgmId.CombatVerse2, DynamicBgmService.GetPathToAudio("Subhuman\\Verse\\015.ogg") },
            { BgmId.CombatVerse3, DynamicBgmService.GetPathToAudio("Subhuman\\Verse\\115.ogg") },
            { BgmId.CombatVerse4, DynamicBgmService.GetPathToAudio("Subhuman\\Verse\\002.ogg") },
            { BgmId.CombatCoreLoopTransition1, DynamicBgmService.GetPathToAudio("Subhuman\\Verse\\071.ogg") },
            { BgmId.CombatCoreLoopTransition2, DynamicBgmService.GetPathToAudio("Subhuman\\Verse\\099.ogg") },
            { BgmId.CombatCoreLoopTransition3, DynamicBgmService.GetPathToAudio("Subhuman\\Verse\\020.ogg") },
            { BgmId.CombatCoreLoopTransition4, DynamicBgmService.GetPathToAudio("Subhuman\\Verse\\067.ogg") },
            { BgmId.CombatCoreLoopExit1, DynamicBgmService.GetPathToAudio("Subhuman\\Verse\\053.ogg") },
            { BgmId.CombatCoreLoopExit2, DynamicBgmService.GetPathToAudio("Subhuman\\Verse\\065.ogg") },
            { BgmId.CombatCoreLoopExit3, DynamicBgmService.GetPathToAudio("Subhuman\\Verse\\001.ogg") },
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
        public SubVerse(AudioService audioService)
        {
            rand = new Random();
            this.audioService = audioService;
            currentTrackStopwatch = new Stopwatch();
            samples = new Queue<ISampleProvider>();

            combatIntro.AddLast(BgmId.CombatEnter1);
            combatIntro.AddLast(BgmId.CombatEnter2);
            combatIntro.AddLast(BgmId.CombatEnter3);
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
                }
                else
                {
                    LeaveState();
                }

            }
        }

        private void PlayNextPart()
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

            PlayBgmPart();
            transitionTime = ComputeNextTransitionTiming();
            currentTrackStopwatch.Restart();
        }

        private void PlayBgmPart()
        {
            var sample = audioService.PlayBgm(currentTrack!.Value);
            if (sample != null)
            {
                samples.Enqueue(sample);
            }
            if (samples.Count > 4)
            {
                samples.Dequeue();
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
            var nextTransitionTime = transitionTimePerId[BgmId.CombatCoreLoopExit1].TransitionStart;
            var selectedTransition = BgmId.CombatCoreLoopExit1;
            if (currentState == CombatLoopState.Exit)
            {
                nextTransitionTime = (int)Math.Max(nextTransitionTime - currentTrackStopwatch.ElapsedMilliseconds, 0);
            }
            else
            {
                if (exit == ExitType.Promotion)
                {
                    selectedTransition = SelectRandom(BgmId.CombatCoreLoopExit1, BgmId.CombatCoreLoopExit2, BgmId.CombatCoreLoopExit3);
                    audioService.PlayBgm(selectedTransition);
                    nextTransitionTime = transitionTimePerId[selectedTransition].TransitionStart;
                }
                else if (exit == ExitType.EndOfCombat && currentState != CombatLoopState.Exit)
                {
                    audioService.PlayBgm(BgmId.CombatEnd, 0, 9000, 6000);
                    nextTransitionTime = 6000;
                }
                currentTrackStopwatch.Restart();
            }
            currentState = CombatLoopState.Exit;
            transitionTime = exit == ExitType.EndOfCombat ? 1400 : transitionTimePerId[selectedTransition].EffectiveStart;

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

            var list = new List<BgmId>(bgmIds);
            var queue = new Queue<BgmId>();
            while (list.Count > 0)
            {
                var k = rand.Next(list.Count);
                queue.Enqueue(list[k]);
                list.RemoveAt(k);
            }
            return queue;
        }



        private LinkedList<BgmId> GenerateCombatLoop()
        {
            var verseQueue = RandomizeQueue(BgmId.CombatVerse1, BgmId.CombatVerse2);
            var verseQueue2 = RandomizeQueue(BgmId.CombatVerse3, BgmId.CombatVerse4);
            var transitionQueue = RandomizeQueue(BgmId.CombatCoreLoopTransition1, BgmId.CombatCoreLoopTransition2, BgmId.CombatCoreLoopTransition3);
            var loop = new LinkedList<BgmId>();
            loop.AddLast(verseQueue.Dequeue());
            loop.AddLast(verseQueue2.Dequeue());
            loop.AddLast(transitionQueue.Dequeue());
            loop.AddLast(BgmId.CombatCoreLoopTransition4);
            loop.AddLast(verseQueue.Dequeue());
            loop.AddLast(verseQueue2.Dequeue());
            loop.AddLast(transitionQueue.Dequeue());
            loop.AddLast(BgmId.CombatCoreLoopTransition4);
            return loop;
        }

        public bool CancelExit()
        {
            return false;
        }
    }
}
