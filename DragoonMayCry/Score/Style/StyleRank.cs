using System;
using FFXIVClientStructs.FFXIV.Common.Math;

namespace DragoonMayCry.Score.Style
{
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
