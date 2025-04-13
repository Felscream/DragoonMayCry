using DragoonMayCry.Audio.Engine;
using System;
using System.Collections.Generic;

namespace DragoonMayCry.Audio.BGM.FSM.States.BuryTheLight
{
    internal class BTLChorus : ChorusFsmState
    {
        private const string ChorusIntro = "chorusIntro";
        private const string ChorusIntro2 = "chorusIntro2";
        private const string Riff = "riff";
        private const string Chorus = "chorus";
        private const string Transition1 = "t1";
        private const string Transition2 = "t2";
        private const string Transition3 = "t3";
        private const string Demotion = "demotion";
        

        public override BgmState ID => BgmState.CombatPeak;
        protected override Dictionary<string, BgmTrackData> Stems => bgmStemData;

        private readonly Dictionary<String, BgmTrackData> bgmStemData = new()
        {
            { ChorusIntro, new BgmTrackData(DynamicBgmService.GetPathToAudio("BuryTheLight\\CombatChorus\\025.ogg"),0, 25600, 27200) },
            { ChorusIntro2, new BgmTrackData(DynamicBgmService.GetPathToAudio("BuryTheLight\\CombatChorus\\094.ogg"), 0, 51200, 52800) },
            { Riff, new BgmTrackData(DynamicBgmService.GetPathToAudio("BuryTheLight\\CombatChorus\\062.ogg"), 0, 25600, 27200) },
            { Chorus, new BgmTrackData(DynamicBgmService.GetPathToAudio("BuryTheLight\\CombatChorus\\112.ogg"), 0, 27200, 27200) },
            { Transition1, new BgmTrackData(DynamicBgmService.GetPathToAudio("BuryTheLight\\CombatChorus\\transition1.ogg"), 0, 1600, 3220) },
            { Transition2, new BgmTrackData(DynamicBgmService.GetPathToAudio("BuryTheLight\\CombatChorus\\transition2.ogg"), 0, 1600, 3220) },
            { Transition3, new BgmTrackData(DynamicBgmService.GetPathToAudio("BuryTheLight\\CombatChorus\\transition3.ogg"), 0, 1600, 3220) },
            { Demotion, new BgmTrackData(DynamicBgmService.GetPathToAudio("BuryTheLight\\CombatChorus\\087.ogg"), 0, 25600) },
        };

        /*private readonly Dictionary<BgmId, int> possibleTransitionTimesToNewState = new()
        {
            { BgmId.ChorusIntro1, 27200  },
            { BgmId.ChorusIntro2, 52800 },
            { BgmId.Riff, 27200 },
            { BgmId.Chorus, 27200 },
            { BgmId.ChorusTransition1, 3220 },
            { BgmId.ChorusTransition2,  3220 },
            { BgmId.ChorusTransition3,  3220 },
        };

        private readonly Dictionary<string, string> bgmPaths = new()
        {
            { BgmId.ChorusIntro1, DynamicBgmService.GetPathToAudio("BuryTheLight\\CombatChorus\\025.ogg") },
            { BgmId.ChorusIntro2, DynamicBgmService.GetPathToAudio("BuryTheLight\\CombatChorus\\094.ogg") },
            { BgmId.Riff, DynamicBgmService.GetPathToAudio("BuryTheLight\\CombatChorus\\062.ogg") },
            { BgmId.Chorus, DynamicBgmService.GetPathToAudio("BuryTheLight\\CombatChorus\\112.ogg") },
            { BgmId.Demotion, DynamicBgmService.GetPathToAudio("BuryTheLight\\CombatChorus\\087.ogg") },
            { BgmId.ChorusTransition1, DynamicBgmService.GetPathToAudio("BuryTheLight\\CombatChorus\\transition1.ogg") },
            { BgmId.ChorusTransition2, DynamicBgmService.GetPathToAudio("BuryTheLight\\CombatChorus\\transition2.ogg") },
            { BgmId.ChorusTransition3, DynamicBgmService.GetPathToAudio("BuryTheLight\\CombatChorus\\transition3.ogg") },
        };*/
        
        private readonly LinkedList<string> firstLoop;
        private readonly LinkedList<string> secondLoop;
        private readonly LinkedList<string> thirdLoop;

        public BTLChorus(AudioService audioService) : base(audioService, 1590)
        {
            firstLoop = new LinkedList<string>();
            firstLoop.AddLast(Riff);
            firstLoop.AddLast(Chorus);

            secondLoop = new LinkedList<string>();
            secondLoop.AddLast(ChorusIntro);
            secondLoop.AddLast(ChorusIntro2);
            secondLoop.AddLast(Riff);
            secondLoop.AddLast(Chorus);

            thirdLoop = new LinkedList<string>();
            thirdLoop.AddLast(ChorusIntro);
            thirdLoop.AddLast(ChorusIntro2);
        }

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
                res.Enqueue(Transition1);
                res.Enqueue(Transition2);
            }
            else
            {
                res.Enqueue(Transition2);
                res.Enqueue(Transition1);
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

        public override int Exit(ExitType exit)
        {
            NextStateTransitionTime = 0;
            if (exit == ExitType.EndOfCombat && CurrentState != PeakState.LeavingStateOutOfCombat)
            {
                TransitionTime = 1600;
                NextPosibleStateTransitionTime = 8000;
                NextStateTransitionTime = 8000;
                AudioService.PlayBgm("CombatEnd");
                CurrentState = PeakState.LeavingStateOutOfCombat;
            }
            else if (CurrentState == PeakState.LeavingStateDemotion)
            {
                return NextStateTransitionTime - (int)CurrentTrackStopwatch.Elapsed.TotalMilliseconds;
            }
            else if (exit == ExitType.Demotion)
            {
                ElapsedStopwatchTimeBeforeDemotion += (int)CurrentTrackStopwatch.Elapsed.TotalMilliseconds;
                TransitionTime = NextPosibleStateTransitionTime - ElapsedStopwatchTimeBeforeDemotion;
                NextStateTransitionTime = TransitionTime + bgmStemData[Demotion].TransitionStart;
                CurrentState = PeakState.LeavingStateDemotion;
            }
            CurrentTrackStopwatch.Restart();

            return NextStateTransitionTime;
        }

        private static string SelectRandomChorus()
        {
            var random = new Random();
            return random.Next(2) < 1 ? ChorusIntro : ChorusIntro2;
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

        public override bool CancelExit()
        {
            if (CurrentState != PeakState.LeavingStateDemotion)
            {
                return false;
            }
            CurrentState = PeakState.Loop;
            TransitionTime = ComputeNextTransitionTiming() - ElapsedStopwatchTimeBeforeDemotion;
            NextStateTransitionTime = ComputeNextStateTransitionTime() - ElapsedStopwatchTimeBeforeDemotion;
            return true;
        }
    }
}
