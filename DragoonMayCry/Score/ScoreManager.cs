using DragoonMayCry.Style;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using DragoonMayCry.Data;
using DragoonMayCry.State;
using FFXIVClientStructs.FFXIV.Client.UI;
using ImGuiNET;
using static Dalamud.Interface.Utility.Raii.ImRaii;

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
    private static readonly double OPENER_COEFFICIENT = 1d;
    private static readonly double OPENER_DURATION = 23;

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
        if (combatStopwatch.TimeInCombat < 1)
        {
            return;
        }

        var points = val;
        if (combatStopwatch.TimeInCombat < OPENER_DURATION)
        {
            points *= OPENER_COEFFICIENT;
        }

        totalScore += points;
    }

    public void UpdateScore(IFramework framework)
    {
        if (!IsActive())
        {
            return;
        }
        CurrentScoreRank.Score = totalScore / combatStopwatch.TimeInCombat;
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
        CurrentScoreRank.Rank = rank;
    }

    private void ResetScore()
    {
        styleRankHandler.Reset();
        totalScore = 0;
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
