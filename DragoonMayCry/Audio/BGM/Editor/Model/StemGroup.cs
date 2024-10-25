using System.Collections.Generic;

namespace DragoonMayCry.Audio.BGM.Editor.Model;

public class StemGroup
{
    public string Name { get; set; } = "";
    public List<Stem> Tracks { get; set; } = [];
    public StemGroup? NextTrackGroup { get; set; }
}
