using DragoonMayCry.Score.Action;
using DragoonMayCry.Score.Model;
using DragoonMayCry.State;
using DragoonMayCry.Util;
using System;
using System.Diagnostics;

namespace DragoonMayCry.Score.Rank
{
    public class FinalRankCalculator : IResettable
    {
        public EventHandler<StyleType>? FinalRankCalculated;
        private readonly PlayerState playerState;
        private readonly PlayerActionTracker playerActionTracker;
        private readonly Stopwatch combatTimer;

        public FinalRankCalculator(PlayerState playerState, PlayerActionTracker playerActionTracker)
        {
            combatTimer = new Stopwatch();
            this.playerActionTracker = playerActionTracker;
            this.playerActionTracker.TotalCombatWastedGcd += OnTotalCombatWastedGcd;
            this.playerState = playerState;
            this.playerState.RegisterCombatStateChangeHandler(OnCombat);
        }

        private StyleType DetermineFinalRank(float wastedGcd)
        {
            var uptimePercentage = Math.Max(combatTimer.Elapsed.TotalSeconds - wastedGcd, 0) / Math.Max(combatTimer.Elapsed.TotalSeconds, 0.1);
            if (Plugin.IsEmdModeEnabled())
            {
                return GetFinalRankEmd(uptimePercentage);
            }
            return GetFinalRank(uptimePercentage);

        }

        private void OnCombat(object? sender, bool enteredCombat)
        {
            if (!CanDisplayFinalRank())
            {
                combatTimer.Reset();
                return;
            }

            if (enteredCombat)
            {
                combatTimer.Restart();
            }
            else
            {
                combatTimer.Stop();
            }
        }

        private void OnTotalCombatWastedGcd(object? sender, float wastedGcd)
        {
            if (!CanDisplayFinalRank())
            {
                combatTimer.Reset();
                return;
            }
            var finalRank = DetermineFinalRank(wastedGcd);
            FinalRankCalculated?.Invoke(this, finalRank);
            PrintFinalRank(finalRank);
            combatTimer.Reset();
        }

        private void PrintFinalRank(StyleType finalRank)
        {
            var minutes = $"{combatTimer.Elapsed.Minutes}";
            minutes = minutes.PadLeft(2, '0');
            var seconds = $"{combatTimer.Elapsed.Seconds}";
            seconds = seconds.PadLeft(2, '0');
            Service.ChatGui.Print($"[DragoonMayCry] [{minutes}:{seconds}] Final rank : {finalRank}");
        }

        public bool CanDisplayFinalRank()
        {
            return JobHelper.IsCombatJob(playerState.GetCurrentJob())
                && !playerState.IsInPvp()
                && Plugin.IsEnabledForCurrentJob()
                && (playerState.IsInsideInstance
                        || Plugin.Configuration!.ActiveOutsideInstance);
        }

        private static StyleType GetFinalRank(double uptimePercentage)
        {
            if (uptimePercentage > 0.98)
            {
                return StyleType.S;
            }

            if (uptimePercentage > 0.97)
            {
                return StyleType.A;
            }

            if (uptimePercentage > 0.95)
            {
                return StyleType.B;
            }

            if (uptimePercentage > 0.93)
            {
                return StyleType.C;
            }

            return StyleType.D;
        }

        private static StyleType GetFinalRankEmd(double uptimePercentage)
        {
            if (uptimePercentage > 0.991)
            {
                return StyleType.S;
            }

            if (uptimePercentage > 0.98)
            {
                return StyleType.A;
            }

            if (uptimePercentage > 0.97)
            {
                return StyleType.B;
            }

            if (uptimePercentage > 0.95)
            {
                return StyleType.C;
            }

            return StyleType.D;
        }

        public void Reset()
        {
            combatTimer.Reset();
        }
    }
}
