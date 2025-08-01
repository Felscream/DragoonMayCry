#region

using DragoonMayCry.Audio.BGM.CustomBgm.Model;
using System.Collections.Generic;

#endregion

namespace DragoonMayCry.Audio.BGM.CustomBgm
{
    public static class CustomBgmValidator
    {
        public static List<string> GetIntroErrors(CustomBgmProject project)
        {
            return GetStemErrors(project.Intro, "The intro is required");
        }

        public static List<string> GetCombatStartErrors(CustomBgmProject project)
        {
            return GetStemErrors(project.CombatStart, "The combat start transition is required");
        }

        public static List<string> GetVerseLoopErrors(CustomBgmProject project)
        {
            return GetStemGroupErrors(project.VerseLoop, "You must have at least one audio file for the verse loop.");
        }

        public static List<string> GetChorusLoopErrors(CustomBgmProject project)
        {
            return GetStemGroupErrors(project.ChorusLoop, "You must have at least one audio file for the chorus loop.");
        }

        public static List<string> GetChorusTransitionErrors(CustomBgmProject project)
        {
            return GetStemListErrors(project.ChorusTransitions,
                                     "You must have at least one audio file for the chorus transition.");
        }

        public static List<string> GetDemotionTransitionErrors(CustomBgmProject project)
        {
            return GetStemListErrors(project.DemotionTransitions,
                                     "You must have at least one audio file for the demotion transition.");
        }

        public static List<string> GetCombatEndErrors(CustomBgmProject project)
        {
            return GetStemErrors(project.CombatEnd, "The combat end is required");
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

        private static List<string> GetStemErrors(Stem? stem, string nullMessage)
        {
            var errors = new List<string>();
            if (stem is null)
            {
                errors.Add(nullMessage);
            }
            else
            {
                errors.AddRange(stem.GetErrors());
            }
            return errors;
        }

        private static List<string> GetStemGroupErrors(LinkedList<Group> groups, string emptyMessage)
        {
            var errors = new List<string>();
            if (groups.Count == 0)
            {
                errors.Add(emptyMessage);
            }
            foreach (var stemGroup in groups)
            {
                errors.AddRange(stemGroup.GetErrors());
            }
            return errors;
        }

        private static List<string> GetStemListErrors(List<Stem> stems, string emptyMessage)
        {
            var errors = new List<string>();
            if (stems.Count == 0)
            {
                errors.Add(emptyMessage);
            }
            foreach (var stem in stems)
            {
                errors.AddRange(stem.GetErrors());
            }
            return errors;
        }
    }
}
