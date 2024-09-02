using DragoonMayCry.Score.Model;
using DragoonMayCry.Score.Style;
using DragoonMayCry.State;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DragoonMayCry.Score.Style.StyleRankHandler;

namespace DragoonMayCry.Score
{
    public class FinalRankCalculator
    {
        public EventHandler<StyleType> FinalRankCalculated;
        private Dictionary<StyleType, double> timeInEachTier;
        private Stopwatch tierTimer;
        private StyleType currentTier = StyleType.NoStyle;
        public FinalRankCalculator(PlayerState playerState, StyleRankHandler styleRankHandler) {

            timeInEachTier = new Dictionary<StyleType, double>();
            tierTimer = new Stopwatch();
            styleRankHandler.StyleRankChange += OnRankChange;
            playerState.RegisterCombatStateChangeHandler(OnCombat);
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
            if (enteredCombat)
            {
                timeInEachTier = new Dictionary<StyleType, double>();
                tierTimer.Start();
            }
            else
            {
                saveTimeInTier();
                var finalRank = DetermineFinalRank();
                FinalRankCalculated?.Invoke(this, finalRank);
                tierTimer.Reset();
            }
        }

        private void OnRankChange(object? sender, RankChangeData rankChange)
        {
            saveTimeInTier();

            currentTier = rankChange.NewRank;
            if (!timeInEachTier.ContainsKey(currentTier))
            {
                timeInEachTier.Add(currentTier, 0);
            }
            tierTimer.Restart();
        }

        private void saveTimeInTier()
        {
            var tier = currentTier;
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
    }
}
