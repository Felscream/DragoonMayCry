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

namespace DragoonMayCry.Audio.BGM.FSM.States.DevilTrigger
{
    public class DTIntro : IFsmState
    {
        enum IntroState
        {
            OutOfCombat,
            CombatStart
        }
        public string Name { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public BgmState ID { get { return BgmState.Intro; } }

        private readonly Dictionary<BgmId, BgmTrackData> transitionTimePerId = new Dictionary<BgmId, BgmTrackData> {
            { BgmId.Intro, new BgmTrackData(0, 96000) },
        };

        private readonly Dictionary<BgmId, string> bgmPaths = new Dictionary<BgmId, string> {
            { BgmId.Intro, DynamicBgmService.GetPathToAudio("DevilTrigger\\intro.ogg") },
            { BgmId.CombatEnd, DynamicBgmService.GetPathToAudio("DevilTrigger\\end.ogg") },
        };

        private readonly AudioService audioService;
        private readonly Stopwatch currentTrackStopwatch;
        private int transitionTime = 0;
        private readonly Queue<ISampleProvider> samples;
        private IntroState state = IntroState.OutOfCombat;
        private int nextStateTransitionTime = 0;

        public DTIntro(AudioService audioService)
        {
            currentTrackStopwatch = new Stopwatch();

            this.audioService = audioService;
            samples = new Queue<ISampleProvider>();
        }

        public void Enter(bool fromVerse)
        {
            state = IntroState.OutOfCombat;
            Service.Log.Debug($"Playing {BgmId.Intro}");
            var sample = audioService.PlayBgm(BgmId.Intro, 100);
            if (sample != null)
            {
                samples.Enqueue(sample);
            }

            transitionTime = transitionTimePerId[BgmId.Intro].TransitionStart;
            currentTrackStopwatch.Restart();
        }

        public void Update()
        {
            if (!currentTrackStopwatch.IsRunning)
            {
                return;
            }

            if (currentTrackStopwatch.Elapsed.TotalMilliseconds > transitionTime)
            {

                if (state != IntroState.OutOfCombat)
                {
                    TransitionToNextState(state);
                }
                else
                {
                    var sample = audioService.PlayBgm(BgmId.Intro);
                    if (sample != null)
                    {
                        samples.Enqueue(sample);
                    }
                    currentTrackStopwatch.Restart();
                }
            }
        }

        public void Reset()
        {
            while (samples.Count > 0)
            {
                audioService.RemoveBgmPart(samples.Dequeue());
            }
            currentTrackStopwatch.Reset();
        }

        public Dictionary<BgmId, string> GetBgmPaths()
        {
            return bgmPaths;
        }

        public int Exit(ExitType exit)
        {
            if (!currentTrackStopwatch.IsRunning)
            {
                return 0;
            }
            // we are already leaving this state, player transitioned rapidly between multiple ranks
            if (state != IntroState.OutOfCombat)
            {
                nextStateTransitionTime = (int)Math.Max(nextStateTransitionTime - currentTrackStopwatch.Elapsed.TotalMilliseconds, 0);
            }
            else if (exit == ExitType.Promotion)
            {
                state = IntroState.CombatStart;
                nextStateTransitionTime = 0;
                transitionTime = 1600;
                currentTrackStopwatch.Restart();
            }

            if (exit == ExitType.EndOfCombat)
            {
                state = IntroState.OutOfCombat;
                transitionTime = 0;
                nextStateTransitionTime = 4500;
                currentTrackStopwatch.Restart();
                audioService.PlayBgm(BgmId.CombatEnd);
            }
            return nextStateTransitionTime;
        }

        private void TransitionToNextState(IntroState type)
        {
            var sample = (FadeInOutSampleProvider) samples.Dequeue();
            sample.BeginFadeOut(3000);

            currentTrackStopwatch.Reset();
        }

        public bool CancelExit()
        {
            return false;
        }
    }
}
