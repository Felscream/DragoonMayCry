using DragoonMayCry.Audio.Engine;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DragoonMayCry.Audio.BGM.FSM.States.DevilsNeverCry
{
    public class DncIntro : IFsmState
    {
        enum IntroState
        {
            OutOfCombat,
            CombatStart,
            EndCombat,
        }
        public BgmState ID { get { return BgmState.Intro; } }

        private readonly Dictionary<BgmId, BgmTrackData> transitionTimePerId = new()
        {
            { BgmId.Intro, new BgmTrackData(0, 47950) },
        };

        private readonly Dictionary<BgmId, string> bgmPaths = new()
        {
            { BgmId.Intro, DynamicBgmService.GetPathToAudio("DevilsNeverCry\\intro.ogg") },
            { BgmId.CombatEnd, DynamicBgmService.GetPathToAudio("DevilsNeverCry\\end.ogg") },
        };

        private readonly AudioService audioService;
        private readonly Stopwatch currentTrackStopwatch;
        private int transitionTime = 0;
        private readonly Queue<ISampleProvider> samples;
        private IntroState state = IntroState.OutOfCombat;
        private int nextStateTransitionTime = 0;

        public DncIntro(AudioService audioService)
        {
            currentTrackStopwatch = new Stopwatch();

            this.audioService = audioService;
            samples = new Queue<ISampleProvider>();
        }

        public void Enter(bool fromVerse)
        {
            state = IntroState.OutOfCombat;
            var sample = audioService.PlayBgm(BgmId.Intro, 12000);
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
                    TransitionToNextState();
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
            TransitionToNextState();
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

            if (exit == ExitType.ImmediateExit)
            {
                transitionTime = 0;
                TransitionToNextState();
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
                TransitionToNextState();
            }

            if (exit == ExitType.EndOfCombat && state != IntroState.EndCombat)
            {
                state = IntroState.EndCombat;
                transitionTime = 1300;
                nextStateTransitionTime = 6000;
                currentTrackStopwatch.Restart();
                audioService.PlayBgm(BgmId.CombatEnd, 0, 9000, 6000);
            }
            return nextStateTransitionTime;
        }

        private void TransitionToNextState()
        {
            while (samples.TryDequeue(out var sample))
            {
                if (sample is ExposedFadeInOutSampleProvider provider)
                {
                    if (provider.fadeState == ExposedFadeInOutSampleProvider.FadeState.FullVolume)
                    {
                        provider.BeginFadeOut(4500);
                        continue;
                    }
                }
                audioService.RemoveBgmPart(sample);
            }

            currentTrackStopwatch.Reset();
        }

        public bool CancelExit()
        {
            return false;
        }
    }
}
