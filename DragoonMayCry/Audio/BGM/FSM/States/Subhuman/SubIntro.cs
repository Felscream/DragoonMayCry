#region

using System.Collections.Generic;

#endregion

namespace DragoonMayCry.Audio.BGM.FSM.States.Subhuman
{
    internal class SubIntro(AudioService audioService) : IntroFsmState(audioService, 4500, 4500,
                                                                       new EndCombatTiming(1300, 6000, 0, 9000, 6000))
    {

        protected override Dictionary<string, BgmTrackData> Stems { get; } = new()
        {
            { BgmStemIds.Intro, new BgmTrackData(DynamicBgmService.GetPathToAudio("Subhuman\\intro.ogg"), 0, 61950) },
            { BgmStemIds.CombatEnd, new BgmTrackData(DynamicBgmService.GetPathToAudio("Subhuman\\end.ogg"), 0, 6000) },
        };
    }
}
