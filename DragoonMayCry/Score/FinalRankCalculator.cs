using DragoonMayCry.Score.Model;
using DragoonMayCry.Score.Style.Rank;
using DragoonMayCry.State;
using DragoonMayCry.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DragoonMayCry.Score.Style.Rank.StyleRankHandler;

namespace DragoonMayCry.Score
{
    public class FinalRankCalculator : IResettable
    {
        public EventHandler<StyleType>? FinalRankCalculated;
        private readonly PlayerState playerState;
        private Dictionary<StyleType, double> timeInEachTier;
        private Stopwatch tierTimer;
        private StyleType currentTier = StyleType.NoStyle;
        public FinalRankCalculator(PlayerState playerState, StyleRankHandler styleRankHandler) {
            timeInEachTier = new Dictionary<StyleType, double>();
            tierTimer = new Stopwatch();
            styleRankHandler.StyleRankChange += OnRankChange;
            this.playerState = playerState;
            this.playerState.RegisterCombatStateChangeHandler(OnCombat);
        }

        private StyleType DetermineFinalRank()
        {
            StyleType finalRank = StyleType.D;
            double maxTime = 0;
       
            foreach(KeyValuePair<StyleType, double> entry in timeInEachTier)
            {
                var timeInTier = entry.Value ;
                if(timeInTier > maxTime)
                {
                    maxTime = timeInTier;
                    finalRank = entry.Key;
                }
            }
            return finalRank;

        }

        private void OnCombat(object? sender, bool enteredCombat)
        {
            if(playerState.IsInPvp()
               || (!playerState.IsInsideInstance && !Plugin.Configuration!.ActiveOutsideInstance)
               || !playerState.IsCombatJob())
            {
                return;
            }

            if (enteredCombat)
            {
                timeInEachTier = new Dictionary<StyleType, double>();
                tierTimer.Start();
            }
            else if(tierTimer.IsRunning)
            {
                saveTimeInTier(currentTier);
                tierTimer.Reset();
                var finalRank = DetermineFinalRank();
                FinalRankCalculated?.Invoke(this, finalRank);
            }
        }

        private void OnRankChange(object? sender, RankChangeData rankChange)
        {
            if (!Plugin.CanRunDmc())
            {
                return;
            }

            currentTier = rankChange.NewRank;
            if (!tierTimer.IsRunning) {
                return;
            }
            saveTimeInTier(rankChange.PreviousRank);
            if(rankChange.IsBlunder)
            {
                if (timeInEachTier.ContainsKey(StyleType.S))
                {
                    timeInEachTier[StyleType.S] -= 10d;
                }
                else
                {
                    timeInEachTier.Add(StyleType.S, -10d);
                }
            }
            
            if (!timeInEachTier.ContainsKey(currentTier))
            {
                timeInEachTier.Add(currentTier, 0);
            }
            tierTimer.Restart();
        }

        private void saveTimeInTier(StyleType tier)
        {
            if(tier == StyleType.NoStyle)
            {
                tier = StyleType.D;
            } else if (tier == StyleType.S || tier == StyleType.SS || tier == StyleType.SSS)
            {
                tier = StyleType.S;
            }
            if (!timeInEachTier.ContainsKey(tier))
            {
                timeInEachTier.Add(tier, tierTimer.Elapsed.TotalSeconds);
            }
            else
            {
                timeInEachTier[tier] += tierTimer.Elapsed.TotalSeconds;
            }
        }

        public void Reset()
        {
            timeInEachTier = new Dictionary<StyleType, double>();
            tierTimer.Reset();
        }
    }
}
