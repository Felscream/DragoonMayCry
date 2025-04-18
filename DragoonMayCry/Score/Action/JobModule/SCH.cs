using System.Collections.Generic;

namespace DragoonMayCry.Score.Action.JobModule
{
    internal class SCH : DotJob
    {

        private readonly Dictionary<uint, uint> actionToStatusIds = new()
        {
            { 17864, 179 },  // Bio
            { 17865, 189 },  // Bio II
            { 16540, 1895 }, // Biolysis
            { 17796, 2039 }, // Biolysis
            { 29233, 3089 }, // Biolysis
        };
        private readonly float dotRefreshBonus = 0.3f;
        private readonly float energyDrainBonus = 0.20f;
        private readonly uint energyDrainId = 167;
        private readonly ScoreManager scoreManager;

        public SCH(ScoreManager scoreManager)
        {
            this.scoreManager = scoreManager;
        }
        protected override Dictionary<uint, uint> ActionToStatusIds => actionToStatusIds;

        public override float OnAction(uint actionId)
        {
            if (actionToStatusIds.ContainsKey(actionId))
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
