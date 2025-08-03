#region

using DragoonMayCry.Audio.BGM.CustomBgm.Model;
using DragoonMayCry.Audio.BGM.FSM.States;
using DragoonMayCry.Audio.BGM.FSM.States.Custom;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace DragoonMayCry.Audio.BGM.CustomBgm
{
    public static class CustomBgmFsmStateFactory
    {
        private static readonly CustomBgmManager CustomBgmManager = CustomBgmManager.Instance;

        public static KeyValuePair<long, Dictionary<BgmState, IFsmState>>? GetCustomBgmStates(
            AudioService audioService, long stemId)
        {
            var project = CustomBgmManager.GetProjectById(stemId);
            if (project == null || !CustomBgmManager.IsProjectValid(project))
            {
                return null;
            }

            var combatEndTimings = CreateCombatEndTransitionTimings(project);

            var states = new Dictionary<BgmState, IFsmState>
            {
                { BgmState.Intro, CreateCustomIntro(audioService, project, combatEndTimings) },
                {
                    BgmState.CombatLoop, CreateCustomVerse(audioService, project, combatEndTimings)
                },
                {
                    BgmState.CombatPeak, CreateCustomVerse(audioService, project, combatEndTimings)
                },
            };

            return new KeyValuePair<long, Dictionary<BgmState, IFsmState>>(project.Id, states);
        }

        private static CustomIntro CreateCustomIntro(
            AudioService audioService, CustomBgmProject project, CombatEndTransitionTimings combatEndTimings)
        {
            var introStem = CreateBgmTrackDataFromStem(project.Intro);
            var endOfCombatStem = CreateBgmTrackDataFromStem(project.CombatEnd);

            return new CustomIntro(audioService, introStem, endOfCombatStem, combatEndTimings);
        }

        private static CustomVerse CreateCustomVerse(
            AudioService audioService, CustomBgmProject project, CombatEndTransitionTimings combatEndTimings)
        {
            var combatIntro = CreateBgmTrackDataFromStem(project.CombatStart);
            var combatLoop = CreateBgmTrackDataForLoops(project.VerseLoop);
            var chorusTransitions = CreateBgmTrackDataFromStemGroup(project.ChorusTransitions);
            return new CustomVerse(audioService, combatIntro, combatLoop, chorusTransitions, combatEndTimings);
        }

        private static CustomChorus CreateCustomChorus(
            AudioService audioService, CustomBgmProject project, CombatEndTransitionTimings combatEndTimings)
        {
            var chorusLoop = CreateBgmTrackDataForLoops(project.ChorusLoop);
            var demotion = CreateBgmTrackDataFromStem(project.DemotionTransition);
            var exitTiming = new ExitTimings(combatEndTimings.TransitionTime, combatEndTimings.NextBgmTransitionTime,
                                             combatEndTimings.NextBgmTransitionTime, 0, combatEndTimings.FadeOutDelay,
                                             combatEndTimings.FadeOutDuration);
            return new CustomChorus(audioService, chorusLoop, demotion, exitTiming);
        }

        private static List<List<CustomBgmTrackData>> CreateBgmTrackDataForLoops(ICollection<StemGroup> stemGroups)
        {
            return stemGroups.Select(CreateBgmTrackDataFromStemGroup).ToList();
        }

        private static List<CustomBgmTrackData> CreateBgmTrackDataFromStemGroup(StemGroup stemGroup)
        {
            return stemGroup.Stems.Select(CreateBgmTrackDataFromStem).ToList();
        }

        private static CustomBgmTrackData CreateBgmTrackDataFromStem(Stem stem)
        {
            return new CustomBgmTrackData(stem.Id, new BgmTrackData(stem.AudioPath, 0, stem.TransitionTime));
        }

        private static CombatEndTransitionTimings CreateCombatEndTransitionTimings(CustomBgmProject project)
        {
            return new CombatEndTransitionTimings(1600, project.NextSongTransistionStart, 0, project.EndFadeOutDelay,
                                                  project.EndFadeOutDuration);
        }
    }
}
