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

        public EventHandler OnDemotionStart;
        public EventHandler OnDemotionEnd;
        public EventHandler OnDemotionCanceled;

        private static readonly double InterpolationWeight = 0.09d;
        private readonly ScoreManager scoreManager;
        private readonly StyleRankHandler styleRankHandler;
        private readonly ActionTracker actionTracker;
        private readonly Stopwatch demotionStopwatch;
        private double interpolatedScore = 0;
        private double lastRankChangeTime = 0;
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
            this.actionTracker.OnLimitBreakCanceled += OnLimitBreakCanceled;

            demotionStopwatch = new Stopwatch();
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
            double timeSinceLastRankChange = time - lastRankChangeTime;

            if (Progress > 0.995f)
            {
                if (isCastingLb)
                {
                    if (currentScoreRank.Rank.StyleType != StyleType.SS && currentScoreRank.Rank.StyleType != StyleType.SSS)
                    {
                        styleRankHandler.GoToNextRank(false, false);
                    }

                    return;
                }
                else if(timeSinceLastRankChange > Plugin.Configuration.MinTimeBetweenPromotions)
                {
                    styleRankHandler.GoToNextRank(true, false);
                }
                
            }

            ApplyDemotion(currentScoreRank, timeSinceLastRankChange);
        }

        private void ApplyDemotion(ScoreManager.ScoreRank currentScoreRank,
                                   double timeSinceLastRankChange)
        {
            if (CanStartDemotionTimer(currentScoreRank))
            {
                demotionStopwatch.Restart();
                OnDemotionStart?.Invoke(this, EventArgs.Empty);
                return;
            }
            if (CanCancelDemotion(currentScoreRank))
            {
                demotionStopwatch.Reset();
                OnDemotionCanceled?.Invoke(this, EventArgs.Empty);
                return;
            }

            if (CanApplyDemotion(currentScoreRank, timeSinceLastRankChange))
            {
                styleRankHandler.ReturnToPreviousRank(false);
                demotionStopwatch.Reset();
                OnDemotionEnd?.Invoke(this, EventArgs.Empty);
            }
        }

        private bool CanCancelDemotion(ScoreManager.ScoreRank currentScoreRank)
        {
            return demotionStopwatch.IsRunning && currentScoreRank.Score > Math.Ceiling(currentScoreRank.Rank.Threshold * 0.1);
        }

        private bool CanStartDemotionTimer(ScoreManager.ScoreRank currentScoreRank)
        {
            return currentScoreRank.Score == 0
                   && !demotionStopwatch.IsRunning;
        }

        private bool CanApplyDemotion(ScoreManager.ScoreRank currentScoreRank, double timeSinceLastRankChange)
        {
            return currentScoreRank.Score <= 0 
                   && timeSinceLastRankChange > 4  
                   && demotionStopwatch.ElapsedMilliseconds > Plugin.Configuration!.TimeBeforeDemotion 
                   && !Plugin.Configuration.StyleRankUiConfiguration.TestRankDisplay;
        }

        private void OnRankChange(object? sender, StyleRankHandler.RankChangeData data)
        {
            interpolatedScore = 0;
            lastRankChangeTime = ImGui.GetTime();
        }

        private void OnLimitBreakCast(object? sender, ActionTracker.LimitBreakEvent e)
        {
            this.isCastingLb = e.IsCasting;
            if (demotionStopwatch.IsRunning)
            {
                OnDemotionCanceled?.Invoke(this, EventArgs.Empty);
                demotionStopwatch.Reset();
            }
        }

        private void OnLimitBreakCanceled(object? sender, EventArgs e)
        {
            isCastingLb = false;
        }

        private void OnLimitBreakEffect(object? sender, EventArgs e)
        {
            styleRankHandler.GoToNextRank(true, false, true);
        }
    }
}
