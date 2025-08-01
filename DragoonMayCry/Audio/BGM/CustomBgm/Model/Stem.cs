#region

using DragoonMayCry.Util;
using System.Collections.Generic;
using System.IO;

#endregion

namespace DragoonMayCry.Audio.BGM.CustomBgm.Model
{
    public class Stem(string audioPath, int transitionTime)
    {
        public string Id { get; } = RandomIdGenerator.GenerateId().ToString();
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

        public List<string> GetErrors()
        {
            var errors = new List<string>();
            if (!File.Exists(AudioPath))
            {
                errors.Add($"{AudioPath} : Audio file  doesn't exist");
            }
            if (Path.GetExtension(AudioPath) != ".ogg")
            {
                errors.Add($"{AudioPath} : Only .ogg files are supported");
            }
            if (TransitionTime is null or < 0)
            {
                errors.Add($"{AudioPath} : Transition time must be set to a positive value");
            }

            return errors;
        }
    }
}
