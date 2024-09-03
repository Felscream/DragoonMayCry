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
using FFXIVClientStructs.FFXIV.Client.Game;
using static DragoonMayCry.Score.Style.StyleRankHandler;
using DragoonMayCry.Score.Table;
using DragoonMayCry.Score.Model;

namespace DragoonMayCry.Score
{
    public unsafe class ScoreManager : IDisposable, IResettable

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

        

        public EventHandler<double>? OnScoring;
        public ScoreRank CurrentScoreRank { get; private set; }
        private readonly PlayerState playerState;
        private readonly StyleRankHandler rankHandler;
        private readonly ItemLevelCalculator itemLevelCalculator;
        private readonly ScoringTableFactory scoringTableFactory;

        private const int PointsReductionDuration = 8300; //milliseconds
        private const float PointReductionFactor = 0.8f;
        private bool isCastingLb;
        private Dictionary<StyleType, StyleScoring> jobScoringTable;
        private readonly Stopwatch pointsReductionStopwatch;

        public ScoreManager(StyleRankHandler styleRankHandler, PlayerActionTracker playerActionTracker)
        {
            pointsReductionStopwatch = new Stopwatch();

            playerState = PlayerState.GetInstance();
            playerState.RegisterJobChangeHandler(((sender, ids) => ResetScore()));
            playerState.RegisterInstanceChangeHandler(OnInstanceChange!);
            playerState.RegisterCombatStateChangeHandler(OnCombatChange!);
            playerState.RegisterJobChangeHandler(OnJobChange);
            playerState.RegisterDeathStateChangeHandler(OnDeath);

            itemLevelCalculator = new ItemLevelCalculator();

            this.rankHandler = styleRankHandler;
            this.rankHandler.StyleRankChange += OnRankChange!;

            playerActionTracker.OnFlyTextCreation += AddScore;
            playerActionTracker.OnGcdClip += OnGcdClip;
            playerActionTracker.OnLimitBreak += OnLimitBreakCast;
            playerActionTracker.OnLimitBreakCanceled += OnLimitBreakCanceled;

            Service.Framework.Update += UpdateScore;
            Service.ClientState.Logout += ResetScore;

            scoringTableFactory = new ScoringTableFactory();
            jobScoringTable = ScoringTableFactory.DefaultScoringTable;

            var styleRank = styleRankHandler.CurrentStyle.Value;
            CurrentScoreRank = new(0, styleRank, jobScoringTable[styleRank]);

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
                DisablePointsGainedReduction();
            }

            if (isCastingLb)
            {
                CurrentScoreRank.Score =
                    Math.Clamp(CurrentScoreRank.Score 
                    + (float)(framework.UpdateDelta.TotalSeconds * CurrentScoreRank.StyleScoring.ReductionPerSecond * 100),
                    0, 
                    CurrentScoreRank.StyleScoring.Threshold);
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
            var points = val * CurrentScoreRank.StyleScoring.PointCoefficient;
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
            OnScoring?.Invoke(this, points);
        }

        private bool CanDisableGcdClippingRestrictions() => pointsReductionStopwatch.IsRunning 
            && pointsReductionStopwatch.ElapsedMilliseconds > PointsReductionDuration;

        private void OnInstanceChange(object send, bool value)
        {
            ResetScore();
        }

        private void OnCombatChange(object send, bool enteringCombat)
        {
            if(enteringCombat)
            {
                jobScoringTable = GetJobScoringTable();
                ResetScore();
            }

            DisablePointsGainedReduction();
            isCastingLb = false;
        }

        private void OnLimitBreakCast(object? sender, PlayerActionTracker.LimitBreakEvent e)
        {
            isCastingLb = e.IsCasting;
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
            pointsReductionStopwatch.Restart();
        }

        private void DisablePointsGainedReduction()
        {
            pointsReductionStopwatch.Reset();
        }

        private void ResetScore()
        {
            CurrentScoreRank.Score = 0;
        }

        private bool AreGcdClippingRestrictionsActive()
        {
            return pointsReductionStopwatch.IsRunning;
        }

        private void OnJobChange(object? sender, JobIds jobId)
        {
            ResetScore();
            DisablePointsGainedReduction();
        }

        private Dictionary<StyleType, StyleScoring> GetJobScoringTable()
        {
            int ilvl = itemLevelCalculator.CalculateCurrentItemLevel();
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

        public void Reset()
        {
            isCastingLb = false;
            ResetScore();
            DisablePointsGainedReduction();
        }
    }
}
