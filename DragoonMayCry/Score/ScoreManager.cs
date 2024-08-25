using Dalamud.Plugin.Services;
using DragoonMayCry.State;
using DragoonMayCry.Style;
using System;
using System.Diagnostics;
using DragoonMayCry.Util;

namespace DragoonMayCry.Score
{
    public class ScoreManager : IDisposable

    {

    public class ScoreRank
    {
        public double Score { get; set; }
        public StyleRank Rank { get; set; }

        public ScoreRank(double score, StyleRank styleRank)
        {
            Score = score;
            Rank = styleRank;

        }
    }

    public EventHandler<double> OnScoring;
    public ScoreRank CurrentScoreRank { get; private set; }
    public ScoreRank PreviousScoreRank { get; private set; }
    private readonly PlayerState playerState;

    private readonly CombatStopwatch combatStopwatch;

    public ScoreManager(StyleRankHandler styleRankHandler)
    {
        combatStopwatch = CombatStopwatch.Instance();
        CurrentScoreRank = new(0, styleRankHandler.CurrentRank.Value);
        ResetScore();

        this.playerState = PlayerState.Instance();
        this.playerState.RegisterJobChangeHandler(((sender, ids) => ResetScore()));
        this.playerState.RegisterInstanceChangeHandler(OnInstanceChange);
        this.playerState.RegisterCombatStateChangeHandler(OnCombatChange);

        styleRankHandler.OnStyleRankChange += OnRankChange;
        Service.Framework.Update += UpdateScore;
    }

    public void Dispose()
    {
        Service.Framework.Update -= UpdateScore;
    }

    public void AddScore(double val)
    {
        var points = val;
        Service.Log.Debug($"Time in combat {combatStopwatch.TimeInCombat()}");

        CurrentScoreRank.Score += points;
        OnScoring?.Invoke(this, points);
    }

    public void UpdateScore(IFramework framework)
    {
        if (!IsActive())
        {
            return;
        }

        CurrentScoreRank.Score -=
            framework.UpdateDelta.TotalSeconds * CurrentScoreRank.Rank.ReductionPerSecond;
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

    private void OnRankChange(object sender, StyleRank rank)
    {
        PreviousScoreRank = CurrentScoreRank;
        if ((int)CurrentScoreRank.Rank.StyleType < (int)rank.StyleType)
        {
            CurrentScoreRank.Score -= CurrentScoreRank.Rank.Threshold;
        }
        else
        {
            CurrentScoreRank.Score = rank.Threshold * 0.8;
        }

        
        CurrentScoreRank.Rank = rank;
    }

    private void ResetScore()
    {
        CurrentScoreRank.Score = 0;
    }


    public void OnLogout() => ResetScore();

    public bool IsActive()
    {
        return playerState.IsInCombat &&
               ((!playerState.IsInsideInstance &&
                 Plugin.Configuration.ActiveOutsideInstance)
                || playerState.IsInsideInstance);
    }
    }
}
