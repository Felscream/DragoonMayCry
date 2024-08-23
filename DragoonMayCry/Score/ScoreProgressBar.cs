using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using DragoonMayCry.Style;

namespace DragoonMayCry.Score
{
    public class ScoreProgressBar : IDisposable
    {
        public double Progress { get; private set; }
        private static readonly double InterpolationWeight = 0.09d;
        private readonly ScoreManager scoreManager;
        private readonly StyleRankHandler styleRankHandler;
        private double interpolatedScore = 0;

        private double previousThreshold;
        public ScoreProgressBar(ScoreManager scoreManager, StyleRankHandler styleRankHandler)
        {
            this.scoreManager = scoreManager;
            Service.Framework.Update += UpdateScoreInterpolation;
            this.styleRankHandler = styleRankHandler;
            this.styleRankHandler.OnStyleRankChange += OnRankChange;
            var previousStyleRank = styleRankHandler.GetPreviousStyleRank();
            previousThreshold = previousStyleRank == null ? 0 : previousStyleRank.Threshold;
        }

        public void Dispose()
        {
            Service.Framework.Update -= UpdateScoreInterpolation;
        }

        public void UpdateScoreInterpolation(IFramework framework)
        {
            if (!scoreManager.IsActive())
            {
                return;
            }

            var currentScoreRank = scoreManager.CurrentScoreRank;
            var threshold = currentScoreRank.Rank.Threshold;
            var tempScore = currentScoreRank.Score - previousThreshold;
            interpolatedScore = double.Lerp(
                interpolatedScore, tempScore,
                InterpolationWeight);
            Progress = Math.Min(tempScore / (threshold - previousThreshold), 1);
            Service.Log.Debug($"Progress : {Progress}, interpolated : {interpolatedScore}, score ${currentScoreRank.Score}, threshold : {threshold} previous threshold {previousThreshold} style {currentScoreRank.Rank.StyleType}");
            if (Progress >= 0.995f)
            {
                styleRankHandler.GoToNextRank(false);
            }

            if (Progress <= 0.005f)
            {
                styleRankHandler.ReturnToPreviousRank();
            }
        }

        private void OnRankChange(object? sender, StyleRank rank)
        {
            interpolatedScore = 0;
            var previousStyleRank = styleRankHandler.GetPreviousStyleRank();
            previousThreshold = previousStyleRank == null
                                    ? 0
                                    : previousStyleRank.Threshold;

        }
    }
}
