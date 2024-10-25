using System.Collections.Generic;

namespace DragoonMayCry.Audio.BGM.Editor.Model;

public class DynamicTrack
{
    public string Name { get; set; } = "";
    public Stem? Intro { get; set; }
    public Stem? CombatTransition { get; set; }
    public LinkedList<StemGroup>? Verse { get; set; }
    public LinkedList<StemGroup>? Chorus { get; set; }
    public StemGroup? ChorusTransition { get; set; }
    public Stem? DemotionTransition { get; set; }
    public Stem? EndCombatTransition { get; set; }
}
