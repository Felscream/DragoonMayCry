using DragoonMayCry.Configuration;
using DragoonMayCry.Score.Action;
using DragoonMayCry.Score.Model;
using DragoonMayCry.State;
using System;
using System.Collections.Generic;

namespace DragoonMayCry.Score.Rank
{
    public class StyleRankHandler : IResettable
    {

        private static readonly LinkedList<StyleType> Styles = new(new[]
        {
            StyleType.NoStyle,
            StyleType.D,
            StyleType.C,
            StyleType.B,
            StyleType.A,
            StyleType.S,
            StyleType.SS,
            StyleType.SSS
        });

        private readonly PlayerState playerState;

        public EventHandler<RankChangeData>? StyleRankChange;

        public StyleRankHandler(PlayerActionTracker playerActionTracker)
        {
            ResetRank();
            playerState = PlayerState.GetInstance();
            playerState.RegisterCombatStateChangeHandler(OnCombatChange!);
            playerState.RegisterDeathStateChangeHandler(OnDeath);
            playerState.RegisterDamageDownHandler(OnDamageDown);
            playerActionTracker.GcdDropped += OnGcdDropped;
            playerActionTracker.LimitBreakCanceled += OnLimitBreakCanceled;
            playerActionTracker.UsingLimitBreak += OnLimitBreak;
            playerActionTracker.LimitBreakEffect += OnLimitBreakEffect;
            CurrentStyle = Styles.First!;
        }
        public LinkedListNode<StyleType> CurrentStyle { get; private set; }

        public void Reset()
        {
            ResetRank();
        }

        public void OnDemotion(object? sender, bool playSfx)
        {
            ReturnToPreviousRank(playSfx);
        }

        public void OnPromotion(object? sender, EventArgs e)
        {
            GoToNextRank();
        }

        private void OnLimitBreakEffect(object? sender, EventArgs e)
        {
            GoToNextRank();
        }

        private void GoToNextRank(bool loop = false)
        {
            if (CurrentStyle.Next == null)
            {
                if (Styles.First != null && loop)
                {
                    ResetRank();
                }
            }
            else if (CurrentStyle.Next != null)
            {
                CurrentStyle = CurrentStyle.Next;
                StyleRankChange?.Invoke(
                    this, new RankChangeData(CurrentStyle.Previous!.Value, CurrentStyle.Value, false));
            }
        }

        private void ReturnToPreviousRank(bool droppedGcd)
        {
            if (CurrentStyle.Previous == null || CurrentStyle.Previous.Value == StyleType.NoStyle)
            {
                if (droppedGcd)
                {
                    StyleRankChange?.Invoke(
                        this, new RankChangeData(CurrentStyle!.Value, CurrentStyle.Value, droppedGcd));
                }

                return;
            }

            CurrentStyle = CurrentStyle.Previous;
            StyleRankChange?.Invoke(this, new RankChangeData(CurrentStyle.Next!.Value, CurrentStyle.Value, droppedGcd));
        }

        private void ResetRank()
        {
            CurrentStyle = Styles.First!;
            StyleRankChange?.Invoke(this, new RankChangeData(CurrentStyle.Value, CurrentStyle.Value, false));
        }

        private void ForceRankTo(StyleType type, bool isBlunder)
        {
            if (CurrentStyle?.Value == type || isBlunder && CurrentStyle?.Value < type)
            {
                return;
            }

            if (CurrentStyle == null)
            {
                CurrentStyle = Styles.First!;
            }

            var tempRank = CurrentStyle;
            CurrentStyle = Styles.Find(type) ?? CurrentStyle;

            StyleRankChange?.Invoke(this, new RankChangeData(tempRank.Value, CurrentStyle.Value, isBlunder));
        }

        private void OnCombatChange(object send, bool enteringCombat)
        {
            if (enteringCombat)
            {
                ResetRank();
            }
        }

        private void OnGcdDropped(object? sender, EventArgs args)
        {
            if (playerState.IsDead)
            {
                return;
            }

            // Do not demote if Sprout mode is active for current job
            var currentJob = playerState.GetCurrentJob();
            if (Plugin.Configuration!.JobConfiguration.TryGetValue(currentJob, out var jobConfiguration) &&
                jobConfiguration.DifficultyMode == DifficultyMode.Sprout)
            {
                return;
            }

            if (CurrentStyle.Value < StyleType.S)
            {
                ReturnToPreviousRank(true);
            }
            else
            {
                ForceRankTo(StyleType.B, true);
            }
        }

        private void OnLimitBreakCanceled(object? sender, EventArgs args)
        {
            if (playerState.IsDead)
            {
                return;
            }

            ForceRankTo(StyleType.D, true);
        }

        private void OnDeath(object? sender, bool isDead)
        {
            if (isDead && Plugin.CanHandleEvents())
            {
                ForceRankTo(StyleType.D, true);
            }
        }

        private void OnLimitBreak(
            object? sender, PlayerActionTracker.LimitBreakEvent e)
        {
            if (!e.IsCasting)
            {
                return;
            }

            if (e.IsTankLb && CurrentStyle.Value < StyleType.S)
            {
                ForceRankTo(StyleType.A, false);
            }
        }

        private void OnDamageDown(object? sender, bool hasDamageDown)
        {
            if (!hasDamageDown || playerState.IsDead)
            {
                return;
            }

            ForceRankTo(StyleType.D, true);
        }

        public struct RankChangeData(StyleType previousRank, StyleType newRank, bool isBlunder)
        {
            public readonly StyleType PreviousRank = previousRank;
            public readonly StyleType NewRank = newRank;
            public readonly bool IsBlunder = isBlunder;
        }
    }
}
