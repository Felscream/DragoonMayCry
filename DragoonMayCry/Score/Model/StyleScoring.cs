namespace DragoonMayCry.Score.Model
{
    public struct StyleScoring
    {
        public float Threshold;
        public int ReductionPerSecond;
        public float DemotionThreshold;
        public float PointCoefficient;

        public StyleScoring(int threshold, int reductionPerSecond, int demotionThreshold, float pointCoefficient)
        {
            Threshold = threshold;
            ReductionPerSecond = reductionPerSecond;
            DemotionThreshold = demotionThreshold;
            PointCoefficient = pointCoefficient;
        }
    }
}
