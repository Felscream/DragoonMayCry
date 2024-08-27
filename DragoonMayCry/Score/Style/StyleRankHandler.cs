
using System;
using DragoonMayCry.Audio;
using DragoonMayCry.Util;
using System.IO;
using DragoonMayCry.Data;
using DragoonMayCry.Score.Action;
using DragoonMayCry.State;

namespace DragoonMayCry.Score.Style
{
    public class StyleRankHandler
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
        
        private static readonly DoubleLinkedList<StyleType> Styles = new DoubleLinkedList<StyleType>(
            StyleType.NoStyle, 
            StyleType.D, 
            StyleType.C, 
            StyleType.B, 
            StyleType.A, 
            StyleType.S, 
            StyleType.SS, 
            StyleType.SSS);

        public StyleRankHandler(ActionTracker actionTracker)
        {
            Reset();

            var playerState = PlayerState.GetInstance();
            playerState.RegisterCombatStateChangeHandler(OnCombatChange!);

            actionTracker.OnGcdDropped += OnGcdDropped;
            actionTracker.OnLimitBreakCanceled += OnLimitBreakCanceled;
            actionTracker.OnLimitBreak += OnLimitBreak;
            CurrentStyle = Styles.Head!;
        }

        public void GoToNextRank(bool playSfx, bool loop = false, bool forceSfx = false)
        {
            if (CurrentStyle?.Next == null)
            {
                if (Plugin.Configuration!.PlaySoundEffects && playSfx && forceSfx && CurrentStyle != null)
                {
                    AudioService.PlaySfx(CurrentStyle.Value);
                }
                if (Styles.Head != null && loop)
                {
                    Reset();
                }
                
            }
            else if (CurrentStyle?.Next != null)
            {
                CurrentStyle = CurrentStyle.Next;
                Service.Log.Debug($"New rank reached {CurrentStyle.Value}");
                StyleRankChange?.Invoke(this, new(CurrentStyle!.Previous!.Value, CurrentStyle.Value, false));
                if (Plugin.Configuration!.PlaySoundEffects && playSfx)
                {
                    AudioService.PlaySfx(CurrentStyle.Value);
                }
            }
        }

        public void ReturnToPreviousRank(bool droppedGcd)
        {
            
            if (CurrentStyle?.Previous == null)
            {
                if (droppedGcd)
                {
                    StyleRankChange?.Invoke(this, new(CurrentStyle!.Value, CurrentStyle.Value, droppedGcd));
                }
                return;
            }

            CurrentStyle = CurrentStyle.Previous;
            Service.Log.Debug($"Going back to rank {CurrentStyle.Value}");
            StyleRankChange?.Invoke(this, new(CurrentStyle!.Next!.Value, CurrentStyle.Value, droppedGcd));
        }

        public void Reset()
        {
            CurrentStyle = Styles.Head!;
            StyleRankChange?.Invoke(this, new(StyleType.NoStyle, CurrentStyle!.Value, false));
        }

        private void ForceRankTo(StyleType type, bool isBlunder)
        {
            
            if (CurrentStyle?.Value == type)
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
            } while(CurrentStyle.Value != type && CurrentStyle.Value != tempRank.Value);

            if (isBlunder)
            {
                AudioService.PlaySfx(SoundId.DeadWeight, true);
            }

            StyleRankChange?.Invoke(this, new(tempRank.Value, CurrentStyle.Value!, isBlunder));
        }

        private void OnCombatChange(object send, bool enteringCombat)
        {
            if (enteringCombat)
            {
                Reset();
            }
        }

        private void OnGcdDropped(object? sender, EventArgs args)
        {
            if (CurrentStyle?.Value != StyleType.NoStyle && Plugin.Configuration!.PlaySoundEffects)
            {
                AudioService.PlaySfx(SoundId.DeadWeight);
            }
            ReturnToPreviousRank(true);
            
        }

        private void OnLimitBreakCanceled(object? sender, EventArgs args)
        {
            ForceRankTo(StyleType.D, true);
        }

        private void OnLimitBreak(
            object? sender, ActionTracker.LimitBreakEvent e)
        {
            if (e.IsTankLb && CurrentStyle?.Value < StyleType.S)
            {
                ForceRankTo(StyleType.A, false);
            }
        }
    }
}
