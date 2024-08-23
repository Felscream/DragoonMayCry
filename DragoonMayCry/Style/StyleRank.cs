using System;

namespace DragoonMayCry.Style
{

    public enum StyleType { 
        NO_STYLE = 0,
        D = 1,
        C = 2,
        B = 3,
        A = 4,
        S = 5,
        SS = 6,
        SSS = 7
    }

    public class StyleRank {
        public StyleType StyleType { get; init; }
        public string? IconPath { get; init; }
        public string? SfxPath { get; init; }
        public double Threshold { get; init; }
        public double ReductionPerSecond { get; init; }
        
        public StyleRank(StyleType styleType, string iconPath, string sfxPath, double threshold, double reductionPerSecond) {
            StyleType = styleType;
            IconPath = iconPath;
            SfxPath = sfxPath;
            Threshold = threshold;
            ReductionPerSecond = reductionPerSecond;
        }
    }
}
