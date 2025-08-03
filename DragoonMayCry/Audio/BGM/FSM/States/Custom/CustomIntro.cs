#region

using System.Collections.Generic;

#endregion

namespace DragoonMayCry.Audio.BGM.FSM.States.Custom
{
    internal class CustomIntro(
        AudioService audioService,
        CustomBgmTrackData introTrackData,
        CustomBgmTrackData endOfCombatTrackData,
        CombatEndTransitionTimings combatEndTimings) : IntroFsmState(audioService, 5000, 2500, combatEndTimings)
    {
        protected override Dictionary<string, BgmTrackData> Stems { get; } = new()
        {
            {
                BgmStemIds.Intro,
                introTrackData.BgmTrackData
            },
            {
                BgmStemIds.CombatEnd,
                endOfCombatTrackData.BgmTrackData
            },
        };
    }
}
