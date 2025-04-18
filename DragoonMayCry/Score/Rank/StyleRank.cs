using DragoonMayCry.Score.Model;

namespace DragoonMayCry.Score.Rank
{
    public class StyleRank
    {

        public StyleRank(StyleType styleType, float threshold, float reductionPerSecond)
        {
            StyleType = styleType;
            Threshold = threshold;
            ReductionPerSecond = reductionPerSecond;
        }
        public StyleType StyleType { get; init; }
        public float Threshold { get; init; }
        public float ReductionPerSecond { get; init; }
    }
}
