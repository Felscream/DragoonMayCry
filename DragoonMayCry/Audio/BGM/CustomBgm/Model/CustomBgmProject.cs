#region

using DragoonMayCry.Util;
using System.Collections.Generic;

#endregion

namespace DragoonMayCry.Audio.BGM.CustomBgm.Model
{
    public class CustomBgmProject
    {
        public string Name { get; set; } = "";
        public long Id { get; } = RandomIdGenerator.GenerateId();

        public Stem? Intro { get; set; }
        public Stem? CombatStart { get; set; }
        public Stem? CombatEnd { get; set; }
        public LinkedList<Group> VerseLoop { get; set; } = [];
        public LinkedList<Group> ChorusLoop { get; set; } = [];
        public List<Stem> ChorusTransitions { get; set; } = [];
        public List<Stem> DemotionTransitions { get; set; } = [];
        public int EndFadeOutDelay { get; set; }
        public int EndFadeOutDuration { get; set; }
        public int NextSongTransistionStart { get; set; }
    }
}
