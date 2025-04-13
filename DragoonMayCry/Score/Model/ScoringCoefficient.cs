namespace DragoonMayCry.Score.Model
{
    public struct ScoringCoefficient
    {
        public readonly float ThresholdCoefficient;
        public readonly float ReductionPerSecondCoefficient;

        public ScoringCoefficient(
            float thresholdCoefficient, float reductionPerSecondCoefficient)
        {
            ThresholdCoefficient = thresholdCoefficient;
            ReductionPerSecondCoefficient = reductionPerSecondCoefficient;
        }
    }
}
