using Dalamud.Plugin.Services;
using DragoonMayCry.Score.Action;
using DragoonMayCry.Score.Model;
using DragoonMayCry.Score.Rank;
using DragoonMayCry.State;
using DragoonMayCry.Util;
using ImGuiNET;
using System;
using System.Diagnostics;

namespace DragoonMayCry.Score
{
    public class ScoreProgressBar : IDisposable, IResettable
    {
        public float Progress { get; private set; }

        public EventHandler<float>? DemotionStart;
        public EventHandler<bool>? DemotionApplied;
        public EventHandler? DemotionCanceled;
        public EventHandler? Promotion;

        private const float TimeBetweenRankChanges = 1f;
        private const float PromotionSafeguardDuration = 3f;
        private const float DemotionTimerDuration = 5000f;
        private const float InterpolationWeight = 0.09f;
        private readonly ScoreManager scoreManager;
        private readonly StyleRankHandler styleRankHandler;
        private readonly PlayerActionTracker playerActionTracker;
        private readonly PlayerState playerState;
        private readonly Stopwatch demotionApplicationStopwatch;
        private float interpolatedScore = 0;
        private double lastPromotionTime = 0;
        private float demotionThreshold = 0;
        private bool isCastingLb;

        public ScoreProgressBar(ScoreManager scoreManager, StyleRankHandler styleRankHandler, PlayerActionTracker playerActionTracker, PlayerState playerState)
        {
            this.scoreManager = scoreManager;
            this.scoreManager.StyleScoringChange += OnStyleScoringChange;
            Service.Framework.Update += UpdateScoreInterpolation;
            this.styleRankHandler = styleRankHandler;
            this.styleRankHandler.StyleRankChange += OnRankChange;
            this.playerActionTracker = playerActionTracker;
            this.playerActionTracker.UsingLimitBreak += OnLimitBreakCast;
            this.playerActionTracker.OnLimitBreakCanceled += OnLimitBreakCanceled;

            this.playerState = playerState;
            playerState.RegisterCombatStateChangeHandler(OnCombat);
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
            Progress = Math.Clamp(interpolatedScore / threshold, 0, 1);

            if (Progress > 0.995f)
            {
                if (isCastingLb)
                {
                    if (currentScoreRank.Rank != StyleType.SS && currentScoreRank.Rank != StyleType.SSS)
                    {
                        Promotion?.Invoke(this, EventArgs.Empty);
                    }
                    return;
                }
                if (GetTimeSinceLastPromotion() > TimeBetweenRankChanges)
                {
                    Promotion?.Invoke(this, EventArgs.Empty);
                }
            }
            else
            {
                HandleDemotion();
            }
        }

        private void HandleDemotion()
        {
            if (CanCancelDemotion())
            {
                CancelDemotion();
                return;
            }

            if (CanStartDemotionTimer())
            {
                StartDemotionAlert();
                return;
            }

            if (CanApplyDemotion())
            {
                ApplyDemotion();
            }
        }

        private void StartDemotionAlert()
        {
            demotionApplicationStopwatch.Restart();
            DemotionStart?.Invoke(this, DemotionTimerDuration);
        }

        private void ApplyDemotion()
        {
            demotionApplicationStopwatch.Reset();
            DemotionApplied?.Invoke(this, false);
        }

        private void CancelDemotion()
        {
            demotionApplicationStopwatch.Reset();
            DemotionCanceled?.Invoke(this, EventArgs.Empty);
        }

        private bool CanCancelDemotion()
        {
            return demotionApplicationStopwatch.IsRunning
                   && (Progress >= demotionThreshold
                       || GetTimeSinceLastPromotion() < PromotionSafeguardDuration);
        }

        private bool CanStartDemotionTimer()
        {
            return this.Progress < demotionThreshold
                   && !playerState.IsIncapacitated()
                   && styleRankHandler.CurrentStyle.Value > StyleType.D
                   && !demotionApplicationStopwatch.IsRunning
                   && GetTimeSinceLastPromotion() > PromotionSafeguardDuration
                   && playerState.CanTargetEnemy();
        }

        private bool CanApplyDemotion()
        {
            return Progress < demotionThreshold
                   && demotionApplicationStopwatch.ElapsedMilliseconds > DemotionTimerDuration;
        }

        private void OnRankChange(object? sender, StyleRankHandler.RankChangeData data)
        {
            interpolatedScore = 0;
            if (data.PreviousRank < data.NewRank)
            {
                lastPromotionTime = ImGui.GetTime();
            }

            if (demotionApplicationStopwatch.IsRunning)
            {
                CancelDemotion();
            }

        }

        private void OnLimitBreakCast(object? sender, PlayerActionTracker.LimitBreakEvent e)
        {
            isCastingLb = e.IsCasting;
            if (!isCastingLb)
            {
                return;
            }
            if (demotionApplicationStopwatch.IsRunning)
            {
                CancelDemotion();
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
            lastPromotionTime = 0;
            if (demotionApplicationStopwatch.IsRunning)
            {
                DemotionCanceled?.Invoke(this, EventArgs.Empty);
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

        private void OnStyleScoringChange(object? sender, StyleScoring styleScoring)
        {
            demotionThreshold = styleScoring.DemotionThreshold / styleScoring.Threshold;
        }

        public void Reset()
        {
            Progress = 0;
            lastPromotionTime = 0;
            isCastingLb = false;
            interpolatedScore = 0;
            if (demotionApplicationStopwatch.IsRunning)
            {
                CancelDemotion();
            }

        }
    }
}
