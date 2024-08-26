using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using DragoonMayCry.Score.Action;
using DragoonMayCry.Score.Style;
using ImGuiNET;

namespace DragoonMayCry.Score
{
    public class ScoreProgressBar : IDisposable
    {
        public float Progress
        {
            get
            {
                if (Plugin.Configuration!.StyleRankUiConfiguration.TestRankDisplay)
                {
                    return Plugin.Configuration.StyleRankUiConfiguration
                                 .DebugProgressValue;
                }
                return progress;
            }
            private set
            {
                progress = value;
            }
        }

        public bool DemotionAlertStarted { get; private set; }

        private static readonly double InterpolationWeight = 0.09d;
        private readonly ScoreManager scoreManager;
        private readonly StyleRankHandler styleRankHandler;
        private readonly ActionTracker actionTracker;
        private readonly Stopwatch rankFloorStopwatch;
        private double interpolatedScore = 0;
        private double lastRankChange = 0;
        private float progress;
        private bool isCastingLb;

        public ScoreProgressBar(ScoreManager scoreManager, StyleRankHandler styleRankHandler, ActionTracker actionTracker)
        {
            this.scoreManager = scoreManager;
            Service.Framework.Update += UpdateScoreInterpolation;
            this.styleRankHandler = styleRankHandler;
            this.styleRankHandler.StyleRankChange += OnRankChange;
            this.actionTracker = actionTracker;
            this.actionTracker.OnLimitBreak += OnLimitBreakCast;
            this.actionTracker.OnLimitBreakEffect += OnLimitBreakEffect;

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
            double time = ImGui.GetTime();
            double timeSinceLastRankChange = time - lastRankChange;

            if (Progress > 0.995f)
            {
                if (isCastingLb)
                {
                    if (currentScoreRank.Rank.StyleType != StyleType.SS)
                    {
                        styleRankHandler.GoToNextRank(false, false);
                        return;
                    }
                }
                else if(timeSinceLastRankChange > 1.5)
                {
                    styleRankHandler.GoToNextRank(true, false);
                }
                
            }

            ApplyDemotion(currentScoreRank, timeSinceLastRankChange);
        }

        private void ApplyDemotion(ScoreManager.ScoreRank currentScoreRank,
                                   double timeSinceLastRankChange)
        {
            if (currentScoreRank.Score == 0 && !rankFloorStopwatch.IsRunning)
            {
                rankFloorStopwatch.Restart();
                DemotionAlertStarted = true;
                return;
            }
            if (rankFloorStopwatch.IsRunning && currentScoreRank.Score > 0)
            {
                DemotionAlertStarted = false;
                rankFloorStopwatch.Reset();
                return;
            }

            if (currentScoreRank.Score == 0 && timeSinceLastRankChange > 2.5  && rankFloorStopwatch.ElapsedMilliseconds > Plugin.Configuration!.TimeBeforeDemotion && !Plugin.Configuration.StyleRankUiConfiguration.TestRankDisplay)
            {
                styleRankHandler.ReturnToPreviousRank(false);
                rankFloorStopwatch.Reset();
                DemotionAlertStarted = false;
            }
        }

        private void OnRankChange(object? sender, StyleRankHandler.RankChangeData data)
        {
            interpolatedScore = 0;
            lastRankChange = ImGui.GetTime();
        }

        private void OnLimitBreakCast(object? sender, bool isCastingLb)
        {
            this.isCastingLb = isCastingLb;
        }

        private void OnLimitBreakEffect(object? sender, EventArgs e)
        {
            styleRankHandler.GoToNextRank(true, false, true);
        }
    }
}
