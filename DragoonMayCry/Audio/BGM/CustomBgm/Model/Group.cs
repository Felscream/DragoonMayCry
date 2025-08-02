#region

using System;
using System.Collections.Generic;

#endregion

namespace DragoonMayCry.Audio.BGM.CustomBgm.Model
{
    [Serializable]
    public class Group
    {
        public List<Stem> Stems { get; private set; } = new(10);


        public void AddStem(Stem stem)
        {
            Stems.Add(stem);
        }

        public void RemoveStem(Stem stem)
        {
            Stems.Remove(stem);
        }

        public void ClearStems()
        {
            Stems.Clear();
        }
    }
}
