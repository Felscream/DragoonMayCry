using System.Collections.Generic;

namespace DragoonMayCry.Score.Action.JobModule
{
    internal class AST : DotJob
    {
        protected override Dictionary<uint, uint> StatusIconIds { get { return statusIconIds; } }

        private readonly Dictionary<uint, uint> statusIconIds = new()
        {
            { 3599, 13213 }, // Combust
            { 3608, 13214 }, // Combust II
            { 16554,  13248}, // Combust III
        };

        private readonly ScoreManager scoreManager;
        public AST(ScoreManager scoreManager) : base()
        {
            this.scoreManager = scoreManager;
        }

        public override float OnAction(uint actionId)
        {
            if (!IsValidDotRefresh(actionId))
            {
                Service.Log.Debug("Failure");
                return 0f;
            }

            return 0.45f * scoreManager.CurrentScoreRank.StyleScoring.Threshold;
        }
    }
}
