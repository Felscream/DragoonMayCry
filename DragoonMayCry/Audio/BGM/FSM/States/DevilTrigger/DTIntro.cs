#region

using System.Collections.Generic;

#endregion

namespace DragoonMayCry.Audio.BGM.FSM.States.DevilTrigger
{
    internal class DtIntro(AudioService audioService) : IntroFsmState(audioService, 4500, 1500,
                                                                      new EndCombatTiming(100, 4500))
    {
        protected override Dictionary<string, BgmTrackData> Stems { get; } = new()
        {
            {
                BgmStemIds.Intro,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("DevilTrigger\\intro.ogg"), 0, 96000)
            },
            {
                BgmStemIds.CombatEnd,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("DevilTrigger\\end.ogg"), 0, 4500)
            },
        };
    }
}
