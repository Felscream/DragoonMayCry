#region

using System.Collections.Generic;

#endregion

namespace DragoonMayCry.Score.Action.JobModule
{
    internal class WHM : DotJob
    {

        private const float DotRefreshBonus = 0.3f;
        private const float RaptureUseBonus = 0.042f;
        private const float SolaceUseBonus = 0.15f;

        private readonly Dictionary<uint, uint> actionToStatusIds = new()
        {
            { 121, 143 },    // Aero
            { 132, 144 },    // Aero II
            { 16532, 1871 }, // Dia
            { 17990, 2035 }, // Another Dia ??
        };
        private readonly HashSet<uint> afflatusRapture = [16534, 18946];

        private readonly HashSet<uint> afflatusSolace = [16531, 17791];

        private readonly ScoreManager scoreManager;

        public WHM(ScoreManager scoreManager)
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

                return DotRefreshBonus * scoreManager.CurrentScoreRank.StyleScoring.Threshold;
            }


            return 0;
        }

        public override float OnActionAppliedOnTarget(uint actionId)
        {
            if (!DmcPlayerState.CanTargetEnemy())
            {
                return 0;
            }
            if (afflatusSolace.Contains(actionId))
            {
                return SolaceUseBonus * scoreManager.CurrentScoreRank.StyleScoring.Threshold;
            }
            if (afflatusRapture.Contains(actionId))
            {
                return RaptureUseBonus * scoreManager.CurrentScoreRank.StyleScoring.Threshold;
            }
            return 0;
        }
    }
}
