using System.Collections.Generic;

namespace DragoonMayCry.Audio.BGM.Editor.Model;

public class StemGroup
{
    public string Name { get; set; } = "";
    public List<Stem> Stems { get; set; } = [];
    public StemGroup? NextStemGroup { get; set; }
}
