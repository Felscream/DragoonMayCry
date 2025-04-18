using System.Collections.Generic;

namespace DragoonMayCry.Score.Action.JobModule
{
    internal class AST : DotJob
    {

        private const float CardUseBonus = 0.15f;
        private const float LadyOfTheCrownBonus = 0.042f;

        private readonly Dictionary<uint, uint> actionToStatusIds = new()
        {
            { 3599, 838 },   // Combust
            { 3608, 843 },   // Combust II
            { 16554, 1881 }, // Combust III
            { 17806, 1881 }, // Combust III
        };
        private readonly float dotRefreshBonus = 0.3f;
        private readonly uint ladyOfTheCrownId = 7445;
        private readonly HashSet<uint> playCardIds = [37023, 37024, 37025, 37026, 37027, 37028];
        private readonly ScoreManager scoreManager;

        public AST(ScoreManager scoreManager)
        {
            this.scoreManager = scoreManager;
        }
        protected override Dictionary<uint, uint> ActionToStatusIds => actionToStatusIds;

        public override float OnAction(uint actionId)
        {
            if (IsValidDotRefresh(actionId))
            {
                return dotRefreshBonus * scoreManager.CurrentScoreRank.StyleScoring.Threshold;
            }

            if (playCardIds.Contains(actionId))
            {
                return CardUseBonus * scoreManager.CurrentScoreRank.StyleScoring.Threshold;
            }

            return 0;
        }

        public override float OnActionAppliedOnTarget(uint actionId)
        {
            if (ladyOfTheCrownId == actionId && playerState.CanTargetEnemy())
            {
                return LadyOfTheCrownBonus * scoreManager.CurrentScoreRank.StyleScoring.Threshold;
            }
            return 0;
        }
    }
}
