#region

using DragoonMayCry.Score.Action;
using DragoonMayCry.Score.Model;
using DragoonMayCry.State;
using DragoonMayCry.Util;
using System;
using System.Diagnostics;

#endregion

namespace DragoonMayCry.Score.Rank
{
    public class FinalRankCalculator : IResettable
    {
        private readonly Stopwatch combatTimer;
        private readonly PlayerActionTracker playerActionTracker;
        private readonly PlayerState playerState;
        public EventHandler<FinalRank>? DutyCompletedFinalRank;
        public EventHandler<StyleType>? FinalRankCalculated;

        public FinalRankCalculator(PlayerState playerState, PlayerActionTracker playerActionTracker)
        {
            combatTimer = new Stopwatch();
            this.playerActionTracker = playerActionTracker;
            this.playerActionTracker.TotalCombatWastedGcd += OnTotalCombatWastedGcd;
            this.playerActionTracker.DutyCompletedWastedGcd += OnDutyCompletedWastedGcd;
            this.playerState = playerState;
            this.playerState.RegisterCombatStateChangeHandler(OnCombat);
        }
        public FinalRank FinalRank { get; private set; }

        public void Reset()
        {
            combatTimer.Reset();
        }

        private FinalRank DetermineFinalRank(float wastedGcd, ushort instanceId = 0)
        {
            var uptimePercentage = Math.Max(combatTimer.Elapsed.TotalSeconds - wastedGcd, 0) /
                                   Math.Max(combatTimer.Elapsed.TotalSeconds, 0.1);

#if DEBUG
            Service.Log.Debug(
                $"uptime % {uptimePercentage} fight time {combatTimer.Elapsed.TotalSeconds} wasted GCD {wastedGcd} s");
#endif
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

        private void OnDutyCompletedWastedGcd(
            object? sender, PlayerActionTracker.DutyCompletionStats dutyCompletionStats)
        {
            combatTimer.Stop();
            if (!CanDisplayFinalRank())
            {
                return;
            }

            FinalRank = DetermineFinalRank(dutyCompletionStats.WastedGcd, dutyCompletionStats.InstanceId);
            DutyCompletedFinalRank?.Invoke(this, FinalRank);
        }

        private void PrintFinalRank(FinalRank finalRank)
        {
            if (!Plugin.Configuration!.EnabledFinalRankChatLogging)
            {
                return;
            }

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
            return uptimePercentage switch
            {
                > 0.95 => StyleType.S,
                > 0.9 => StyleType.A,
                > 0.85 => StyleType.B,
                > 0.75 => StyleType.C,
                _ => StyleType.D,
            };

        }

        private static StyleType GetFinalRankEmd(double uptimePercentage)
        {
            return uptimePercentage switch
            {
                > 0.98 => StyleType.S,
                > 0.96 => StyleType.A,
                > 0.93 => StyleType.B,
                > 0.88 => StyleType.C,
                _ => StyleType.D,
            };

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
