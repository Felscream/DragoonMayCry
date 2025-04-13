#region

using DragoonMayCry.Audio.Engine;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

#endregion

namespace DragoonMayCry.Audio.BGM.FSM.States.Subhuman
{
    internal class SubVerse : IFsmState
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
                new BgmTrackData(DynamicBgmService.GetPathToAudio("Subhuman\\Verse\\014.ogg"), 0, 1300)
            },
            {
                BgmStemIds.CombatEnter2,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("Subhuman\\Verse\\097.ogg"), 0, 20650)
            },
            {
                BgmStemIds.CombatEnter3,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("Subhuman\\Verse\\022.ogg"), 0, 5100)
            },
            {
                BgmStemIds.CombatVerse1,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("Subhuman\\Verse\\018.ogg"), 0, 20650)
            },
            {
                BgmStemIds.CombatVerse2,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("Subhuman\\Verse\\015.ogg"), 0, 20650)
            },
            {
                BgmStemIds.CombatVerse3,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("Subhuman\\Verse\\115.ogg"), 0, 20650)
            },
            {
                BgmStemIds.CombatVerse4,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("Subhuman\\Verse\\002.ogg"), 0, 20650)
            },
            {
                BgmStemIds.CombatCoreLoopTransition1,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("Subhuman\\Verse\\071.ogg"), 0, 20650)
            },
            {
                BgmStemIds.CombatCoreLoopTransition2,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("Subhuman\\Verse\\099.ogg"), 0, 20650)
            },
            {
                BgmStemIds.CombatCoreLoopTransition3,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("Subhuman\\Verse\\020.ogg"), 0, 20650)
            },
            {
                BgmStemIds.CombatCoreLoopTransition4,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("Subhuman\\Verse\\067.ogg"), 0, 20650)
            },
            {
                BgmStemIds.CombatCoreLoopExit1,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("Subhuman\\Verse\\053.ogg"), 1290, 2650)
            },
            {
                BgmStemIds.CombatCoreLoopExit2,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("Subhuman\\Verse\\065.ogg"), 1290, 2550)
            },
            {
                BgmStemIds.CombatCoreLoopExit3,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("Subhuman\\Verse\\001.ogg"), 1290, 2600)
            },
        };

        private LinkedList<string> combatLoop = new();
        private CombatLoopState currentState;
        private LinkedListNode<string>? currentTrack;
        private int transitionTime;
        public SubVerse(AudioService audioService)
        {
            rand = new Random();
            this.audioService = audioService;
            currentTrackStopwatch = new Stopwatch();
            samples = new Queue<ISampleProvider>();

            combatIntro.AddLast(BgmStemIds.CombatEnter1);
            combatIntro.AddLast(BgmStemIds.CombatEnter2);
            combatIntro.AddLast(BgmStemIds.CombatEnter3);
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
                    selectedTransition = SelectRandom(BgmStemIds.CombatCoreLoopExit1, BgmStemIds.CombatCoreLoopExit2,
                                                      BgmStemIds.CombatCoreLoopExit3);
                    audioService.PlayBgm(selectedTransition);
                    nextTransitionTime = stems[selectedTransition].TransitionStart;
                }
                else if (exit == ExitType.EndOfCombat && currentState != CombatLoopState.Exit)
                {
                    audioService.PlayBgm(BgmStemIds.CombatEnd, 0, 9000, 6000);
                    nextTransitionTime = 6000;
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
            if (samples.Count > 4)
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

        private string SelectRandom(params string[] bgmIds)
        {
            var index = rand.Next(0, bgmIds.Length);
            return bgmIds[index];
        }

        private Queue<string> RandomizeQueue(params string[] bgmIds)
        {
            var list = new List<string>(bgmIds);
            var queue = new Queue<string>();
            while (list.Count > 0)
            {
                var k = rand.Next(list.Count);
                queue.Enqueue(list[k]);
                list.RemoveAt(k);
            }
            return queue;
        }

        private LinkedList<string> GenerateCombatLoop()
        {
            var verseQueue = RandomizeQueue(BgmStemIds.CombatVerse1, BgmStemIds.CombatVerse2);
            var verseQueue2 = RandomizeQueue(BgmStemIds.CombatVerse3, BgmStemIds.CombatVerse4);
            var transitionQueue = RandomizeQueue(BgmStemIds.CombatCoreLoopTransition1,
                                                 BgmStemIds.CombatCoreLoopTransition2,
                                                 BgmStemIds.CombatCoreLoopTransition3);
            var loop = new LinkedList<string>();
            loop.AddLast(verseQueue.Dequeue());
            loop.AddLast(verseQueue2.Dequeue());
            loop.AddLast(transitionQueue.Dequeue());
            loop.AddLast(BgmStemIds.CombatCoreLoopTransition4);
            loop.AddLast(verseQueue.Dequeue());
            loop.AddLast(verseQueue2.Dequeue());
            loop.AddLast(transitionQueue.Dequeue());
            loop.AddLast(BgmStemIds.CombatCoreLoopTransition4);
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
