using System.Collections.Generic;

namespace DragoonMayCry.Score.Action.JobModule
{
    internal class SCH : DotJob
    {
        protected override Dictionary<uint, uint> StatusIconIds { get { return dotIconIds; } }

        private readonly Dictionary<uint, uint> dotIconIds = new()
        {
            { 17864, 10504 }, // Bio
            { 17865, 10505 }, // Bio II
            { 16540,  12812}, // Biolysis
            { 17796,  12812}, // Biolysis
            { 29233,  12812}, // Biolysis
        };
        private readonly ScoreManager scoreManager;
        private readonly uint energyDrainId = 167;
        private readonly float dotRefreshBonus = 0.3f;
        private readonly float energyDrainBonus = 0.20f;

        public SCH(ScoreManager scoreManager) : base()
        {
            this.scoreManager = scoreManager;
        }

        public override float OnAction(uint actionId)
        {
            if (dotIconIds.ContainsKey(actionId))
            {
                if (!IsValidDotRefresh(actionId))
                {
                    return 0;
                }
                return dotRefreshBonus * scoreManager.CurrentScoreRank.StyleScoring.Threshold;
            }

            return 0;
        }

        public override float OnActionAppliedOnTarget(uint actionId)
        {
            if (actionId == energyDrainId)
            {
                return energyDrainBonus * scoreManager.CurrentScoreRank.StyleScoring.Threshold;
            }
            return 0;
        }
    }
}
