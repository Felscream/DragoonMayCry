using DragoonMayCry.Audio.BGM;
using DragoonMayCry.Audio.BGM.FSM;
using DragoonMayCry.Audio.BGM.FSM.States;
using Lumina.Data.Parsing;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.TimeZoneInfo;

namespace DragoonMayCry.Audio.BGM.FSM.States.DevilTrigger
{
    internal class DTVerse : IFsmState
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
            { BgmId.CombatEnter1, new BgmTrackData(0, 3000) },
            { BgmId.CombatEnter2, new BgmTrackData(0, 10500) },
            { BgmId.CombatVerse1, new BgmTrackData(0, 22500) },
            { BgmId.CombatVerse2, new BgmTrackData(0, 22500) },
            { BgmId.CombatCoreLoopTransition1, new BgmTrackData(0, 25500) },
            { BgmId.CombatCoreLoopTransition2, new BgmTrackData(0, 24000) },
            { BgmId.CombatCoreLoopTransition3, new BgmTrackData(0, 24000) },
            { BgmId.CombatCoreLoopExit1, new BgmTrackData(1, 1550) },
            { BgmId.CombatCoreLoopExit2, new BgmTrackData(1, 1550) },
            { BgmId.CombatCoreLoopExit3, new BgmTrackData(1, 1550) },
        };

        private readonly Dictionary<BgmId, string> bgmPaths = new Dictionary<BgmId, string> {
            { BgmId.CombatEnter1, DynamicBgmService.GetPathToAudio("DevilTrigger\\Verse\\028.ogg") },
            { BgmId.CombatEnter2, DynamicBgmService.GetPathToAudio("DevilTrigger\\Verse\\101.ogg") },
            { BgmId.CombatVerse1, DynamicBgmService.GetPathToAudio("DevilTrigger\\Verse\\116.ogg") },
            { BgmId.CombatVerse2, DynamicBgmService.GetPathToAudio("DevilTrigger\\Verse\\021.ogg") },
            { BgmId.CombatCoreLoopTransition1, DynamicBgmService.GetPathToAudio("DevilTrigger\\Verse\\109.ogg") },
            { BgmId.CombatCoreLoopTransition2, DynamicBgmService.GetPathToAudio("DevilTrigger\\Verse\\106.ogg") },
            { BgmId.CombatCoreLoopTransition3, DynamicBgmService.GetPathToAudio("DevilTrigger\\Verse\\066.ogg") },
            { BgmId.CombatCoreLoopExit1, DynamicBgmService.GetPathToAudio("DevilTrigger\\Verse\\070.ogg") },
            { BgmId.CombatCoreLoopExit2, DynamicBgmService.GetPathToAudio("DevilTrigger\\Verse\\075.ogg") },
            { BgmId.CombatCoreLoopExit3, DynamicBgmService.GetPathToAudio("DevilTrigger\\Verse\\085.ogg") },
        };

        private LinkedList<BgmId> combatLoop = new LinkedList<BgmId>();
        private readonly LinkedList<BgmId> combatIntro = new LinkedList<BgmId>();
        private LinkedListNode<BgmId>? currentTrack;
        private readonly AudioService audioService;
        private readonly Stopwatch currentTrackStopwatch;
        private CombatLoopState currentState;
        private int transitionTime = 0;
        private readonly Queue<ISampleProvider> samples;
        private readonly Random rand;
        public DTVerse(AudioService audioService)
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
                sample = audioService.PlayBgm(currentTrack.Value, 1);
                currentState = CombatLoopState.CoreLoop;
            }
            else
            {
                currentTrack = combatIntro.First!;
                sample = audioService.PlayBgm(currentTrack.Value, 1);
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
            var sample = audioService.PlayBgm(currentTrack!.Value, 1);
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
            
            while (samples.Count > 1)
            {
                audioService.RemoveBgmPart(samples.Dequeue());
            }
            if (samples.TryDequeue(out var sample))
            {
                if (sample is FadeInOutSampleProvider)
                {
                    ((FadeInOutSampleProvider)sample).BeginFadeOut(500);
                }
            }
            currentState = CombatLoopState.CoreLoop;
            currentTrackStopwatch.Reset();
        }

        public int Exit(ExitType exit)
        {
            var nextTransitionTime = transitionTimePerId[BgmId.CombatCoreLoopExit1].TransitionStart;
            if (currentState == CombatLoopState.Exit)
            {
                nextTransitionTime = (int)Math.Max(nextTransitionTime - currentTrackStopwatch.ElapsedMilliseconds, 0);
            }
            else
            {
                if (exit == ExitType.Promotion)
                {
                    audioService.PlayBgm(SelectRandom(BgmId.CombatCoreLoopExit1, BgmId.CombatCoreLoopExit2, BgmId.CombatCoreLoopExit3), 1);
                }
                else if (exit == ExitType.EndOfCombat)
                {
                    audioService.PlayBgm(BgmId.CombatEnd);
                    nextTransitionTime = 4500;
                }
                currentTrackStopwatch.Restart();
            }
            currentState = CombatLoopState.Exit;
            transitionTime = exit == ExitType.EndOfCombat ? 1 : transitionTimePerId[BgmId.CombatCoreLoopExit1].EffectiveStart;

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
            int index = rand.Next(0, bgmIds.Length);
            return bgmIds[index];
        }

        private Queue<BgmId> RandomizeQueue(params BgmId[] bgmIds)
        {
            var k = rand.Next(2);
            var queue = new Queue<BgmId>();
            if (k < 1)
            {
                for(int i = 0; i < bgmIds.Length; i++)
                {
                    queue.Enqueue(bgmIds[i]);
                }
            }
            else
            {
                for (int i = bgmIds.Length - 1; i >= 0; i--)
                {
                    queue.Enqueue(bgmIds[i]);
                }
            }
            return queue;
        }

        private LinkedList<BgmId> GenerateCombatLoop()
        {
            var verseQueue = RandomizeQueue(BgmId.CombatVerse1, BgmId.CombatVerse2);
            var loop = new LinkedList<BgmId>();
            loop.AddLast(BgmId.CombatCoreLoopTransition1);
            loop.AddLast(verseQueue.Dequeue());
            loop.AddLast(BgmId.CombatCoreLoopTransition2);
            loop.AddLast(BgmId.CombatCoreLoopTransition3);
            loop.AddLast(BgmId.CombatCoreLoopTransition1);
            loop.AddLast(verseQueue.Dequeue());
            loop.AddLast(BgmId.CombatCoreLoopTransition2);
            loop.AddLast(BgmId.CombatCoreLoopTransition3);
            return loop;
        }

        public bool CancelExit()
        {
            return false;
        }
    }
}
