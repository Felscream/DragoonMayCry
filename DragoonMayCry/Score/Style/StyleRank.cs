using System;
using FFXIVClientStructs.FFXIV.Common.Math;

namespace DragoonMayCry.Score.Style
{

    public enum StyleType
    {
        NO_STYLE = 0,
        D = 1,
        C = 2,
        B = 3,
        A = 4,
        S = 5,
        SS = 6,
        SSS = 7,
        DEAD_WEIGHT
    }

    public class StyleRank
    {
        public StyleType StyleType { get; init; }
        public float Threshold { get; init; }
        public float ReductionPerSecond { get; init; }

        public StyleRank(StyleType styleType, float threshold, float reductionPerSecond)
        {
            StyleType = styleType;
            Threshold = threshold;
            ReductionPerSecond = reductionPerSecond;
        }
    }
}
