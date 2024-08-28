using Dalamud.Plugin.Services;
using DragoonMayCry.State;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using DragoonMayCry.Data;
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
            public StyleType Rank { get; set; }
            public StyleScoring StyleScoring { get; set; }

            public ScoreRank(float score, StyleType styleRank, StyleScoring styleScoring)
            {
                Score = score;
                Rank = styleRank;
                StyleScoring = styleScoring;
            }
        }

        public struct StyleScoring
        {
            public float Threshold;
            public int ReductionPerSecond;
            public float DemotionThreshold;
            public float PointCoefficient;

            public StyleScoring(int threshold, int reductionPerSecond, int demotionThreshold, float pointCoefficient)
            {
                Threshold = threshold;
                ReductionPerSecond = reductionPerSecond;
                DemotionThreshold = demotionThreshold;
                PointCoefficient = pointCoefficient;
            }
        }

        public EventHandler<double>? OnScoring;
        public ScoreRank CurrentScoreRank { get; private set; }
        private readonly PlayerState playerState;
        private readonly StyleRankHandler rankHandler;

        private const int GcdClippingRestrictionDuration = 6000; //milliseconds
        private const int DamageInstancesToCancelOnGcdClip = 3;
        private bool isCastingLb;
        private Dictionary<StyleType, StyleScoring> jobScoringTable;
        private readonly Stopwatch gcdClippingStopwatch;
        private int damageInstancesToCancel;

        public ScoreManager(StyleRankHandler styleRankHandler, PlayerActionTracker playerActionTracker)
        {
            gcdClippingStopwatch = new Stopwatch();

            var styleRank = styleRankHandler.CurrentStyle!.Value;

            

            playerState = PlayerState.GetInstance();
            playerState.RegisterJobChangeHandler(((sender, ids) => ResetScore()));
            playerState.RegisterInstanceChangeHandler(OnInstanceChange!);
            playerState.RegisterCombatStateChangeHandler(OnCombatChange!);
            playerState.RegisterJobChangeHandler(OnJobChange);

            jobScoringTable = GetJobScoringTable();
            CurrentScoreRank = new(0, styleRank, jobScoringTable[styleRank]);

            this.rankHandler = styleRankHandler;
            this.rankHandler.StyleRankChange += OnRankChange!;

            playerActionTracker.OnFlyTextCreation += AddScore;
            playerActionTracker.OnGcdClip += OnGcdClip;
            playerActionTracker.OnLimitBreak += OnLimitBreakCast;
            playerActionTracker.OnLimitBreakCanceled += OnLimitBreakCanceled;

            Service.Framework.Update += UpdateScore;
            Service.ClientState.Logout += ResetScore;
            ResetScore();
        }

        public void Dispose()
        {
            Service.Framework.Update -= UpdateScore;
            Service.ClientState.Logout -= ResetScore;
        }

        public void UpdateScore(IFramework framework)
        {
            if (!Plugin.CanRunDmc())
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
                    (float)(framework.UpdateDelta.TotalSeconds * CurrentScoreRank.StyleScoring.ReductionPerSecond * 100);
                
            }
            else
            {
                var scoreReduction =
                    (float)(framework.UpdateDelta.TotalSeconds *
                            CurrentScoreRank.StyleScoring.ReductionPerSecond);
                if (AreGcdClippingRestrictionsActive())
                {
                    scoreReduction *= 1.5f;
                }
                CurrentScoreRank.Score -= scoreReduction;
                
            }
            CurrentScoreRank.Score = Math.Clamp(
                CurrentScoreRank.Score, 0, CurrentScoreRank.StyleScoring.Threshold * 1.2f);
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
            var points = val * CurrentScoreRank.StyleScoring.PointCoefficient;

            CurrentScoreRank.Score += points;
            if (CurrentScoreRank.Rank == StyleType.SSS)
            {
                CurrentScoreRank.Score = Math.Min(
                    CurrentScoreRank.Score, CurrentScoreRank.StyleScoring.Threshold);
            }
            OnScoring?.Invoke(this, points);
        }

        private bool CanDisableGcdClippingRestrictions()
        {
            return gcdClippingStopwatch.IsRunning &&
                   (gcdClippingStopwatch.ElapsedMilliseconds > GcdClippingRestrictionDuration 
                    || damageInstancesToCancel <= 0);
        }

        private void OnInstanceChange(object send, bool value)
        {
            ResetScore();
        }

        private void OnCombatChange(object send, bool enteringCombat)
        {
            if(enteringCombat)
            {
                ResetScore();
            }

            DisableGcdClippingRestrictions();
            isCastingLb = false;
        }

        private void OnLimitBreakCast(object? sender, PlayerActionTracker.LimitBreakEvent e)
        {
            this.isCastingLb = e.IsCasting;
            if (!isCastingLb)
            {
                CurrentScoreRank.Score = CurrentScoreRank.StyleScoring.Threshold;
            }
        }

        private void OnLimitBreakCanceled(object? sender, EventArgs e)
        {
            isCastingLb = false;
            ResetScore();
        }

        private void OnRankChange(object sender, RankChangeData data)
        {
            if (!jobScoringTable.ContainsKey(data.NewRank))
            {
                return;
            }

            var nextStyleScoring = jobScoringTable[data.NewRank];
            if ((int)CurrentScoreRank.Rank < (int)data.NewRank)
            {
                CurrentScoreRank.Score = (float)Math.Clamp(CurrentScoreRank.Score %
                                                           nextStyleScoring.Threshold, 0, nextStyleScoring.Threshold * 0.5); ;
            }
            else if (data.IsBlunder)
            {
                CurrentScoreRank.Score = 0f;
            }

            CurrentScoreRank.Rank = data.NewRank;
            CurrentScoreRank.StyleScoring = nextStyleScoring;
        }

        private void OnGcdClip(object? send, float clippingTime)
        {
            var newScore = CurrentScoreRank.Score - CurrentScoreRank.StyleScoring.Threshold * 0.3f;
            CurrentScoreRank.Score = Math.Max(newScore, 0);
            gcdClippingStopwatch.Restart();
            damageInstancesToCancel = DamageInstancesToCancelOnGcdClip;
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
            CurrentScoreRank.StyleScoring = jobScoringTable[StyleType.NoStyle];
            isCastingLb = false;
        }

        private bool AreGcdClippingRestrictionsActive()
        {
            return gcdClippingStopwatch.IsRunning &&
                   damageInstancesToCancel > 0;
        }

        private void OnJobChange(object? sender, JobIds jobId)
        {
            jobScoringTable = GetJobScoringTable(jobId);
            ResetScore();
            DisableGcdClippingRestrictions();
        }

        private Dictionary<StyleType, StyleScoring> GetJobScoringTable()
        {
            var currentJob = JobHelper.GetCurrentJob();
            return GetJobScoringTable(currentJob);
        }

        private Dictionary<StyleType, StyleScoring> GetJobScoringTable(JobIds job)
        {
            if (JobHelper.IsTank(job))
            {
                Service.Log.Debug($"Setting tank scoring table");
                return ScoringTable.TankScoringTable;
            }

            if (JobHelper.IsHealer(job))
            {
                Service.Log.Debug($"Setting healer scoring table");
                return ScoringTable.HealerScoringTable;
            }
            Service.Log.Debug($"Setting Melee scoring table");
            return ScoringTable.MeleeScoringTable;
        }
    }
}
