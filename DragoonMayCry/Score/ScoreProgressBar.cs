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
        public EventHandler<bool>? OnDemotionApplied;
        public EventHandler? OnDemotionCanceled;
        public EventHandler<bool>? OnPromotion;

        private const float TimeBetweenRankChanges = 1f;
        private const float PromotionSafeguardDuration = 3f;
        private const float TimeBetweenDemotions = 4f;
        private const float DemotionTimerDuration = 3000f;
        private const float InterpolationWeight = 0.09f;
        private readonly ScoreManager scoreManager;
        private readonly StyleRankHandler styleRankHandler;
        private readonly PlayerActionTracker playerActionTracker;
        private readonly Stopwatch demotionApplicationStopwatch;
        private float interpolatedScore = 0;
        private double lastPromotionTime = 0;
        private double lastDemotionTime = 0;
        private float progress;
        private bool isCastingLb;

        public ScoreProgressBar(ScoreManager scoreManager, StyleRankHandler styleRankHandler, PlayerActionTracker playerActionTracker)
        {
            this.scoreManager = scoreManager;
            Service.Framework.Update += UpdateScoreInterpolation;
            this.styleRankHandler = styleRankHandler;
            this.styleRankHandler.StyleRankChange += OnRankChange;
            this.playerActionTracker = playerActionTracker;
            this.playerActionTracker.OnLimitBreak += OnLimitBreakCast;
            this.playerActionTracker.OnLimitBreakCanceled += OnLimitBreakCanceled;

            PlayerState.GetInstance().RegisterCombatStateChangeHandler(OnCombat);
            demotionApplicationStopwatch = new Stopwatch();
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
            interpolatedScore = float.Lerp(
                interpolatedScore, currentScoreRank.Score,
                InterpolationWeight);
            Progress = Math.Clamp(interpolatedScore / threshold, 0,1);

            if (Progress > 0.995f)
            {
                if (isCastingLb)
                {
                    if (currentScoreRank.Rank != StyleType.SS && currentScoreRank.Rank != StyleType.SSS)
                    {
                        OnPromotion?.Invoke(this, false);
                    }
                    return;
                }
                if(GetTimeSinceLastPromotion() > TimeBetweenRankChanges)
                {
                    OnPromotion?.Invoke(this, true);
                }
            }
            else
            {
                HandleDemotion();
            }
        }

        private void HandleDemotion()
        {
            var demotionThreshold = GetDemotionThresholdRatio();
            if (CanCancelDemotion(demotionThreshold))
            {
                CancelDemotion();
                return;
            }

            if (CanStartDemotionTimer(demotionThreshold))
            {
                StartDemotionAlert();
                return;
            }

            if (CanApplyDemotion(demotionThreshold))
            {
                ApplyDemotion();
            }
        }

        private void StartDemotionAlert()
        {
            Service.Log.Debug("Starting demotion");
            demotionApplicationStopwatch.Restart();
            OnDemotionStart?.Invoke(this, EventArgs.Empty);
        }

        private void ApplyDemotion()
        {
            Service.Log.Debug("Applying demotion");
            demotionApplicationStopwatch.Reset();
            OnDemotionApplied?.Invoke(this, false);
        }

        private void CancelDemotion()
        {
            Service.Log.Debug("Cancelling demotion");
            demotionApplicationStopwatch.Reset();
            OnDemotionCanceled?.Invoke(this, EventArgs.Empty);
        }

        private bool CanCancelDemotion(float demotionThreshold)
        {
            return demotionApplicationStopwatch.IsRunning 
                   && (Progress >= demotionThreshold 
                       || GetTimeSinceLastPromotion() < PromotionSafeguardDuration);
        }

        private bool CanStartDemotionTimer(float demotionThreshold)
        {
            return Progress < demotionThreshold
                   && scoreManager.CurrentScoreRank.Rank != StyleType.NoStyle
                   && !demotionApplicationStopwatch.IsRunning 
                   && GetTimeSinceLastPromotion() > TimeBetweenRankChanges
                   && GetTimeSinceLastDemotion() > TimeBetweenDemotions;
        }

        private bool CanApplyDemotion(float demotionThreshold)
        {
            return Progress < demotionThreshold
                   && GetTimeSinceLastDemotion() > TimeBetweenDemotions
                   && demotionApplicationStopwatch.ElapsedMilliseconds > DemotionTimerDuration
                   && !Plugin.Configuration.StyleRankUiConfiguration.TestRankDisplay;
        }

        private void OnRankChange(object? sender, StyleRankHandler.RankChangeData data)
        {
            interpolatedScore = 0;
            if (data.PreviousRank < data.NewRank)
            {
                lastPromotionTime = ImGui.GetTime();
            } else if (data.PreviousRank > data.NewRank)
            {
                lastDemotionTime = ImGui.GetTime();
            }

            if (demotionApplicationStopwatch.IsRunning)
            {
                CancelDemotion();
            }
            
        }

        private void OnLimitBreakCast(object? sender, PlayerActionTracker.LimitBreakEvent e)
        {
            this.isCastingLb = e.IsCasting;
            if (demotionApplicationStopwatch.IsRunning)
            {
                OnDemotionCanceled?.Invoke(this, EventArgs.Empty);
                demotionApplicationStopwatch.Reset();
            }
        }

        private void OnLimitBreakCanceled(object? sender, EventArgs e)
        {
            isCastingLb = false;
        }

        private void OnCombat(object? sender, bool enteredCombat)
        {
            isCastingLb = false;
            interpolatedScore = 0;
            lastDemotionTime = 0;
            lastPromotionTime = 0;
            if (demotionApplicationStopwatch.IsRunning)
            {
                OnDemotionCanceled?.Invoke(this, EventArgs.Empty);
                demotionApplicationStopwatch.Reset();
            }
            
        }

        private double GetTimeSinceLastPromotion()
        {
            if (lastPromotionTime == 0)
            {
                return float.MaxValue;
            }
            return ImGui.GetTime() - lastPromotionTime;
        }

        private double GetTimeSinceLastDemotion()
        {
            if (lastDemotionTime == 0)
            {
                return float.MaxValue;
            }
            return ImGui.GetTime() - lastDemotionTime;
        }

        private float GetDemotionThresholdRatio()
        {
            return scoreManager.CurrentScoreRank.StyleScoring.DemotionThreshold /
                   scoreManager.CurrentScoreRank.StyleScoring.Threshold;
        }
    }
}
