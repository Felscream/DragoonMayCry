using System.ComponentModel.DataAnnotations;
using DragoonMayCry.Audio.BGM.Editor.Validator;

namespace DragoonMayCry.Audio.BGM.Editor.Model;

public class Stem
{
    [Required(ErrorMessage = "Please enter a track name")]
    [MinLength(1)]
    public string Name { get; set; }

    [Required(ErrorMessage = "Please select an audio file path")]
    [AudioFilePath]
    public string AudioFilePath { get; set; }

    [Required]
    public int TransitionStart { get; set; }
}
