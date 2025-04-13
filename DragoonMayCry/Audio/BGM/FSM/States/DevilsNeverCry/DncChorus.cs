using DragoonMayCry.Audio.Engine;
using NAudio.Wave;
using System.Collections.Generic;
using System.Diagnostics;

namespace DragoonMayCry.Audio.BGM.FSM.States.DevilsNeverCry
{
    internal class DncChorus : ChorusFsmState
    {
        protected override Dictionary<string, BgmTrackData> Stems { get; } = new()
        {
            { BgmStemIds.ChorusIntro1, new BgmTrackData(DynamicBgmService.GetPathToAudio("DevilsNeverCry\\Chorus\\04.ogg"), 0, 23700, 24500) },
            { BgmStemIds.Chorus, new BgmTrackData(DynamicBgmService.GetPathToAudio("DevilsNeverCry\\Chorus\\22.ogg"), 0, 13500, 13800) },
            { BgmStemIds.Chorus2, new BgmTrackData(DynamicBgmService.GetPathToAudio("DevilsNeverCry\\Chorus\\16.ogg"), 0, 25630, 25800) },
            { BgmStemIds.Chorus3, new BgmTrackData(DynamicBgmService.GetPathToAudio("DevilsNeverCry\\Chorus\\26.ogg"), 0, 26800, 26200) },
            { BgmStemIds.ChorusTransition1, new BgmTrackData(DynamicBgmService.GetPathToAudio("DevilsNeverCry\\Chorus\\01.ogg"), 0, 3750, 3000) },
            { BgmStemIds.Demotion, new BgmTrackData(DynamicBgmService.GetPathToAudio("DevilsNeverCry\\Chorus\\05.ogg"), 0, 4510, 3000) },
        };
        public DncChorus(AudioService audioService): base(audioService, 1290, 
                new ExitTimings(1300, 6000, 6000, 0, 9000, 6000),
            1300, 3
            )
        {
            CompleteSequence = GenerateChorusLoop();
        }
    
        protected sealed override LinkedList<string> GenerateChorusLoop()
        {
            LinkedList<string> sequence = new();
            sequence.AddLast(BgmStemIds.ChorusIntro1);
            sequence.AddLast(BgmStemIds.Chorus);
            sequence.AddLast(BgmStemIds.Chorus2);
            sequence.AddLast(BgmStemIds.Chorus3);
            sequence.AddLast(BgmStemIds.ChorusTransition1);
            return sequence;
        }


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
    }
}
