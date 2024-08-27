using Dalamud.Plugin.Services;
using DragoonMayCry.State;
using System;
using System.Collections;
using System.Collections.Generic;
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
            public int Threshold;
            public int ReductionPerSecond;
            public int DemotionThreshold;

            public StyleScoring(int threshold, int reductionPerSecond, int demotionThreshold)
            {
                Threshold = threshold;
                ReductionPerSecond = reductionPerSecond;
            }
        }

        private static readonly Dictionary<StyleType, StyleScoring>
            DefaultScoreTable = new Dictionary<StyleType, StyleScoring>
            {
                { StyleType.NoStyle, new StyleScoring(60000, 500, 0) },
                { StyleType.D, new StyleScoring(80000, 1000, 4000) },
                { StyleType.C, new StyleScoring(90000, 5000, 9000) },
                { StyleType.B, new StyleScoring(90000, 6000, 9000) },
                { StyleType.A, new StyleScoring(100000, 8000, 10000) },
                { StyleType.S, new StyleScoring(100000, 10000, 10000) },
                { StyleType.SS, new StyleScoring(70000, 12000, 7000) },
                { StyleType.SSS, new StyleScoring(48000, 16000, 4800) },
            };

        public EventHandler<double>? OnScoring;
        public ScoreRank CurrentScoreRank { get; private set; }
        public ScoreRank? PreviousScoreRank { get; private set; }
        private readonly PlayerState playerState;
        private readonly StyleRankHandler rankHandler;
        private Dictionary<StyleType, StyleScoring> jobScoringTable;
        
        private bool isCastingLb;

        private readonly Stopwatch gcdClippingStopwatch;
        private int damageInstancesToCancel;

        public ScoreManager(StyleRankHandler styleRankHandler, ActionTracker actionTracker)
        {
            jobScoringTable = DefaultScoreTable;
            gcdClippingStopwatch = new Stopwatch();

            var styleRank = styleRankHandler.CurrentStyle!.Value;
            CurrentScoreRank = new(0, styleRank, jobScoringTable[styleRank]);

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
            if (CurrentScoreRank.Rank == StyleType.SSS)
            {
                CurrentScoreRank.Score = Math.Min(
                    CurrentScoreRank.Score, CurrentScoreRank.StyleScoring.Threshold);
            }
            OnScoring?.Invoke(this, points);
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
                CurrentScoreRank.Score = Math.Clamp(
                    CurrentScoreRank.Score, 0, CurrentScoreRank.StyleScoring.Threshold * 1.2f);
            }
            else
            {
                var scoreReduction =
                    (float)(framework.UpdateDelta.TotalSeconds *
                            CurrentScoreRank.StyleScoring.ReductionPerSecond);
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
            damageInstancesToCancel = 0;
            gcdClippingStopwatch.Reset();
        }

        private void OnLimitBreakCast(object? sender, ActionTracker.LimitBreakEvent e)
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

            var styleScoring = jobScoringTable[data.NewRank];
            if ((int)CurrentScoreRank.Rank < (int)data.NewRank)
            {
                
                CurrentScoreRank.Score = (float)Math.Clamp(CurrentScoreRank.Score %
                                                           styleScoring.Threshold, 0, styleScoring.Threshold * 0.5); ;
            }
            else if (data.IsBlunder)
            {
                if (data.NewRank == data.PreviousRank)
                {
                    CurrentScoreRank.Score = 0;
                }
                else
                {
                    CurrentScoreRank.Score = Math.Max(CurrentScoreRank.Score - CurrentScoreRank.StyleScoring.Threshold * 0.1f, 0);
                }
            }
            else
            {
                CurrentScoreRank.Score = styleScoring.Threshold * 0.8f;
            }

            PreviousScoreRank = CurrentScoreRank;
            CurrentScoreRank.Rank = data.NewRank;
        }

        private void OnGcdClip(object? send, float clippingTime)
        {
            var newScore = CurrentScoreRank.Score - CurrentScoreRank.StyleScoring.Threshold * 0.3f;
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
            isCastingLb = false;
        }

        private bool AreGcdClippingRestrictionsActive()
        {
            return gcdClippingStopwatch.IsRunning &&
                   damageInstancesToCancel > 0;
        }
    }
}
