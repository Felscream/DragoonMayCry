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
using DragoonMayCry.State;
using FFXIVClientStructs.FFXIV.Client.UI;
using ImGuiNET;
using static Dalamud.Interface.Utility.Raii.ImRaii;

namespace DragoonMayCry.Score
{
    public class ScoreManager
    {

        public class ScoreRank
        {
            public int Score { get; set; }
            public StyleRank Rank { get; set; }

            public ScoreRank(int score, StyleRank styleRank)
            {
                Score = score;
                Rank = styleRank;

            }
        }

       
        public ScoreRank CurrentScoreRank { get; private set; }
        
        private StyleRankHandler styleRankHandler;
        private PlayerState playerState;
        private ScoreRank previousScoreRank;
        

        public ScoreManager(PlayerState playerState)
        {
            styleRankHandler = new();
            CurrentScoreRank = new(0, styleRankHandler.CurrentRank.Value);
            ResetScore();
            this.playerState = playerState;
            this.playerState.RegisterInstanceChangeHandler(OnInstanceChange);
            this.playerState.RegisterCombatStateChangeHandler(OnCombatChange);
        }

        

        public void GoToNextRank(bool loop, double lastThreshold)
        {
            if (styleRankHandler.ReachedLastRank() && !loop)
            {
                return;
            }
            styleRankHandler.GoToNextRank(true);
            CurrentScoreRank.Rank = styleRankHandler.CurrentRank.Value;
            CurrentScoreRank.Score = (int)Math.Floor(CurrentScoreRank.Score - lastThreshold);
        }

        public void AddScore(int val)
        {
            CurrentScoreRank.Score += val;
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
            }
        }

        private void ResetScore()
        {
            styleRankHandler.Reset();
            CurrentScoreRank = new ScoreRank(0, styleRankHandler.CurrentRank.Value);
        }


        public void OnLogout() => ResetScore();
        public bool IsActive() => playerState.IsInCombat;
    }
}
