using System.Collections.Generic;

namespace DragoonMayCry.Score.Action.JobModule
{
    internal class SGE : DotJob
    {
        protected override Dictionary<uint, uint> ActionToStatusIds { get { return actionToStatusIds; } }

        private readonly Dictionary<uint, uint> actionToStatusIds = new()
        {
            { 24293, 2614 }, // Eukrasian Dosis
            { 24308, 2615 }, // Eukrasian Dosis II
            { 24314,  2616}, // Eukrasian Dosis III
            { 25421,  2864}, // Eukrasian Dosis III
            { 27823,  3108}, // Eukrasian Dosis III
            { 29257,  3976}, // Eukrasian Dosis III
        };

        private const uint TaurocholeId = 24303;
        private readonly HashSet<uint> druocholeIds = [24296, 27831];
        private readonly HashSet<uint> ixocholeIds = [24299, 27832];

        private readonly ScoreManager scoreManager;
        private readonly float dotRefreshBonus = 0.3f;
        private readonly float singleTargetAddersgallBonus = 0.15f;
        private readonly float multiTargetAddersgallBonus = 0.042f;

        public SGE(ScoreManager scoreManager) : base()
        {
            this.scoreManager = scoreManager;
        }

        public override float OnAction(uint actionId)
        {
            if (!IsValidDotRefresh(actionId))
            {
                return 0;
            }

            return dotRefreshBonus * scoreManager.CurrentScoreRank.StyleScoring.Threshold;
        }

        public override float OnActionAppliedOnTarget(uint actionId)
        {
            if (!playerState.CanTargetEnemy())
            {
                return 0;
            }
            if (actionId == TaurocholeId || druocholeIds.Contains(actionId))
            {
                return singleTargetAddersgallBonus * scoreManager.CurrentScoreRank.StyleScoring.Threshold;
            }
            else if (ixocholeIds.Contains(actionId))
            {
                return multiTargetAddersgallBonus * scoreManager.CurrentScoreRank.StyleScoring.Threshold;
            }

            return 0;
        }
    }
}
