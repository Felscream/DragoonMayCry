#region

using System.Collections.Generic;

#endregion

namespace DragoonMayCry.Audio.BGM.FSM.States.CrimsonCloud
{
    internal class CCIntro(AudioService audioService) : IntroFsmState(audioService, 4500, 1500,
                                                                      new CombatEndTransitionTimings(100, 4500))
    {
        protected override Dictionary<string, BgmTrackData> Stems { get; } = new()
        {
            {
                BgmStemIds.Intro,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("CrimsonCloud\\intro.ogg"), 0, 85500)
            },
            {
                BgmStemIds.CombatEnd,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("CrimsonCloud\\end.ogg"), 0, 85500)
            },
        };
    }
}
