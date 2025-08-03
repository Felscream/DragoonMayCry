#region

using DragoonMayCry.Audio.BGM.CustomBgm.Model;
using System.Collections.Generic;
using System.IO;

#endregion

namespace DragoonMayCry.Audio.BGM.CustomBgm
{
    public static class CustomBgmValidator
    {
        public static List<string> GetIntroErrors(CustomBgmProject project)
        {
            return GetStemErrors(project.Intro, "Intro");
        }

        public static List<string> GetCombatStartErrors(CustomBgmProject project)
        {
            return GetStemErrors(project.CombatStart, "Combat Start");
        }

        public static List<string> GetVerseLoopErrors(CustomBgmProject project)
        {
            return GetStemGroupsErrors(project.VerseLoop, "You must have at least one audio file for the verse loop.",
                                       "Verse Loop");
        }

        public static List<string> GetChorusLoopErrors(CustomBgmProject project)
        {
            return GetStemGroupsErrors(project.ChorusLoop, "You must have at least one audio file for the chorus loop.",
                                       "Chorus Loop");
        }

        public static List<string> GetChorusTransitionErrors(CustomBgmProject project)
        {
            return GetStemGroupErrors(project.ChorusTransitions,
                                      "You must have at least one audio file for the chorus transition.",
                                      "Chorus Transition");
        }

        public static List<string> GetDemotionTransitionErrors(CustomBgmProject project)
        {
            return GetStemErrors(project.DemotionTransition,
                                 "Demotion Transition");
        }

        public static List<string> GetCombatEndErrors(CustomBgmProject project)
        {
            return GetStemErrors(project.CombatEnd, "Combat End");
        }

        public static List<string> GetErrors(CustomBgmProject project)
        {
            List<string> errors = [];
            errors.AddRange(GetIntroErrors(project));
            errors.AddRange(GetCombatStartErrors(project));
            errors.AddRange(GetVerseLoopErrors(project));
            errors.AddRange(GetChorusLoopErrors(project));
            errors.AddRange(GetChorusTransitionErrors(project));
            errors.AddRange(GetDemotionTransitionErrors(project));
            errors.AddRange(GetCombatEndErrors(project));
            return errors;
        }

        private static List<string> GetStemErrors(Stem? stem, string prefix)
        {
            var errors = new List<string>();
            if (stem is null)
            {
                errors.Add($"{prefix} : No stem has been set");
            }
            else
            {
                if (!File.Exists(stem.AudioPath))
                {
                    errors.Add($"{prefix} : Audio file  doesn't exist");
                }
                else if (Path.GetExtension(stem.AudioPath) != ".ogg")
                {
                    errors.Add($"{prefix} : Only .ogg files are supported");
                }
            }
            return errors;
        }

        private static List<string> GetStemGroupsErrors(LinkedList<Group> groups, string emptyMessage, string prefix)
        {
            var errors = new List<string>();
            if (groups.Count == 0)
            {
                errors.Add($"{prefix} : {emptyMessage}");
            }
            var index = 1;
            foreach (var stemGroup in groups)
            {
                errors.AddRange(GetStemGroupErrors(stemGroup, "No stem has been set", $"{prefix} - Group {index}"));
                index++;
            }
            return errors;
        }

        private static List<string> GetStemGroupErrors(Group group, string emptyMessage, string prefix)
        {
            List<string> errors = [];
            if (group.Stems.Count == 0)
            {
                errors.Add($"{prefix} : {emptyMessage}");
            }
            for (var i = 0; i < group.Stems.Count; i++)
            {
                errors.AddRange(GetStemErrors(group.Stems[i], $"{prefix} - Stem {i + 1}"));
            }

            return errors;
        }
    }
}
