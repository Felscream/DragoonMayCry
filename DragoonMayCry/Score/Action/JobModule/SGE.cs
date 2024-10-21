using System.Collections.Generic;

namespace DragoonMayCry.Score.Action.JobModule
{
    internal class SGE : DotJob
    {
        protected override Dictionary<uint, uint> StatusIconIds { get { return statusIconIds; } }

        private readonly Dictionary<uint, uint> statusIconIds = new()
        {
            { 24293, 12960 }, // Eukrasian Dosis
            { 24308, 12961 }, // Eukrasian Dosis II
            { 24314,  12962}, // Eukrasian Dosis III
        };

        private readonly ScoreManager scoreManager;
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

            return 0.45f * scoreManager.CurrentScoreRank.StyleScoring.Threshold;
        }
    }
}
