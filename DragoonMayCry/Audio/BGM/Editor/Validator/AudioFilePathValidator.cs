using System;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace DragoonMayCry.Audio.BGM.Editor.Validator;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field,
                AllowMultiple = false)]
public class AudioFilePathAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value == null)
        {
            return false;
        }

        var path = (string)value;
        return File.Exists(path);
    }
}
