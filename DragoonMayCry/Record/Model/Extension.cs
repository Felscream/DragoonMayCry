using System.Collections.Generic;

namespace DragoonMayCry.Record.Model
{
    public class Extension(string name, ExtensionCategory[] categories, Dictionary<ushort, TrackableDuty> instances)
    {
        public string Name { get; private set; } = name;
        public ExtensionCategory[] Categories { get; private set; } = categories;
        public Dictionary<ushort, TrackableDuty> Instances { get; private set; } = instances;
    }
}
