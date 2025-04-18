using DragoonMayCry.Audio.Engine;
using System;
using System.Collections.Generic;

namespace DragoonMayCry.Audio.BGM.FSM.States.BuryTheLight
{
    internal class BtlChorus : ChorusFsmState
    {

        private readonly LinkedList<string> firstLoop;
        private readonly LinkedList<string> secondLoop;
        private readonly LinkedList<string> thirdLoop;

        public BtlChorus(AudioService audioService)
            : base(audioService, 1590,
                   new ExitTimings(1600, 8000, 8000))
        {
            firstLoop = new LinkedList<string>();
            firstLoop.AddLast(BgmStemIds.Riff);
            firstLoop.AddLast(BgmStemIds.Chorus);

            secondLoop = new LinkedList<string>();
            secondLoop.AddLast(BgmStemIds.ChorusIntro1);
            secondLoop.AddLast(BgmStemIds.ChorusIntro2);
            secondLoop.AddLast(BgmStemIds.Riff);
            secondLoop.AddLast(BgmStemIds.Chorus);

            thirdLoop = new LinkedList<string>();
            thirdLoop.AddLast(BgmStemIds.ChorusIntro1);
            thirdLoop.AddLast(BgmStemIds.ChorusIntro2);
        }

        protected override Dictionary<string, BgmTrackData> Stems { get; } = new()
        {
            {
                BgmStemIds.ChorusIntro1,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("BuryTheLight\\CombatChorus\\025.ogg"), 0, 25600,
                                 27200)
            },
            {
                BgmStemIds.ChorusIntro2,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("BuryTheLight\\CombatChorus\\094.ogg"), 0, 51200,
                                 52800)
            },
            {
                BgmStemIds.Riff,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("BuryTheLight\\CombatChorus\\062.ogg"), 0, 25600,
                                 27200)
            },
            {
                BgmStemIds.Chorus,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("BuryTheLight\\CombatChorus\\112.ogg"), 0, 27200,
                                 27200)
            },
            {
                BgmStemIds.ChorusTransition1,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("BuryTheLight\\CombatChorus\\transition1.ogg"), 0,
                                 1600, 3220)
            },
            {
                BgmStemIds.ChorusTransition2,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("BuryTheLight\\CombatChorus\\transition2.ogg"), 0,
                                 1600, 3220)
            },
            {
                BgmStemIds.ChorusTransition3,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("BuryTheLight\\CombatChorus\\transition3.ogg"), 0,
                                 1600, 3220)
            },
            {
                BgmStemIds.Demotion,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("BuryTheLight\\CombatChorus\\087.ogg"), 0, 25600)
            },
        };

        public override void Enter(bool fromVerse)
        {
            var chorus = SelectRandomChorus();
            CurrentTrack = new LinkedListNode<string>(chorus);
            CurrentState = PeakState.VerseIntro;
            var sample = AudioService.PlayBgm(chorus);
            if (sample != null)
            {
                Samples.Enqueue(sample);
            }
            CompleteSequence = GenerateChorusLoop();
            TransitionTime = ComputeNextTransitionTiming();
            CurrentTrackStopwatch.Restart();
            NextPosibleStateTransitionTime = ComputeNextStateTransitionTime();

        }

        public override void Update()
        {
            if (!CurrentTrackStopwatch.IsRunning)
            {
                return;
            }
            if (CurrentTrackStopwatch.ElapsedMilliseconds >= TransitionTime)
            {
                ElapsedStopwatchTimeBeforeDemotion = 0;
                switch (CurrentState)
                {
                    case PeakState.LeavingStateOutOfCombat:
                        LeaveStateOutOfCombat();
                        break;
                    case PeakState.LeavingStateDemotion:
                        StartDemotionTransition();
                        break;
                    case PeakState.CleaningUpDemotion:
                        while (Samples.Count > 1)
                        {
                            AudioService.RemoveBgmPart(Samples.Dequeue());
                        }
                        CurrentTrackStopwatch.Reset();
                        break;
                    default:
                        PlayNextPart();
                        break;
                }
            }
        }

        protected override LinkedList<string> GenerateChorusLoop()
        {
            var transitions = GenerateTransitionOrder();
            var sequence = new LinkedList<string>();
            var node = firstLoop.First!;
            while (node != null)
            {
                sequence.AddLast(node.Value);
                node = node.Next;
            }
            node = secondLoop.First!;
            sequence.AddLast(transitions.Dequeue());
            while (node != null)
            {
                sequence.AddLast(node.Value);
                node = node.Next;
            }
            sequence.AddLast(transitions.Dequeue());
            node = thirdLoop.First!;
            while (node != null)
            {
                sequence.AddLast(node.Value);
                node = node.Next;
            }
            return sequence;

        }

        private Queue<string> GenerateTransitionOrder()
        {
            var random = new Random();
            var val = random.Next(2);
            var res = new Queue<string>();
            if (val < 1)
            {
                res.Enqueue(BgmStemIds.ChorusTransition1);
                res.Enqueue(BgmStemIds.ChorusTransition2);
            }
            else
            {
                res.Enqueue(BgmStemIds.ChorusTransition2);
                res.Enqueue(BgmStemIds.ChorusTransition1);
            }
            return res;
        }

        protected override void LeaveStateOutOfCombat()
        {
            while (Samples.Count > 0)
            {
                AudioService.RemoveBgmPart(Samples.Dequeue());
            }
            CurrentTrackStopwatch.Reset();

        }

        protected override void PlayNextPart()
        {
            if (CurrentState == PeakState.VerseIntro)
            {
                CurrentTrack = CompleteSequence!.First!;
                CurrentState = PeakState.Loop;
            }
            else if (CurrentState == PeakState.Loop)
            {
                // swap to transition at the end of each loop
                CurrentTrack = CurrentTrack!.Next == null ?
                                   CompleteSequence!.First!
                                   : CurrentTrack.Next;
            }


            if (Samples.Count > 4)
            {
                Samples.Dequeue();
            }

            var sample = AudioService.PlayBgm(CurrentTrack!.Value);
            if (sample != null)
            {
                Samples.Enqueue(sample);
            }
            TransitionTime = ComputeNextTransitionTiming();
            CurrentTrackStopwatch.Restart();
            NextPosibleStateTransitionTime = ComputeNextStateTransitionTime();
        }

        private static string SelectRandomChorus()
        {
            var random = new Random();
            return random.Next(2) < 1 ? BgmStemIds.ChorusIntro1 : BgmStemIds.ChorusIntro2;
        }

        public override void Reset()
        {
            while (Samples.TryDequeue(out var sample))
            {
                if (sample is ExposedFadeInOutSampleProvider provider)
                {
                    if (provider.fadeState == ExposedFadeInOutSampleProvider.FadeState.FullVolume)
                    {
                        provider.BeginFadeOut(1500);
                        continue;
                    }
                }
                AudioService.RemoveBgmPart(sample);
            }
            CurrentTrackStopwatch.Reset();
            CurrentState = PeakState.VerseIntro;
        }
    }
}
