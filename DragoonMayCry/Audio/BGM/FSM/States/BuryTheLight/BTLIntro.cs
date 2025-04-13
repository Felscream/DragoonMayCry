#region

using System.Collections.Generic;

#endregion

namespace DragoonMayCry.Audio.BGM.FSM.States.BuryTheLight
{
    internal class BtlIntro(AudioService audioService) : IntroFsmState(audioService, 4500, 1500,
                                                                       new EndCombatTiming(1600, 8000))
    {
        public override BgmState Id => BgmState.Intro;

        protected override Dictionary<string, BgmTrackData> Stems { get; } = new()
        {
            {
                BgmStemIds.Intro,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("BuryTheLight\\intro.ogg"), 1600, 51500)
            },
            {
                BgmStemIds.CombatEnd,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("BuryTheLight\\end.ogg"), 1600, 8000)
            },
        };
    }
}
