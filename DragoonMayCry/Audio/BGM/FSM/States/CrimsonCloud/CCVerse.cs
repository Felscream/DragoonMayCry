using DragoonMayCry.Audio.Engine;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DragoonMayCry.Audio.BGM.FSM.States.CrimsonCloud
{
    internal class CCVerse : IFsmState
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
            { BgmId.CombatEnter1, new BgmTrackData(0, 2500) },
            { BgmId.CombatEnter2, new BgmTrackData(0, 10400) },
            { BgmId.CombatEnter3, new BgmTrackData(0, 2590) },
            { BgmId.CombatVerse1, new BgmTrackData(0, 20650) },
            { BgmId.CombatVerse2, new BgmTrackData(0, 20650) },
            { BgmId.CombatVerse3, new BgmTrackData(0, 20600) },
            { BgmId.CombatVerse4, new BgmTrackData(0, 20600) },
            { BgmId.CombatCoreLoopTransition1, new BgmTrackData(0, 19355) },
            { BgmId.CombatCoreLoopTransition2, new BgmTrackData(1300, 21900) },
            { BgmId.CombatCoreLoopTransition3, new BgmTrackData(0, 10295) },
            { BgmId.CombatCoreLoopExit1, new BgmTrackData(1, 1000) },
            { BgmId.CombatCoreLoopExit2, new BgmTrackData(1, 2555) },
        };

        private readonly Dictionary<BgmId, string> bgmPaths = new()
        {
            { BgmId.CombatEnter1, DynamicBgmService.GetPathToAudio("CrimsonCloud\\Verse\\004.ogg") },
            { BgmId.CombatEnter2, DynamicBgmService.GetPathToAudio("CrimsonCloud\\Verse\\069.ogg") },
            { BgmId.CombatEnter3, DynamicBgmService.GetPathToAudio("CrimsonCloud\\Verse\\036.ogg") },
            { BgmId.CombatVerse1, DynamicBgmService.GetPathToAudio("CrimsonCloud\\Verse\\110.ogg") },
            { BgmId.CombatVerse2, DynamicBgmService.GetPathToAudio("CrimsonCloud\\Verse\\056.ogg") },
            { BgmId.CombatVerse3, DynamicBgmService.GetPathToAudio("CrimsonCloud\\Verse\\007.ogg") },
            { BgmId.CombatVerse4, DynamicBgmService.GetPathToAudio("CrimsonCloud\\Verse\\074.ogg") },
            { BgmId.CombatCoreLoopTransition1, DynamicBgmService.GetPathToAudio("CrimsonCloud\\Verse\\052.ogg") },
            { BgmId.CombatCoreLoopTransition2, DynamicBgmService.GetPathToAudio("CrimsonCloud\\Verse\\060.ogg") },
            { BgmId.CombatCoreLoopTransition3, DynamicBgmService.GetPathToAudio("CrimsonCloud\\Verse\\108.ogg") },
            { BgmId.CombatCoreLoopExit1, DynamicBgmService.GetPathToAudio("CrimsonCloud\\Verse\\038.ogg") },
            { BgmId.CombatCoreLoopExit2, DynamicBgmService.GetPathToAudio("CrimsonCloud\\Verse\\045.ogg") },
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
        private BgmId selectedChorusTransisition = BgmId.None;
        public CCVerse(AudioService audioService)
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
            combatLoop = GenerateCombatLoop();
            currentTrackStopwatch.Restart();
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
            var next = currentTrack!.Next;
            if (next == null)
            {
                next = combatLoop.First!;
            }
            return transitionTimePerId[currentTrack!.Value].TransitionStart - transitionTimePerId[next.Value].EffectiveStart;
        }

        private void LeaveState()
        {
            selectedChorusTransisition = BgmId.None;
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
            if (selectedChorusTransisition != BgmId.None)
            {
                nextTransitionTime = transitionTimePerId[selectedChorusTransisition].TransitionStart;
            }

            if (currentState == CombatLoopState.Exit)
            {
                nextTransitionTime = (int)Math.Max(nextTransitionTime - currentTrackStopwatch.ElapsedMilliseconds, 0);
            }
            else
            {
                if (exit == ExitType.Promotion)
                {
                    selectedChorusTransisition = SelectRandom(BgmId.CombatCoreLoopExit1, BgmId.CombatCoreLoopExit2);
                    audioService.PlayBgm(selectedChorusTransisition);
                    nextTransitionTime = transitionTimePerId[selectedChorusTransisition].TransitionStart;
                }
                else if (exit == ExitType.EndOfCombat && currentState != CombatLoopState.Exit)
                {
                    audioService.PlayBgm(BgmId.CombatEnd);
                    nextTransitionTime = 4500;
                }
                currentTrackStopwatch.Restart();
            }
            currentState = CombatLoopState.Exit;
            transitionTime = exit == ExitType.EndOfCombat ? 1 : transitionTimePerId[selectedChorusTransisition].EffectiveStart;

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
            var verse2Queue = RandomizeQueue(BgmId.CombatVerse3, BgmId.CombatVerse4);
            var verse3Queue = RandomizeQueue(BgmId.CombatCoreLoopTransition1, BgmId.CombatCoreLoopTransition2);
            var loop = new LinkedList<BgmId>();
            loop.AddLast(verseQueue.Dequeue());
            loop.AddLast(verse2Queue.Dequeue());
            loop.AddLast(verse3Queue.Dequeue());
            loop.AddLast(BgmId.CombatCoreLoopTransition3);
            loop.AddLast(verseQueue.Dequeue());
            loop.AddLast(verse2Queue.Dequeue());
            loop.AddLast(verse3Queue.Dequeue());
            loop.AddLast(BgmId.CombatCoreLoopTransition3);
            return loop;
        }

        public bool CancelExit()
        {
            return false;
        }
    }
}
