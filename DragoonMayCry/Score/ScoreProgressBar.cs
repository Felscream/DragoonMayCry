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
using DragoonMayCry.State;
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

        public EventHandler? OnDemotionStart;
        public EventHandler? OnDemotionEnd;
        public EventHandler? OnDemotionCanceled;

        private static readonly double InterpolationWeight = 0.09d;
        private readonly ScoreManager scoreManager;
        private readonly StyleRankHandler styleRankHandler;
        private readonly ActionTracker actionTracker;
        private readonly Stopwatch demotionStopwatch;
        private double interpolatedScore = 0;
        private float lastPromotionTime = 0;
        private float lastDemotionTime = 0;
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

            PlayerState.GetInstance().RegisterCombatStateChangeHandler(OnCombat);
            demotionStopwatch = new Stopwatch();
        }

        public void Dispose()
        {
            Service.Framework.Update -= UpdateScoreInterpolation;
        }

        public void UpdateScoreInterpolation(IFramework framework)
        {
            if (!Plugin.CanRunDmc())
            {
                return;
            }

            var currentScoreRank = scoreManager.CurrentScoreRank;
            var threshold = currentScoreRank.StyleScoring.Threshold;
            interpolatedScore = double.Lerp(
                interpolatedScore, currentScoreRank.Score,
                InterpolationWeight);
            Progress = (float)Math.Min(interpolatedScore / threshold , 1);
            double time = ImGui.GetTime();

            if (Progress > 0.995f)
            {
                if (isCastingLb)
                {
                    if (currentScoreRank.Rank != StyleType.SS && currentScoreRank.Rank != StyleType.SSS)
                    {
                        styleRankHandler.GoToNextRank(false);
                    }
                    return;
                }
                if(GetTimeSinceLastPromotion() > Plugin.Configuration!.TimeBetweenRankChanges)
                {
                    styleRankHandler.GoToNextRank(true);
                }
            }
            else
            {
                ApplyDemotion(currentScoreRank);
            }
        }

        private void ApplyDemotion(ScoreManager.ScoreRank currentScoreRank)
        {
            var demotionThreshold = GetDemotionThresholdRatio();
            if (CanStartDemotionTimer(demotionThreshold))
            {
                demotionStopwatch.Restart();
                OnDemotionStart?.Invoke(this, EventArgs.Empty);
                return;
            }
            if (CanCancelDemotion(demotionThreshold))
            {
                demotionStopwatch.Reset();
                OnDemotionCanceled?.Invoke(this, EventArgs.Empty);
                return;
            }

            if (CanApplyDemotion(demotionThreshold))
            {
                styleRankHandler.ReturnToPreviousRank(false);
                demotionStopwatch.Reset();
                OnDemotionEnd?.Invoke(this, EventArgs.Empty);
            }
        }

        private bool CanCancelDemotion(float demotionThreshold)
        {
            return demotionStopwatch.IsRunning 
                   && Progress >= demotionThreshold;
        }

        private bool CanStartDemotionTimer(float demotionThreshold)
        {
            return Progress < demotionThreshold
                   && scoreManager.CurrentScoreRank.Rank != StyleType.NoStyle
                   && !demotionStopwatch.IsRunning 
                   && GetTimeSinceLastPromotion() > Plugin.Configuration!.TimeBetweenRankChanges
                   && GetTimeSinceLastDemotion() > Plugin.Configuration!.TimeBetweenDemotions;
        }

        private bool CanApplyDemotion(float demotionThreshold)
        {
            return Progress < demotionThreshold
                   && GetTimeSinceLastDemotion() > Plugin.Configuration!.TimeBetweenDemotions
                   && demotionStopwatch.ElapsedMilliseconds > Plugin.Configuration!.DemotionTimerDuration
                   && !Plugin.Configuration.StyleRankUiConfiguration.TestRankDisplay;
        }

        private void OnRankChange(object? sender, StyleRankHandler.RankChangeData data)
        {
            interpolatedScore = 0;
            demotionStopwatch.Reset();
            var currentStyle = scoreManager.CurrentScoreRank.Rank;
            if (currentStyle < data.NewRank)
            {
                lastPromotionTime = (float)ImGui.GetTime();
            } else if (currentStyle > data.NewRank)
            {
                lastDemotionTime = (float)ImGui.GetTime();
            }
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

        private void OnCombat(object? sender, bool enteredCombat)
        {
            isCastingLb = false;
            interpolatedScore = 0;
            if (demotionStopwatch.IsRunning)
            {
                OnDemotionCanceled?.Invoke(this, EventArgs.Empty);
            }
            demotionStopwatch.Reset();
        }

        private float GetTimeSinceLastPromotion()
        {
            return (float)ImGui.GetTime() - lastPromotionTime;
        }

        private float GetTimeSinceLastDemotion()
        {
            return (float)ImGui.GetTime() - lastDemotionTime;
        }

        private float GetDemotionThresholdRatio()
        {
            return scoreManager.CurrentScoreRank.Score /
                   scoreManager.CurrentScoreRank.StyleScoring.Threshold;
        }
    }
}
