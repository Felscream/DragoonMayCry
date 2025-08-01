#region

using System.Collections.Generic;

#endregion

namespace DragoonMayCry.Audio.BGM.CustomBgm.Model
{
    public class Group
    {
        public HashSet<Stem> Stems { get; private set; } = [];

        public List<string> GetErrors()
        {
            List<string> errors = [];
            if (Stems.Count == 0)
            {
                errors.Add("No Stems have been set");
            }
            foreach (var stem in Stems)
            {
                errors.AddRange(stem.GetErrors());
            }

            return errors;
        }

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
