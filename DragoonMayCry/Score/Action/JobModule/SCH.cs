using System.Collections.Generic;

namespace DragoonMayCry.Score.Action.JobModule
{
    internal class SCH : DotJob
    {
        protected override Dictionary<uint, uint> StatusIconIds { get { return statusIconIds; } }

        private readonly Dictionary<uint, uint> statusIconIds = new()
        {
            { 17864, 10504 }, // Bio
            { 17865, 10505 }, // Bio II
            { 16540,  12812}, // Biolysis
        };

        private readonly ScoreManager scoreManager;
        public SCH(ScoreManager scoreManager) : base()
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
