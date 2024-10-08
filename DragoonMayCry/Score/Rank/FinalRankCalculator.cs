using DragoonMayCry.Score.Model;
using DragoonMayCry.State;
using DragoonMayCry.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using static DragoonMayCry.Score.Rank.StyleRankHandler;

namespace DragoonMayCry.Score.Rank
{
    public class FinalRankCalculator : IResettable
    {
        public EventHandler<StyleType>? FinalRankCalculated;
        private readonly PlayerState playerState;
        private Dictionary<StyleType, double> timeInEachTier;
        private readonly Stopwatch tierTimer;
        private StyleType currentTier = StyleType.NoStyle;
        public FinalRankCalculator(PlayerState playerState, StyleRankHandler styleRankHandler)
        {
            timeInEachTier = new Dictionary<StyleType, double>();
            tierTimer = new Stopwatch();
            styleRankHandler.StyleRankChange += OnRankChange;
            this.playerState = playerState;
            this.playerState.RegisterCombatStateChangeHandler(OnCombat);
        }

        private StyleType DetermineFinalRank()
        {
            var finalRank = StyleType.D;
            double maxTime = 0;

            foreach (var entry in timeInEachTier)
            {
                var timeInTier = entry.Value;
                if (timeInTier > maxTime)
                {
                    maxTime = timeInTier;
                    finalRank = entry.Key;
                }
            }
            return finalRank;

        }

        private void OnCombat(object? sender, bool enteredCombat)
        {
            if (!CanDisplayFinalRank())
            {
                return;
            }

            if (enteredCombat)
            {
                timeInEachTier = new Dictionary<StyleType, double>();
                tierTimer.Start();
            }
            else if (tierTimer.IsRunning)
            {
                saveTimeInTier(currentTier);
                tierTimer.Reset();
                var finalRank = DetermineFinalRank();
                FinalRankCalculated?.Invoke(this, finalRank);
            }
        }

        public bool CanDisplayFinalRank()
        {
            return JobHelper.IsCombatJob(playerState.GetCurrentJob())
            && !playerState.IsInPvp()
            && Plugin.IsEnabledForCurrentJob()
            && (playerState.IsInsideInstance
                    || Plugin.Configuration!.ActiveOutsideInstance);
        }

        private void OnRankChange(object? sender, RankChangeData rankChange)
        {
            if (!CanDisplayFinalRank())
            {
                return;
            }

            currentTier = rankChange.NewRank;
            if (!tierTimer.IsRunning)
            {
                return;
            }
            saveTimeInTier(rankChange.PreviousRank);
            if (rankChange.IsBlunder)
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
            if (tier == StyleType.NoStyle)
            {
                tier = StyleType.D;
            }
            else if (tier == StyleType.S || tier == StyleType.SS || tier == StyleType.SSS)
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
