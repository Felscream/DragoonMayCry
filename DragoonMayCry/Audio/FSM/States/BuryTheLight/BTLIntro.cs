using Lumina.Data.Parsing;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DragoonMayCry.Audio.FSM.States.BuryTheLight
{
    public class BTLIntro : FsmState
    {
        enum IntroState
        {
            OutOfCombat,
            CombatStart
        }
        public string Name { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public BgmState ID { get { return BgmState.Intro; } }

        private Dictionary<BgmId, BgmTrackData> transitionTimePerId = new Dictionary<BgmId, BgmTrackData> {
            { BgmId.Intro, new BgmTrackData(1600, 38000) },
        };

        public Dictionary<BgmId, string> BgmPaths = new Dictionary<BgmId, string> {
            { BgmId.Intro, BuryTheLightFsm.GetPathToAudio("Intro\\intro.ogg") },
            
        };

        private readonly AudioService audioService;
        private readonly Stopwatch currentTrackStopwatch;
        private int transitionTime = 0;
        private readonly Queue<ISampleProvider> samples;
        private IntroState state = IntroState.OutOfCombat;
        private int nextStateTransitionTime = 0;

        public BTLIntro(AudioService audioService)
        {
            currentTrackStopwatch = new Stopwatch();

            this.audioService = audioService;
            samples = new Queue<ISampleProvider>();
        }

        public void Enter(bool fromVerse)
        {
            state = IntroState.OutOfCombat;
            Service.Log.Debug($"Playing {BgmId.Intro}");
            var sample = audioService.PlayBgm(BgmId.Intro, 20000);
            if(sample != null)
            {
                samples.Enqueue(sample);
            }

            transitionTime = transitionTimePerId[BgmId.Intro].TransitionStart;
            currentTrackStopwatch.Restart();
            Service.Log.Debug($"{transitionTime}");
        }

        public void Update()
        {
            if(!currentTrackStopwatch.IsRunning)
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
            return BgmPaths;
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
            else if(exit == ExitType.Promotion)
            {
                state = IntroState.CombatStart;
                nextStateTransitionTime = 0;
                transitionTime = 1600;
                currentTrackStopwatch.Restart();
            }

            if(exit == ExitType.EndOfCombat)
            {
                state = IntroState.OutOfCombat;
                transitionTime = 1600;
                nextStateTransitionTime = 8000;
                currentTrackStopwatch.Restart();
                audioService.PlayBgm(BgmId.CombatEnd);
            }
            Service.Log.Debug($"{nextStateTransitionTime}");
            return nextStateTransitionTime;
        }

        private void TransitionToNextState(IntroState type)
        {
            Service.Log.Debug("Going to next state");
            while (samples.Count > 0)
            {
                audioService.RemoveBgmPart(samples.Dequeue());
            }
            
            Reset();
        }

        public bool CancelExit()
        {
            return false;
        }
    }
}
