using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using DragoonMayCry.Style;
using ImGuiNET;

namespace DragoonMayCry.Score
{
    public class ScoreProgressBar : IDisposable
    {
        public double Progress { get; private set; }
        private static readonly double InterpolationWeight = 0.09d;
        private readonly ScoreManager scoreManager;
        private readonly StyleRankHandler styleRankHandler;
        private double interpolatedScore = 0;
        private double lastRankChange = 0;

        public ScoreProgressBar(ScoreManager scoreManager, StyleRankHandler styleRankHandler)
        {
            this.scoreManager = scoreManager;
            Service.Framework.Update += UpdateScoreInterpolation;
            this.styleRankHandler = styleRankHandler;
            this.styleRankHandler.OnStyleRankChange += OnRankChange;
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
            interpolatedScore = double.Lerp(
                interpolatedScore, currentScoreRank.Score,
                InterpolationWeight);
            Progress = Math.Min(interpolatedScore / threshold , 1);

            double time = ImGui.GetTime();
            double timeSinceLastRankChange = time - lastRankChange;
            double scoreToThresholdRatio =
                currentScoreRank.Score / threshold;
            if (Progress >= 0.995f && timeSinceLastRankChange > 3 || scoreToThresholdRatio > 1.1)
            {
                styleRankHandler.GoToNextRank(true, false);
            }

            
            if (currentScoreRank.Score == 0 && (timeSinceLastRankChange > 2.5 ))
            {
                styleRankHandler.ReturnToPreviousRank();
            }
        }

        private void OnRankChange(object? sender, StyleRank rank)
        {
            interpolatedScore = 0;
            lastRankChange = ImGui.GetTime();
        }
    }
}
