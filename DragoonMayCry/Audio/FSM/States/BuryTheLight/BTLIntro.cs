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

        public string Name { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public BgmState ID { get; set; }

        private Dictionary<BgmId, BgmTrackData> transitionTimePerId = new Dictionary<BgmId, BgmTrackData> {
            { BgmId.Intro, new BgmTrackData(1600, 50293) },
            { BgmId.IntroExit, new BgmTrackData(1588, 3150) }
        };

        public Dictionary<BgmId, string> BgmPaths = new Dictionary<BgmId, string> {
            { BgmId.Intro, BuryTheLightFsm.GetPathToAudio("Intro\\intro.mp3") },
            { BgmId.IntroExit, BuryTheLightFsm.GetPathToAudio("Intro\\111.mp3") }
        };

        private readonly AudioService audioService;
        private readonly Stopwatch currentTrackStopwatch;
        private int transitionTime = 0;
        private readonly Queue<ISampleProvider> samples;
        private bool isLeavingState = false;
        private int nextStateTransitionTime = 0;

        public BTLIntro(BgmState id, AudioService audioService)
        {
            ID = id;
            currentTrackStopwatch = new Stopwatch();

            this.audioService = audioService;
            samples = new Queue<ISampleProvider>();
        }

        public void Enter()
        {
            isLeavingState = false;
            Service.Log.Debug($"Playing {BgmId.Intro}");
            var sample = audioService.PlayBgm(BgmId.Intro);
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
            Service.Log.Debug($"Time {currentTrackStopwatch.Elapsed.TotalMilliseconds} vs {transitionTime}");
            if (currentTrackStopwatch.Elapsed.TotalMilliseconds > transitionTime && transitionTime >= 0)
            {
                if (isLeavingState)
                {
                    TransitionToNextState();
                    return;
                } else
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
            samples.Clear();
            currentTrackStopwatch.Reset();
            isLeavingState = false;
        }

        public Dictionary<BgmId, string> GetBgmPaths()
        {
            return BgmPaths;
        }

        public int Exit()
        {
            if (!currentTrackStopwatch.IsRunning)
            {
                return 0;
            }
            // we are already leaving this state, player transitioned rapidly between multiple ranks
            if (isLeavingState)
            {
                nextStateTransitionTime = (int)Math.Max(nextStateTransitionTime - currentTrackStopwatch.Elapsed.TotalMilliseconds, 0);
            } else
            {
                nextStateTransitionTime = transitionTimePerId[BgmId.IntroExit].TransitionStart;
                audioService.PlayBgm(BgmId.IntroExit);
                transitionTime = transitionTimePerId[BgmId.IntroExit].EffectiveStart;
                currentTrackStopwatch.Restart();
            }
            isLeavingState = true;
            return nextStateTransitionTime;
        }

        private void TransitionToNextState()
        {
            Service.Log.Debug("Going to next state");
            while (samples.Count > 0)
            {
                audioService.RemoveBgmPart(samples.Dequeue());
            }
            Reset();
        }


        public void OnEnter()
        {
        }
    }
}
