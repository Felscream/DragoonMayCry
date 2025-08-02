#region

using Newtonsoft.Json;
using System;

#endregion

namespace DragoonMayCry.Audio.BGM.CustomBgm.Model
{
    [Serializable]
    [method: JsonConstructor]
    public class Stem(string id, string audioPath, int transitionTime)
    {

        public Stem() : this(Guid.NewGuid().ToString(), string.Empty, 18000) { }

        public string Id { get; } = id;
        public string AudioPath { get; set; } = audioPath;
        public int? TransitionTime { get; set; } = transitionTime;
        protected bool Equals(Stem other)
        {
            return Id == other.Id;
        }
        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Stem)obj);
        }
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
