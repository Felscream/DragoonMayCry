using System.Collections.Generic;

namespace DragoonMayCry.Audio.BGM.FSM.States.CrimsonCloud
{
    internal class CcChorus : ChorusFsmState
    {

        public CcChorus(AudioService audioService) : base(audioService, 1590,
                                                          new ExitTimings(1, 5200, 5200),
                                                          500)
        {
            CompleteSequence = GenerateChorusLoop();
        }

        protected override Dictionary<string, BgmTrackData> Stems { get; } = new()
        {
            {
                BgmStemIds.ChorusIntro1,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("CrimsonCloud\\Chorus\\086.ogg"), 0, 20605, 20605)
            },
            {
                BgmStemIds.ChorusIntro2,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("CrimsonCloud\\Chorus\\080.ogg"), 0, 10300, 10300)
            },
            {
                BgmStemIds.Riff,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("CrimsonCloud\\Chorus\\103.ogg"), 0, 19350, 20600)
            },
            {
                BgmStemIds.ChorusTransition1,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("CrimsonCloud\\Chorus\\059.ogg"), 0, 20650, 21900)
            },
            {
                BgmStemIds.ChorusTransition2,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("CrimsonCloud\\Chorus\\031.ogg"), 0, 11600, 11600)
            },
            {
                BgmStemIds.ChorusTransition3,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("CrimsonCloud\\Chorus\\082.ogg"), 0, 2600, 2600)
            },
            {
                BgmStemIds.Demotion,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("CrimsonCloud\\Chorus\\016.ogg"), 0, 10300)
            },
        };

        public override void Enter(bool fromVerse)
        {
            CurrentTrack = CompleteSequence.First!;
            CurrentState = PeakState.Loop;
            var sample = AudioService.PlayBgm(CurrentTrack.Value);
            if (sample != null)
            {
                Samples.Enqueue(sample);
            }
            TransitionTime = ComputeNextTransitionTiming();
            CurrentTrackStopwatch.Restart();
            NextPosibleStateTransitionTime = ComputeNextStateTransitionTime();
        }

        protected sealed override LinkedList<string> GenerateChorusLoop()
        {
            var loop = new LinkedList<string>();
            loop.AddLast(BgmStemIds.ChorusIntro1);
            loop.AddLast(BgmStemIds.ChorusIntro2);
            loop.AddLast(BgmStemIds.Riff);
            loop.AddLast(BgmStemIds.ChorusTransition1);
            loop.AddLast(BgmStemIds.ChorusTransition2);
            loop.AddLast(BgmStemIds.ChorusTransition3);
            return loop;
        }
    }
}
