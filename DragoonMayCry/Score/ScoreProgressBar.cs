using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public float Progress
        {
            get
            {
                if (Plugin.Configuration.StyleRankUiConfiguration.TestRankDisplay)
                {
                    return Plugin.Configuration.StyleRankUiConfiguration
                                 .DebugProgressValue;
                }
                return _progress;
            }
            private set
            {
                _progress = value;
            }
        }

        private static readonly double InterpolationWeight = 0.09d;
        private readonly ScoreManager scoreManager;
        private readonly StyleRankHandler styleRankHandler;
        private Stopwatch rankFloorStopwatch;
        private double interpolatedScore = 0;
        private double lastRankChange = 0;
        private float _progress;

        public ScoreProgressBar(ScoreManager scoreManager, StyleRankHandler styleRankHandler)
        {
            this.scoreManager = scoreManager;
            Service.Framework.Update += UpdateScoreInterpolation;
            this.styleRankHandler = styleRankHandler;
            this.styleRankHandler.OnStyleRankChange += OnRankChange;
            rankFloorStopwatch = new Stopwatch();
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
            Progress = (float)Math.Min(interpolatedScore / threshold , 1);
            Service.Log.Information($"Progress {Progress}");
            double time = ImGui.GetTime();
            double timeSinceLastRankChange = time - lastRankChange;

            if (Progress >= 0.995f && timeSinceLastRankChange > 1.5)
            {
                styleRankHandler.GoToNextRank(true, false);
            }

            if (currentScoreRank.Score == 0 && !rankFloorStopwatch.IsRunning)
            {
                rankFloorStopwatch.Restart();
            } else if (currentScoreRank.Score > 0)
            {
                rankFloorStopwatch.Stop();
                rankFloorStopwatch.Reset();
            }
            
            if (currentScoreRank.Score == 0 && (timeSinceLastRankChange > 2.5  && rankFloorStopwatch.ElapsedMilliseconds > 2000))
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
