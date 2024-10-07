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
        public EventHandler<FinalRank>? DutyCompletedFinalRank;
        public FinalRank FinalRank { get; private set; }
        private readonly PlayerState playerState;
        private readonly PlayerActionTracker playerActionTracker;
        private readonly Stopwatch combatTimer;

        public FinalRankCalculator(PlayerState playerState, PlayerActionTracker playerActionTracker)
        {
            combatTimer = new Stopwatch();
            this.playerActionTracker = playerActionTracker;
            this.playerActionTracker.TotalCombatWastedGcd += OnTotalCombatWastedGcd;
            this.playerActionTracker.DutyCompletedWastedGcd += OnDutyCompletedWastedGcd;
            this.playerState = playerState;
            this.playerState.RegisterCombatStateChangeHandler(OnCombat);
        }

        private FinalRank DetermineFinalRank(float wastedGcd, ushort instanceId = 0)
        {
            var uptimePercentage = Math.Max(combatTimer.Elapsed.TotalSeconds - wastedGcd, 0) / Math.Max(combatTimer.Elapsed.TotalSeconds, 0.1);
            var rank = StyleType.D;
            if (Plugin.IsEmdModeEnabled())
            {
                rank = GetFinalRankEmd(uptimePercentage);
            }
            else
            {
                rank = GetFinalRank(uptimePercentage);
            }

            return new FinalRank(rank, new TimeSpan(combatTimer.ElapsedTicks), instanceId);
        }

        private void OnCombat(object? sender, bool enteredCombat)
        {
            if (!CanDisplayFinalRank())
            {
                combatTimer.Stop();
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
            combatTimer.Stop();
            if (!CanDisplayFinalRank())
            {
                return;
            }
            FinalRank = DetermineFinalRank(wastedGcd);
            FinalRankCalculated?.Invoke(this, FinalRank.Rank);
            PrintFinalRank(FinalRank);
        }

        private void OnDutyCompletedWastedGcd(object? sender, PlayerActionTracker.DutyCompletionStats dutyCompletionStats)
        {
            combatTimer.Stop();
            if (!Plugin.IsEnabledForCurrentJob())
            {
                return;
            }
            FinalRank = DetermineFinalRank(dutyCompletionStats.WastedGcd, dutyCompletionStats.InstanceId);
            DutyCompletedFinalRank?.Invoke(this, FinalRank);
        }

        private void PrintFinalRank(FinalRank finalRank)
        {
            var minutes = $"{finalRank.KillTime.Minutes}";
            minutes = minutes.PadLeft(2, '0');
            var seconds = $"{finalRank.KillTime.Seconds}";
            seconds = seconds.PadLeft(2, '0');
            Service.ChatGui.Print($"[DragoonMayCry] [{minutes}:{seconds}] Final rank : {finalRank.Rank}");
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

    public struct FinalRank
    {
        public StyleType Rank { get; private set; }
        public TimeSpan KillTime { get; private set; }
        public ushort InstanceId { get; private set; }

        public FinalRank(StyleType rank, TimeSpan killTime, ushort instanceId)
        {
            Rank = rank;
            KillTime = killTime;
            InstanceId = instanceId;
        }
    }
}
