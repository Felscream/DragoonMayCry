using System;

namespace DragoonMayCry.Configuration
{
    public enum DifficultyMode
    {
        Sprout,
        WyrmHunter,
        EstinienMustDie,
    }

    public static class DifficultyModeExtension
    {
        public static string GetLabel(this DifficultyMode difficultyMode)
        {
            switch (difficultyMode)
            {
                case DifficultyMode.Sprout:
                    return "Sprout";
                case DifficultyMode.WyrmHunter:
                    return "Wyrm Hunter";
                case DifficultyMode.EstinienMustDie:
                    return "Estinien Must Die";
                default:
                    throw new InvalidOperationException($"{difficultyMode} does not have a label.");
            }
        }
    }
}
