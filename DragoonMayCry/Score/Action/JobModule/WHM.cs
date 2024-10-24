using System.Collections.Generic;

namespace DragoonMayCry.Score.Action.JobModule
{
    internal unsafe class WHM : DotJob
    {
        protected override Dictionary<uint, uint> ActionToStatusIds { get { return actionToStatusIds; } }

        private readonly Dictionary<uint, uint> actionToStatusIds = new()
        {
            { 121, 143 }, // Aero
            { 132, 144 }, // Aero II
            { 16532, 1871 }, // Dia
            { 17990, 2035 }, // Another Dia ??
        };

        private readonly HashSet<uint> afflatusSolace = [16531, 17791];
        private readonly HashSet<uint> afflatusRapture = [16534, 18946];

        private const float DotRefreshBonus = 0.3f;
        private const float RaptureUseBonus = 0.042f;
        private const float SolaceUseBonus = 0.15f;

        private readonly ScoreManager scoreManager;

        public WHM(ScoreManager scoreManager) : base()
        {
            this.scoreManager = scoreManager;
        }

        public override float OnAction(uint actionId)
        {
            if (actionToStatusIds.ContainsKey(actionId))
            {
                if (!IsValidDotRefresh(actionId))
                {
                    return 0;
                }

                return DotRefreshBonus * scoreManager.CurrentScoreRank.StyleScoring.Threshold;
            }


            return 0;
        }

        public override float OnActionAppliedOnTarget(uint actionId)
        {
            if (!playerState.CanTargetEnemy())
            {
                return 0;
            }
            if (afflatusSolace.Contains(actionId))
            {
                return SolaceUseBonus * scoreManager.CurrentScoreRank.StyleScoring.Threshold;
            }
            else if (afflatusRapture.Contains(actionId))
            {
                return RaptureUseBonus * scoreManager.CurrentScoreRank.StyleScoring.Threshold;
            }
            return 0;
        }
    }
}
