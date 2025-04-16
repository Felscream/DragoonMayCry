#region

using Dalamud.Game.Gui.FlyText;
using Dalamud.Plugin.Services;
using DragoonMayCry.Configuration;
using DragoonMayCry.Data;
using DragoonMayCry.Score.Action;
using DragoonMayCry.Score.Model;
using DragoonMayCry.Score.Rank;
using DragoonMayCry.Score.ScoringTable;
using DragoonMayCry.State;
using DragoonMayCry.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using static DragoonMayCry.Score.Rank.StyleRankHandler;

#endregion

namespace DragoonMayCry.Score
{
    public class ScoreManager : IDisposable, IResettable
    {

        private const int PointsReductionDuration = 8300; //milliseconds
        private const float PointReductionFactor = 0.8f;
        private const float PointsDecayMultiplierMalus = 4f;
        private static readonly List<FlyTextKind> decayFreezeKind = [FlyTextKind.DamageCritDh];
        private readonly Stopwatch decayFreezeStopwatch;
        private readonly ItemLevelCalculator itemLevelCalculator;
        private readonly PlayerState playerState;
        private readonly Stopwatch pointsReductionStopwatch;
        private readonly StyleRankHandler rankHandler;
        private readonly ScoringTableFactory scoringTableFactory;
        private bool isCastingLb;
        private Dictionary<StyleType, StyleScoring> jobScoringTable;

        private float pointsDecayMultiplier = 1f;
        private float scoreMultiplier = 1f;


        public EventHandler<double>? Scoring;
        public EventHandler<StyleScoring>? StyleScoringChange;

        public ScoreManager(StyleRankHandler styleRankHandler, PlayerActionTracker playerActionTracker)
        {
            pointsReductionStopwatch = new Stopwatch();
            decayFreezeStopwatch = new Stopwatch();

            playerState = PlayerState.GetInstance();
            playerState.RegisterJobChangeHandler((_, _) => ResetScore());
            playerState.RegisterInstanceChangeHandler(OnInstanceChange!);
            playerState.RegisterCombatStateChangeHandler(OnCombatChange!);
            playerState.RegisterJobChangeHandler(OnJobChange);
            playerState.RegisterDeathStateChangeHandler(OnDeath);

            itemLevelCalculator = new ItemLevelCalculator();

            rankHandler = styleRankHandler;
            rankHandler.StyleRankChange += OnRankChange!;

            playerActionTracker.DamageActionUsed += AddScore;
            playerActionTracker.GcdClip += OnGcdClip;
            playerActionTracker.UsingLimitBreak += OnLimitBreakCast;
            playerActionTracker.LimitBreakCanceled += OnLimitBreakCanceled;
            playerActionTracker.GcdDropped += OnGcdDropped;

            Service.Framework.Update += UpdateScore;
            Service.ClientState.Logout += ResetScore;

            scoringTableFactory = new ScoringTableFactory();
            jobScoringTable = ScoringTableFactory.DefaultScoringTable;

            var styleRank = styleRankHandler.CurrentStyle.Value;
            CurrentScoreRank = new ScoreRank(0, styleRank, jobScoringTable[styleRank]);

            ResetScore();
        }
        public ScoreRank CurrentScoreRank { get; private set; }

        public void Dispose()
        {
            Service.Framework.Update -= UpdateScore;
            Service.ClientState.Logout -= ResetScore;
        }

        public void Reset()
        {
            isCastingLb = false;
            ResetScore();
            DisablePointsGainedReduction();
            pointsDecayMultiplier = 1f;
        }

        // Increase score decay overtime if GCD is dropped in Sprout mode, do nothing otherwise
        private void OnGcdDropped(object? sender, EventArgs e)
        {
            var currentJob = playerState.GetCurrentJob();
            if (!Plugin.Configuration!.JobConfiguration.TryGetValue(currentJob, out var jobConfiguration) ||
                jobConfiguration.DifficultyMode != DifficultyMode.Sprout)
            {
                return;
            }

            pointsDecayMultiplier = PointsDecayMultiplierMalus;
        }

        private void UpdateScore(IFramework framework)
        {
            if (!Plugin.CanRunDmc())
            {
                return;
            }

            if (CanDisableGcdClippingRestrictions())
            {
                DisablePointsGainedReduction();
            }

            if (decayFreezeStopwatch.IsRunning && decayFreezeStopwatch.ElapsedMilliseconds > 1200)
            {
                decayFreezeStopwatch.Reset();
                Service.Log.Information("Freezing removed");
            }

            if (isCastingLb)
            {
                CurrentScoreRank.Score =
                    Math.Clamp(CurrentScoreRank.Score
                               + (float)(framework.UpdateDelta.TotalSeconds *
                                         CurrentScoreRank.StyleScoring.ReductionPerSecond * 100),
                               0,
                               CurrentScoreRank.StyleScoring.Threshold * 1.5f);
            }
            else if (!decayFreezeStopwatch.IsRunning)
            {
                var scoreReduction =
                    (float)(framework.UpdateDelta.TotalSeconds *
                            CurrentScoreRank.StyleScoring.ReductionPerSecond * pointsDecayMultiplier);
                if (AreGcdClippingRestrictionsActive())
                {
                    scoreReduction *= 1.5f;
                }

                CurrentScoreRank.Score -= scoreReduction;
            }

            CurrentScoreRank.Score = Math.Clamp(
                CurrentScoreRank.Score, 0, CurrentScoreRank.StyleScoring.Threshold * 1.2f);
        }

        private void AddScore(object? sender, PlayerActionTracker.DamagePayload payload)
        {
            pointsDecayMultiplier = 1f;
            var points = payload.Damage * CurrentScoreRank.StyleScoring.PointCoefficient * scoreMultiplier;
            if (AreGcdClippingRestrictionsActive())
            {
                points *= PointReductionFactor;
            }

            CurrentScoreRank.Score += points;
            if (CurrentScoreRank.Rank == StyleType.SSS)
            {
                CurrentScoreRank.Score = Math.Min(
                    CurrentScoreRank.Score, CurrentScoreRank.StyleScoring.Threshold);
            }

            if (decayFreezeKind.Contains(payload.HitKind))
            {
                decayFreezeStopwatch.Restart();
                Service.Log.Information("Freezing active");
            }

            Scoring?.Invoke(this, points);
        }

        private bool CanDisableGcdClippingRestrictions()
        {
            return pointsReductionStopwatch is
            {
                IsRunning: true, ElapsedMilliseconds: > PointsReductionDuration,
            };
        }

        private void OnInstanceChange(object send, bool value)
        {
            ResetScore();
        }

        private void OnCombatChange(object send, bool enteringCombat)
        {
            pointsDecayMultiplier = 1f;
            DisablePointsGainedReduction();
            isCastingLb = false;
            if (enteringCombat)
            {
                jobScoringTable = GetJobScoringTable();
                scoreMultiplier = GetScoreMultiplier();
                ResetScore();
            }
        }

        private float GetScoreMultiplier()
        {
            var currentJob = playerState.GetCurrentJob();
            var multiplier = 1f;
            if (Plugin.Configuration!.JobConfiguration.TryGetValue(currentJob, out var jobConfiguration))
            {
                multiplier = Math.Max(0.25f, Math.Min(3, jobConfiguration.ScoreMultiplier.Value));
            }

            return multiplier;
        }

        private void OnLimitBreakCast(object? sender, PlayerActionTracker.LimitBreakEvent e)
        {
            isCastingLb = e.IsCasting;
            pointsDecayMultiplier = 1f;
        }

        private void OnLimitBreakCanceled(object? sender, EventArgs e)
        {
            isCastingLb = false;
            if (!playerState.IsIncapacitated())
            {
                ResetScore();
            }
        }

        private void OnRankChange(object sender, RankChangeData data)
        {
            if (!jobScoringTable.TryGetValue(data.NewRank, out var nextStyleScoring))
            {
                return;
            }

            if ((int)CurrentScoreRank.Rank < (int)data.NewRank)
            {
                CurrentScoreRank.Score = Math.Clamp(
                    (CurrentScoreRank.Score - CurrentScoreRank.StyleScoring.Threshold) %
                    nextStyleScoring.Threshold, 0, nextStyleScoring.Threshold * 0.3f);
            }
            else if (data.IsBlunder)
            {
                CurrentScoreRank.Score = 0f;
            }

            CurrentScoreRank.Rank = data.NewRank;
            CurrentScoreRank.StyleScoring = nextStyleScoring;
            StyleScoringChange?.Invoke(this, CurrentScoreRank.StyleScoring);
        }

        private void OnGcdClip(object? send, float clippingTime)
        {
            var newScore = CurrentScoreRank.Score - CurrentScoreRank.StyleScoring.Threshold * 0.3f;
            CurrentScoreRank.Score = Math.Max(newScore, 0);
            pointsReductionStopwatch.Restart();
        }

        private void DisablePointsGainedReduction()
        {
            pointsReductionStopwatch.Reset();
        }

        private void ResetScore(int type, int code)
        {
            ResetScore();
        }

        private void ResetScore()
        {
            CurrentScoreRank = new ScoreRank(0, StyleType.NoStyle, jobScoringTable[StyleType.NoStyle]);
            pointsDecayMultiplier = 1f;
        }

        private bool AreGcdClippingRestrictionsActive()
        {
            return pointsReductionStopwatch.IsRunning;
        }

        private void OnJobChange(object? sender, JobId jobId)
        {
            ResetScore();
            DisablePointsGainedReduction();
        }

        private Dictionary<StyleType, StyleScoring> GetJobScoringTable()
        {
            var ilvl = itemLevelCalculator.CalculateCurrentItemLevel();
            var currentJob = playerState.GetCurrentJob();
            return scoringTableFactory.GetScoringTable(ilvl, currentJob);
        }

        private void OnDeath(object? sender, bool isDead)
        {
            if (isDead)
            {
                DisablePointsGainedReduction();
                ResetScore();
            }
        }

        public class ScoreRank
        {

            public ScoreRank(float score, StyleType styleRank, StyleScoring styleScoring)
            {
                Score = score;
                Rank = styleRank;
                StyleScoring = styleScoring;
            }
            public float Score { get; set; }
            public StyleType Rank { get; set; }
            public StyleScoring StyleScoring { get; set; }
        }
    }
}
