#region

using DragoonMayCry.Audio.Engine;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

#endregion

namespace DragoonMayCry.Audio.BGM.FSM.States.BuryTheLight
{
    internal class BTLVerse : IFsmState
    {
        private readonly AudioService audioService;

        private readonly LinkedList<string> combatIntro = new();
        private readonly Stopwatch currentTrackStopwatch;
        private readonly Random rand;
        private readonly Queue<ISampleProvider> samples;

        private readonly Dictionary<string, BgmTrackData> stems = new()
        {
            {
                BgmStemIds.CombatEnter1,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("BuryTheLight\\CombatLoop\\111.ogg"), 0, 3200)
            },
            {
                BgmStemIds.CombatEnter2,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("BuryTheLight\\CombatLoop\\029.ogg"), 0, 12800)
            },
            {
                BgmStemIds.CombatVerse1,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("BuryTheLight\\CombatLoop\\017.ogg"), 0, 25600)
            },
            {
                BgmStemIds.CombatVerse2,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("BuryTheLight\\CombatLoop\\040.ogg"), 0, 25600)
            },
            {
                BgmStemIds.CombatCoreLoop,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("BuryTheLight\\CombatLoop\\coreloop.ogg"), 0, 80000)
            },
            {
                BgmStemIds.CombatCoreLoopTransition1,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("BuryTheLight\\CombatLoop\\029.ogg"), 0, 12800)
            },
            {
                BgmStemIds.CombatCoreLoopTransition2,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("BuryTheLight\\CombatLoop\\064.ogg"), 0, 12800)
            },
            {
                BgmStemIds.CombatCoreLoopExit1,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("BuryTheLight\\CombatLoop\\039.ogg"), 1590, 1600)
            },
            {
                BgmStemIds.CombatCoreLoopExit2,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("BuryTheLight\\CombatLoop\\092.ogg"), 1590, 1600)
            },
            {
                BgmStemIds.CombatCoreLoopExit3,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("BuryTheLight\\CombatLoop\\093.ogg"), 1590, 1600)
            },
        };

        private LinkedList<string> combatLoop = new();
        private CombatLoopState currentState;
        private LinkedListNode<string>? currentTrack;
        private int transitionTime;
        public BTLVerse(AudioService audioService)
        {
            rand = new Random();
            this.audioService = audioService;
            currentTrackStopwatch = new Stopwatch();
            samples = new Queue<ISampleProvider>();

            combatIntro.AddLast(BgmStemIds.CombatEnter1);
            combatIntro.AddLast(BgmStemIds.CombatEnter2);

        }
        public BgmState Id => BgmState.CombatLoop;

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

        public int Exit(ExitType exit)
        {
            transitionTime = exit == ExitType.EndOfCombat ? 1600
                                 : stems[BgmStemIds.CombatCoreLoopExit1].EffectiveStart;
            var nextTransitionTime = stems[BgmStemIds.CombatCoreLoopExit1].TransitionStart;
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

                    audioService.PlayBgm(SelectRandom(BgmStemIds.CombatCoreLoopExit1, BgmStemIds.CombatCoreLoopExit2,
                                                      BgmStemIds.CombatCoreLoopExit3));
                }
                else if (exit == ExitType.EndOfCombat && currentState != CombatLoopState.Exit)
                {
                    audioService.PlayBgm(BgmStemIds.CombatEnd);
                    nextTransitionTime = 8000;
                }
                currentTrackStopwatch.Restart();
            }
            currentState = CombatLoopState.Exit;


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
                        provider.BeginFadeOut(1500);
                        continue;
                    }
                }
                audioService.RemoveBgmPart(sample);
            }
            currentState = CombatLoopState.CoreLoop;
            currentTrackStopwatch.Reset();
        }

        private string SelectRandom(params string[] bgmIds)
        {
            var index = rand.Next(0, bgmIds.Length);
            return bgmIds[index];
        }

        private Queue<string> RandomizeQueue(params string[] bgmIds)
        {
            var k = rand.Next(2);
            var queue = new Queue<string>();
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

        private LinkedList<string> GenerateCombatLoop()
        {
            var verseQueue = RandomizeQueue(BgmStemIds.CombatVerse1, BgmStemIds.CombatVerse2);
            var loopTransitionQueue =
                RandomizeQueue(BgmStemIds.CombatCoreLoopTransition1, BgmStemIds.CombatCoreLoopTransition2);
            var loop = new LinkedList<string>();
            loop.AddLast(verseQueue.Dequeue());
            loop.AddLast(BgmStemIds.CombatCoreLoop);
            loop.AddLast(loopTransitionQueue.Dequeue());
            loop.AddLast(verseQueue.Dequeue());
            loop.AddLast(BgmStemIds.CombatCoreLoop);
            loop.AddLast(loopTransitionQueue.Dequeue());
            return loop;
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
