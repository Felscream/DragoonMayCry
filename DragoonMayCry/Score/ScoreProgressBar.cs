using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;

namespace DragoonMayCry.Score
{
    public class ScoreProgressBar : IDisposable
    {
        public double Progress { get; private set; }
        private readonly ScoreManager scoreManager;
        private double interpolatedScore;
        private static readonly double INTERPOLATION_WEIGHT = 0.09d;
        private static readonly double THRESHOLD = 60000d;
        public ScoreProgressBar(ScoreManager scoreManager)
        {
            this.scoreManager = scoreManager;
            Service.Framework.Update += UpdateScoreInterpolation;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void UpdateScoreInterpolation(IFramework framework)
        {
            if (!scoreManager.IsActive())
            {
                return;
            }

            var currentScoreRank = scoreManager.CurrentScoreRank;
            interpolatedScore = double.Lerp(
                interpolatedScore, currentScoreRank.Score,
                INTERPOLATION_WEIGHT);
            Progress = Math.Min(interpolatedScore / THRESHOLD, 1);

            if (Progress >= 0.995f)
            {
                interpolatedScore = 0;
                scoreManager.GoToNextRank(false, THRESHOLD);
                
            }
        }
    }
}
