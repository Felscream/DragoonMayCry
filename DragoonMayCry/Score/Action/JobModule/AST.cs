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
            { 17806,  13248}, // Combust III
        };
        private readonly HashSet<uint> playCardIds = [37023, 37024, 37025, 37026, 37027, 37028];
        private readonly uint ladyOfTheCrownId = 7445;

        private const float CardUseBonus = 0.15f;
        private const float LadyOfTheCrownBonus = 0.042f;
        private readonly float dotRefreshBonus = 0.3f;
        private readonly ScoreManager scoreManager;

        public AST(ScoreManager scoreManager) : base()
        {
            this.scoreManager = scoreManager;
        }

        public override float OnAction(uint actionId)
        {
            if (statusIconIds.ContainsKey(actionId))
            {
                if (!IsValidDotRefresh(actionId))
                {
                    return 0f;
                }

                return dotRefreshBonus * scoreManager.CurrentScoreRank.StyleScoring.Threshold;
            }
            else if (playCardIds.Contains(actionId))
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
