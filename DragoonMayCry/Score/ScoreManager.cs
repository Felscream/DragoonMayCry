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

        public ScoreManager(StyleRankHandler styleRankHandler, ActionTracker actionTracker)
        {
            combatStopwatch = CombatStopwatch.GetInstance();
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
            var points = val;
            Service.Log.Debug($"Time in combat {combatStopwatch.TimeInCombat()}");

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

            CurrentScoreRank.Score -=
                (float)(framework.UpdateDelta.TotalSeconds * CurrentScoreRank.Rank.ReductionPerSecond);
            CurrentScoreRank.Score = Math.Max(CurrentScoreRank.Score, 0);
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
            Service.Log.Warning("Clip");
            var newScore = CurrentScoreRank.Score - CurrentScoreRank.Rank.Threshold * 0.3f;
            CurrentScoreRank.Score = Math.Max(newScore, 0);
        }

        private void ResetScore()
        {
            CurrentScoreRank.Score = 0;
        }
    }
}
