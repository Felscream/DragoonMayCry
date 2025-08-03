#region

using DragoonMayCry.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

#endregion

namespace DragoonMayCry.Audio.BGM.CustomBgm.Model
{
    [Serializable]
    public class CustomBgmProject
    {

        public CustomBgmProject(string name)
        {
            Name = name;
            Id = RandomIdGenerator.GenerateId();
        }

        [JsonConstructor]
        public CustomBgmProject(
            string name, long id, Stem intro, Stem combatStart, Stem combatEnd, LinkedList<Group> verseLoop,
            LinkedList<Group> chorusLoop, Group chorusTransitions, Stem demotionTransition,
            int endFadeOutDelay, int endFadeOutDuration, int nextSongTransistionStart)
        {
            Name = name;
            Id = id;
            Intro = intro;
            CombatStart = combatStart;
            CombatEnd = combatEnd;
            VerseLoop = verseLoop;
            ChorusLoop = chorusLoop;
            ChorusTransitions = chorusTransitions;
            DemotionTransition = demotionTransition;
            EndFadeOutDelay = endFadeOutDelay;
            EndFadeOutDuration = endFadeOutDuration;
            NextSongTransistionStart = nextSongTransistionStart;
        }
        public string Name { get; set; }
        public long Id { get; private set; }

        public Stem Intro { get; set; } = new();
        public Stem CombatStart { get; set; } = new();
        public Stem CombatEnd { get; set; } = new();
        public LinkedList<Group> VerseLoop { get; set; } = [];
        public LinkedList<Group> ChorusLoop { get; set; } = [];
        public Group ChorusTransitions { get; set; } = new();
        public Stem DemotionTransition { get; set; } = new();
        public int EndFadeOutDelay { get; set; }
        public int EndFadeOutDuration { get; set; }
        public int NextSongTransistionStart { get; set; }
    }
}
