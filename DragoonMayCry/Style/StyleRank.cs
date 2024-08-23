using System;

namespace DragoonMayCry.Style
{

    public enum StyleType { 
        NO_STYLE,
        D,
        C,
        B,
        A,
        S,
        SS,
        SSS
    }

    public class StyleRank {
        public StyleType StyleType { get; init; }
        public string? IconPath { get; init; }
        public string? SfxPath { get; init; }
        public double Threshold { get; init; }
        
        public StyleRank(StyleType styleType, string iconPath, string sfxPath, double threshold) {
            StyleType = styleType;
            IconPath = iconPath;
            SfxPath = sfxPath;
            Threshold = threshold;
        }
    }
}
