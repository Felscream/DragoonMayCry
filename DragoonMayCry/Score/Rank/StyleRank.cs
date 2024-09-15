using System;
using DragoonMayCry.Score.Model;
using FFXIVClientStructs.FFXIV.Common.Math;

namespace DragoonMayCry.Score.Rank
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
