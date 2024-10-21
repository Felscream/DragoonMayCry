using System.Collections.Generic;

namespace DragoonMayCry.Score.Action.JobModule
{
    internal unsafe class WHM : DotJob
    {
        protected override Dictionary<uint, uint> StatusIconIds { get { return statusIconIds; } }

        private readonly Dictionary<uint, uint> statusIconIds = new()
        {
            { 121, 10403 }, // Aero
            { 132, 10409 }, // Aero II
            { 16532, 12635 }, // Dia
            { 17990, 12635 }, // Another Dia ??
        };

        private readonly ScoreManager scoreManager;
        public WHM(ScoreManager scoreManager) : base()
        {
            this.scoreManager = scoreManager;
        }

        public override float OnAction(uint actionId)
        {
            if (!IsValidDotRefresh(actionId))
            {
                return 0;
            }

            return 0.45f * scoreManager.CurrentScoreRank.StyleScoring.Threshold;
        }
    }
}
