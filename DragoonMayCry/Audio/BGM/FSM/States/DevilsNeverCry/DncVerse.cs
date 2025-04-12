using DragoonMayCry.Audio.Engine;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DragoonMayCry.Audio.BGM.FSM.States.DevilsNeverCry
{
    internal class DncVerse : IFsmState
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
            { BgmId.CombatEnter1, new BgmTrackData(0, 26891) },
            { BgmId.CombatEnter2, new BgmTrackData(0, 12835) },
            { BgmId.CombatCoreLoop, new BgmTrackData(0, 76806) },
            { BgmId.CombatCoreLoopExit1, new BgmTrackData(0, 3750) },
        };

        private readonly Dictionary<BgmId, string> bgmPaths = new()
        {
            { BgmId.CombatEnter1, DynamicBgmService.GetPathToAudio("DevilsNeverCry\\Verse\\battle_start.ogg") },
            { BgmId.CombatEnter2, DynamicBgmService.GetPathToAudio("DevilsNeverCry\\Verse\\10.ogg") },
            { BgmId.CombatCoreLoop, DynamicBgmService.GetPathToAudio("DevilsNeverCry\\Verse\\07.ogg") },
            { BgmId.CombatCoreLoopExit1, DynamicBgmService.GetPathToAudio("DevilsNeverCry\\Verse\\promote.ogg") },
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
        public DncVerse(AudioService audioService)
        {
            rand = new Random();
            this.audioService = audioService;
            currentTrackStopwatch = new Stopwatch();
            samples = new Queue<ISampleProvider>();

            combatIntro.AddLast(BgmId.CombatEnter1);
            combatIntro.AddLast(BgmId.CombatEnter2);
            combatLoop.AddLast(BgmId.CombatCoreLoop);
        }

        public void Enter(bool fromVerse)
        {
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
            if (samples.Count > 1)
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
                        provider.BeginFadeOut(1800);
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
                    audioService.PlayBgm(selectedTransition);
                    nextTransitionTime = transitionTimePerId[selectedTransition].TransitionStart;
                }
                else if (exit == ExitType.EndOfCombat && currentState != CombatLoopState.Exit)
                {
                    audioService.PlayBgm(BgmId.CombatEnd, 0, 9000, 6000);
                    nextTransitionTime = 8000;
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

        public bool CancelExit()
        {
            return false;
        }
    }
}
