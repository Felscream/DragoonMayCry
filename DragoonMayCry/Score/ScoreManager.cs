using Dalamud.Plugin.Services;
using DragoonMayCry.State;
using System;
using System.Diagnostics;
using DragoonMayCry.Score.Action;
using DragoonMayCry.Util;
using DragoonMayCry.Score.Style;
using static DragoonMayCry.Score.Style.StyleRankHandler;

namespace DragoonMayCry.Score
{
    public class ScoreManager : IDisposable

    {
        public class ScoreRank
        {
            public float Score { get; set; }
            public StyleRank Rank { get; set; }

            public ScoreRank(float score, StyleRank styleRank)
            {
                Score = score;
                Rank = styleRank;

            }
        }

        public EventHandler<double>? OnScoring;
        public ScoreRank CurrentScoreRank { get; private set; }
        public ScoreRank? PreviousScoreRank { get; private set; }
        private readonly PlayerState playerState;
        private readonly StyleRankHandler rankHandler;
        private readonly CombatStopwatch combatStopwatch;
        
        private bool isCastingLb;

        private readonly Stopwatch gcdClippingStopwatch;
        private int damageInstancesToCancel;

        public ScoreManager(StyleRankHandler styleRankHandler, ActionTracker actionTracker)
        {
            combatStopwatch = CombatStopwatch.GetInstance();
            gcdClippingStopwatch = new Stopwatch();
            CurrentScoreRank = new(0, styleRankHandler.CurrentRank!.Value);
            ResetScore();

            this.playerState = PlayerState.GetInstance();
            this.playerState.RegisterJobChangeHandler(((sender, ids) => ResetScore()));
            this.playerState.RegisterInstanceChangeHandler(OnInstanceChange!);
            this.playerState.RegisterCombatStateChangeHandler(OnCombatChange!);

            this.rankHandler = styleRankHandler;
            this.rankHandler.StyleRankChange += OnRankChange!;

            actionTracker.OnFlyTextCreation += AddScore;
            actionTracker.OnGcdClip += OnGcdClip;
            actionTracker.OnLimitBreak += OnLimitBreakCast;
            actionTracker.OnLimitBreakCanceled += OnLimitBreakCanceled;

            Service.Framework.Update += UpdateScore;
            Service.ClientState.Logout += ResetScore;
        }

        public void Dispose()
        {
            Service.Framework.Update -= UpdateScore;
            Service.ClientState.Logout -= ResetScore;
        }

        public bool IsActive()
        {
            return playerState.IsInCombat &&
                   ((!playerState.IsInsideInstance &&
                     Plugin.Configuration!.ActiveOutsideInstance)
                    || playerState.IsInsideInstance);
        }

        private void AddScore(object? sender, float val)
        {
            if (damageInstancesToCancel > 0)
            {
                damageInstancesToCancel--;
                Service.Log.Info($"{damageInstancesToCancel} remaining");
                if (CanDisableGcdClippingRestrictions())
                {
                    DisableGcdClippingRestrictions();
                }
                return;
            }
            var points = val;

            CurrentScoreRank.Score += points;
            if (CurrentScoreRank.Rank.StyleType == StyleType.SSS)
            {
                CurrentScoreRank.Score = Math.Min(
                    CurrentScoreRank.Score, CurrentScoreRank.Rank.Threshold);
            }
            OnScoring?.Invoke(this, points);
        }

        public void UpdateScore(IFramework framework)
        {
            if (!IsActive())
            {
                return;
            }

            if (CanDisableGcdClippingRestrictions())
            {
                DisableGcdClippingRestrictions();
            }

            if (isCastingLb)
            {
                CurrentScoreRank.Score +=
                    (float)(framework.UpdateDelta.TotalSeconds * CurrentScoreRank.Rank.ReductionPerSecond * 100);
                CurrentScoreRank.Score = Math.Clamp(
                    CurrentScoreRank.Score, 0, CurrentScoreRank.Rank.Threshold * 1.2f);
            }
            else
            {
                var scoreReduction =
                    (float)(framework.UpdateDelta.TotalSeconds *
                            CurrentScoreRank.Rank.ReductionPerSecond);
                if (AreGcdClippingRestrictionsActive())
                {
                    scoreReduction *= 10;
                }
                CurrentScoreRank.Score -= scoreReduction;
                CurrentScoreRank.Score = Math.Max(CurrentScoreRank.Score, 0);
            }
            
        }

        private bool CanDisableGcdClippingRestrictions()
        {
            return gcdClippingStopwatch.IsRunning &&
                   (gcdClippingStopwatch.ElapsedMilliseconds > Plugin.Configuration!
                        .GcdClippingRestrictionDuration ||
                    damageInstancesToCancel <= 0);
        }

        private void OnInstanceChange(object send, bool value)
        {
            ResetScore();
        }

        private void OnCombatChange(object send, bool enteringCombat)
        {
            if (!enteringCombat)
            {
                PreviousScoreRank = CurrentScoreRank;
            }
            else
            {
                ResetScore();
            }
        }

        private void OnLimitBreakCast(object? sender, ActionTracker.LimitBreakEvent e)
        {
            this.isCastingLb = e.IsCasting;
            if (!isCastingLb)
            {
                CurrentScoreRank.Score = CurrentScoreRank.Rank.Threshold;
            }
        }

        private void OnLimitBreakCanceled(object? sender, EventArgs e)
        {
            isCastingLb = false;
            ResetScore();
        }

        private void OnRankChange(object sender, RankChangeData data)
        {
            PreviousScoreRank = CurrentScoreRank;
            if ((int)CurrentScoreRank.Rank.StyleType < (int)data.NewRank.StyleType)
            {
                CurrentScoreRank.Score = (float)Math.Clamp(CurrentScoreRank.Score %
                    data.NewRank.Threshold, 0, data.NewRank.Threshold * 0.5); ;
            }
            else if (data.IsBlunder)
            {
                if (data.NewRank.StyleType == data.PreviousRank?.StyleType)
                {
                    CurrentScoreRank.Score = 0;
                }
                else
                {
                    CurrentScoreRank.Score = Math.Max(CurrentScoreRank.Score - CurrentScoreRank.Rank.Threshold * 0.1f, 0);
                }
            }
            else
            {
                CurrentScoreRank.Score = data.NewRank.Threshold * 0.8f;
            }

            
            CurrentScoreRank.Rank = data.NewRank;
        }

        private void OnGcdClip(object? send, float clippingTime)
        {
            var newScore = CurrentScoreRank.Score - CurrentScoreRank.Rank.Threshold * 0.3f;
            CurrentScoreRank.Score = Math.Max(newScore, 0);
            gcdClippingStopwatch.Restart();
            damageInstancesToCancel = Plugin.Configuration!.DamageInstancesToCancelOnGcdClip;
            Service.Log.Debug($"Clipping detected {damageInstancesToCancel} instances of damage will be blocked");
        }

        private void DisableGcdClippingRestrictions()
        {
            gcdClippingStopwatch.Reset();
            damageInstancesToCancel = 0;
            Service.Log.Debug($"Clipping restrictions removed");
        }

        private void ResetScore()
        {
            CurrentScoreRank.Score = 0;
        }

        private bool AreGcdClippingRestrictionsActive()
        {
            return gcdClippingStopwatch.IsRunning &&
                   damageInstancesToCancel > 0;
        }
    }
}
