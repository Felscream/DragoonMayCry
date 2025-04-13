#region

using DragoonMayCry.Audio.Engine;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

#endregion

namespace DragoonMayCry.Audio.BGM.FSM.States.CrimsonCloud
{
    internal class CcVerse : IFsmState
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
                new BgmTrackData(DynamicBgmService.GetPathToAudio("CrimsonCloud\\Verse\\004.ogg"), 0, 2500)
            },
            {
                BgmStemIds.CombatEnter2,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("CrimsonCloud\\Verse\\069.ogg"), 0, 10400)
            },
            {
                BgmStemIds.CombatEnter3,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("CrimsonCloud\\Verse\\036.ogg"), 0, 2590)
            },
            {
                BgmStemIds.CombatVerse1,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("CrimsonCloud\\Verse\\110.ogg"), 0, 20650)
            },
            {
                BgmStemIds.CombatVerse2,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("CrimsonCloud\\Verse\\056.ogg"), 0, 20650)
            },
            {
                BgmStemIds.CombatVerse3,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("CrimsonCloud\\Verse\\007.ogg"), 0, 20600)
            },
            {
                BgmStemIds.CombatVerse4,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("CrimsonCloud\\Verse\\074.ogg"), 0, 20600)
            },
            {
                BgmStemIds.CombatCoreLoopTransition1,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("CrimsonCloud\\Verse\\052.ogg"), 0, 19355)
            },
            {
                BgmStemIds.CombatCoreLoopTransition2,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("CrimsonCloud\\Verse\\060.ogg"), 1300, 21900)
            },
            {
                BgmStemIds.CombatCoreLoopTransition3,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("CrimsonCloud\\Verse\\108.ogg"), 0, 10295)
            },
            {
                BgmStemIds.CombatCoreLoopExit1,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("CrimsonCloud\\Verse\\038.ogg"), 1, 1000)
            },
            {
                BgmStemIds.CombatCoreLoopExit2,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("CrimsonCloud\\Verse\\045.ogg"), 1, 2555)
            },
        };

        private LinkedList<string> combatLoop = new();
        private CombatLoopState currentState;
        private LinkedListNode<string>? currentTrack;
        private string selectedChorusTransisition = "";
        private int transitionTime;
        public CcVerse(AudioService audioService)
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

        public int Exit(ExitType exit)
        {
            var nextTransitionTime = stems[BgmStemIds.CombatCoreLoopExit1].TransitionStart;
            if (selectedChorusTransisition != "")
            {
                nextTransitionTime = stems[selectedChorusTransisition].TransitionStart;
            }

            if (currentState == CombatLoopState.Exit)
            {
                nextTransitionTime = (int)Math.Max(nextTransitionTime - currentTrackStopwatch.ElapsedMilliseconds, 0);
            }
            else
            {
                if (exit == ExitType.Promotion)
                {
                    selectedChorusTransisition =
                        SelectRandom(BgmStemIds.CombatCoreLoopExit1, BgmStemIds.CombatCoreLoopExit2);
                    audioService.PlayBgm(selectedChorusTransisition);
                    nextTransitionTime = stems[selectedChorusTransisition].TransitionStart;
                }
                else if (exit == ExitType.EndOfCombat && currentState != CombatLoopState.Exit)
                {
                    audioService.PlayBgm(BgmStemIds.CombatEnd);
                    nextTransitionTime = 4500;
                }
                currentTrackStopwatch.Restart();
            }
            currentState = CombatLoopState.Exit;
            transitionTime = exit == ExitType.EndOfCombat ? 1
                                 : stems[selectedChorusTransisition].EffectiveStart;

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
            var next = currentTrack!.Next;
            if (next == null)
            {
                next = combatLoop.First!;
            }
            return stems[currentTrack!.Value].TransitionStart
                   - stems[next.Value].EffectiveStart;
        }

        private void LeaveState()
        {
            selectedChorusTransisition = "";
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
            var verse2Queue = RandomizeQueue(BgmStemIds.CombatVerse3, BgmStemIds.CombatVerse4);
            var verse3Queue =
                RandomizeQueue(BgmStemIds.CombatCoreLoopTransition1, BgmStemIds.CombatCoreLoopTransition2);
            var loop = new LinkedList<string>();
            loop.AddLast(verseQueue.Dequeue());
            loop.AddLast(verse2Queue.Dequeue());
            loop.AddLast(verse3Queue.Dequeue());
            loop.AddLast(BgmStemIds.CombatCoreLoopTransition3);
            loop.AddLast(verseQueue.Dequeue());
            loop.AddLast(verse2Queue.Dequeue());
            loop.AddLast(verse3Queue.Dequeue());
            loop.AddLast(BgmStemIds.CombatCoreLoopTransition3);
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
