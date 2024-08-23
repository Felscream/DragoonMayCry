using Dalamud.Plugin.Services;
using DragoonMayCry.State;
using DragoonMayCry.Style;
using System;

namespace DragoonMayCry.Score
{
    public class ScoreManager : IDisposable

    {

    public class ScoreRank
    {
        public double Score { get; set; }
        public StyleRank Rank { get; set; }

        public ScoreRank(int score, StyleRank styleRank)
        {
            Score = score;
            Rank = styleRank;

        }
    }


    public ScoreRank CurrentScoreRank { get; private set; }

    private readonly StyleRankHandler styleRankHandler;
    private readonly PlayerState playerState;

    private readonly CombatStopwatch combatStopwatch;
    private static readonly double OPENER_COEFFICIENT = .6d;
    private static readonly double MALUS_DURATION = 15;
    private static readonly double reductionPerSecond = 2000;
    private ScoreRank previousScoreRank;
    private Double totalScore;

    public ScoreManager(
        PlayerState playerState, StyleRankHandler styleRankHandler)
    {
        combatStopwatch = new();
        this.styleRankHandler = styleRankHandler;
        CurrentScoreRank = new(0, styleRankHandler.CurrentRank.Value);
        ResetScore();
        this.playerState = playerState;
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
        Service.Log.Debug($"Damage {val}");
        Service.Log.Debug($"Time in combat {combatStopwatch.TimeInCombat}");
            if (combatStopwatch.TimeInCombat < MALUS_DURATION)
        {
            points *= OPENER_COEFFICIENT;
        }

        totalScore += points;
        CurrentScoreRank.Score += points;
        Service.Log.Debug($"Score {CurrentScoreRank.Score}");
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

    public ScoreRank GetScoreRankToDisplay()
    {
        if (playerState.IsInCombat)
        {
            return CurrentScoreRank;
        }

        return previousScoreRank;
    }



    private void OnInstanceChange(object send, bool value)
    {
        ResetScore();
    }

    private void OnCombatChange(object send, bool enteringCombat)
    {
        if (!enteringCombat)
        {
            previousScoreRank = CurrentScoreRank;
            ResetScore();
            combatStopwatch.Stop();
        }
        else
        {
            combatStopwatch.Start();
        }
    }

    private void OnRankChange(object sender, StyleRank rank)
    {
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
        styleRankHandler.Reset();
        totalScore = 0;
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
