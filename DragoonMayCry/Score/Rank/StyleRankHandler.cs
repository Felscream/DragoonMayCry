
using DragoonMayCry.Score.Action;
using DragoonMayCry.Score.Model;
using DragoonMayCry.State;
using DragoonMayCry.Util;
using System;

namespace DragoonMayCry.Score.Rank
{
    public class StyleRankHandler : IResettable
    {
        public struct RankChangeData
        {
            public StyleType PreviousRank;
            public StyleType NewRank;
            public bool IsBlunder;

            public RankChangeData(
                StyleType previousRank, StyleType newRank, bool isBlunder)
            {
                PreviousRank = previousRank;
                NewRank = newRank;
                IsBlunder = isBlunder;
            }

        }

        public EventHandler<RankChangeData>? StyleRankChange;
        public DoubleLinkedNode<StyleType> CurrentStyle { get; private set; }

        private static readonly DoubleLinkedList<StyleType> Styles = new(
            StyleType.NoStyle,
            StyleType.D,
            StyleType.C,
            StyleType.B,
            StyleType.A,
            StyleType.S,
            StyleType.SS,
            StyleType.SSS);

        private readonly PlayerState playerState;

        public StyleRankHandler(PlayerActionTracker playerActionTracker)
        {
            ResetRank();
            playerState = PlayerState.GetInstance();
            playerState.RegisterCombatStateChangeHandler(OnCombatChange!);
            playerState.RegisterDeathStateChangeHandler(OnDeath);
            playerState.RegisterDamageDownHandler(OnDamageDown);
            playerActionTracker.OnGcdDropped += OnGcdDropped;
            playerActionTracker.OnLimitBreakCanceled += OnLimitBreakCanceled;
            playerActionTracker.UsingLimitBreak += OnLimitBreak;
            playerActionTracker.OnLimitBreakEffect += OnLimitBreakEffect;
            CurrentStyle = Styles.Head!;
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
                if (Styles.Head != null && loop)
                {
                    ResetRank();
                }

            }
            else if (CurrentStyle.Next != null)
            {
                CurrentStyle = CurrentStyle.Next;
                StyleRankChange?.Invoke(this, new(CurrentStyle.Previous!.Value, CurrentStyle.Value, false));
            }
        }

        private void ReturnToPreviousRank(bool droppedGcd)
        {
            if (CurrentStyle.Previous == null || CurrentStyle.Previous.Value == StyleType.NoStyle)
            {
                if (droppedGcd)
                {
                    StyleRankChange?.Invoke(this, new(CurrentStyle!.Value, CurrentStyle.Value, droppedGcd));
                }
                return;
            }
            CurrentStyle = CurrentStyle.Previous;
            StyleRankChange?.Invoke(this, new(CurrentStyle.Next!.Value, CurrentStyle.Value, droppedGcd));
        }

        private void ResetRank()
        {
            CurrentStyle = Styles.Head!;
            StyleRankChange?.Invoke(this, new(CurrentStyle.Value, CurrentStyle.Value, false));
        }

        private void ForceRankTo(StyleType type, bool isBlunder)
        {

            if (CurrentStyle?.Value == type || isBlunder && CurrentStyle?.Value < type)
            {
                return;
            }

            if (CurrentStyle == null)
            {
                CurrentStyle = Styles.Head!;
            }
            var tempRank = CurrentStyle;
            do
            {
                if (CurrentStyle?.Next != null)
                {
                    CurrentStyle = CurrentStyle.Next;
                }
                else
                {
                    CurrentStyle = Styles.Head!;
                }
            } while (CurrentStyle.Value != type && CurrentStyle.Value != tempRank.Value);

            StyleRankChange?.Invoke(this, new(tempRank.Value, CurrentStyle.Value, isBlunder));
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

        public void Reset()
        {
            ResetRank();
        }
    }
}
