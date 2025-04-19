#region

using System.Collections.Generic;

#endregion

namespace DragoonMayCry.Audio.BGM.FSM.States.DevilsNeverCry
{
    internal class DncVerse : VerseFsmState
    {
        public DncVerse(AudioService audioService) : base(audioService, 1800,
                                                          new CombatEndTransitionTimings(1400, 10000, 0, 8500, 6000), 1)
        {
            CombatIntro = GenerateCombatIntro();
        }

        protected override Dictionary<string, BgmTrackData> Stems { get; } = new()
        {
            {
                BgmStemIds.CombatEnter1,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("DevilsNeverCry\\Verse\\battle_start.ogg"), 0, 26891)
            },
            {
                BgmStemIds.CombatEnter2,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("DevilsNeverCry\\Verse\\10.ogg"), 0, 12835)
            },
            {
                BgmStemIds.CombatCoreLoop,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("DevilsNeverCry\\Verse\\07.ogg"), 0, 76806)
            },
            {
                BgmStemIds.CombatCoreLoopExit1,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("DevilsNeverCry\\Verse\\promote.ogg"), 0, 3750)
            },
        };

        protected override LinkedList<string> GenerateCombatLoop()
        {
            var list = new LinkedList<string>();
            list.AddLast(BgmStemIds.CombatCoreLoop);
            return list;
        }

        protected sealed override LinkedList<string> GenerateCombatIntro()
        {
            var list = new LinkedList<string>();
            list.AddLast(BgmStemIds.CombatEnter1);
            list.AddLast(BgmStemIds.CombatEnter2);
            return list;
        }

        protected override string SelectChorusTransitionStem()
        {
            return BgmStemIds.CombatCoreLoopExit1;
        }
    }
}
