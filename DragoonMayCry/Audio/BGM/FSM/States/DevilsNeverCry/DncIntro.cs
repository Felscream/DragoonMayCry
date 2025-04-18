#region

using System.Collections.Generic;

#endregion

namespace DragoonMayCry.Audio.BGM.FSM.States.DevilsNeverCry
{
    internal class DncIntro(AudioService audioService) : IntroFsmState(audioService, 12000, 2500,
                                                                       new CombatEndTransitionTimings(1300, 6000))
    {
        protected override Dictionary<string, BgmTrackData> Stems { get; } = new()
        {
            {
                BgmStemIds.Intro,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("DevilsNeverCry\\intro.ogg"), 0, 47950)
            },
            {
                BgmStemIds.CombatEnd,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("DevilsNeverCry\\end.ogg"), 0, 8000)
            },
        };
    }

}
