#region

using DragoonMayCry.Audio.Engine;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

#endregion

namespace DragoonMayCry.Audio.BGM.FSM.States.DevilsNeverCry
{
    internal class DncVerse : IFsmState
    {
        private readonly AudioService audioService;

        private readonly LinkedList<string> combatIntro = new();

        private readonly LinkedList<string> combatLoop = new();
        private readonly Stopwatch currentTrackStopwatch;
        private readonly Queue<ISampleProvider> samples;

        private readonly Dictionary<string, BgmTrackData> stems = new()
        {
            {
                BgmStemIds.CombatEnter1,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("DevilsNeverCry\\Verse\\battle_start.ogg"), 0, 26891)
            },
            {
                BgmStemIds.CombatEnter2,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("DevilsNeverCry\\Verse\\10.ogg"), 0, 12835)
            },
            {
                BgmStemIds.CombatCoreLoop,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("DevilsNeverCry\\Verse\\07.ogg"), 0, 76806)
            },
            {
                BgmStemIds.CombatCoreLoopExit1,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("DevilsNeverCry\\Verse\\promote.ogg"), 0, 3750)
            },
        };
        private CombatLoopState currentState;
        private LinkedListNode<string>? currentTrack;
        private int transitionTime;
        public DncVerse(AudioService audioService)
        {
            this.audioService = audioService;
            currentTrackStopwatch = new Stopwatch();
            samples = new Queue<ISampleProvider>();

            combatIntro.AddLast(BgmStemIds.CombatEnter1);
            combatIntro.AddLast(BgmStemIds.CombatEnter2);
            combatLoop.AddLast(BgmStemIds.CombatCoreLoop);
        }
        public BgmState Id => BgmState.CombatLoop;

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

        public int Exit(ExitType exit)
        {
            var nextTransitionTime = stems[BgmStemIds.CombatCoreLoopExit1].TransitionStart;
            var selectedTransition = BgmStemIds.CombatCoreLoopExit1;
            if (currentState == CombatLoopState.Exit)
            {
                nextTransitionTime = (int)Math.Max(nextTransitionTime - currentTrackStopwatch.ElapsedMilliseconds, 0);
            }
            else
            {
                if (exit == ExitType.Promotion)
                {
                    audioService.PlayBgm(selectedTransition);
                    nextTransitionTime = stems[selectedTransition].TransitionStart;
                }
                else if (exit == ExitType.EndOfCombat && currentState != CombatLoopState.Exit)
                {
                    audioService.PlayBgm(BgmStemIds.CombatEnd, 0, 9000, 6000);
                    nextTransitionTime = 8000;
                }
                currentTrackStopwatch.Restart();
            }
            currentState = CombatLoopState.Exit;
            transitionTime = exit == ExitType.EndOfCombat ? 1400
                                 : stems[selectedTransition].EffectiveStart;

            return nextTransitionTime;
        }

        public void Reset()
        {
            LeaveState();
        }

        public bool CancelExit()
        {
            return false;
        }

        public Dictionary<string, string> GetBgmPaths()
        {
            return stems.ToDictionary(entry => entry.Key, entry => entry.Value.AudioPath);
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
            return stems[currentTrack!.Value].TransitionStart;
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

        // loop between verse and riff
        private enum CombatLoopState
        {
            Intro,
            CoreLoop,
            Exit,
        }
    }
}
