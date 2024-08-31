
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

        private readonly AudioService audioService;

        public StyleRankHandler(PlayerActionTracker playerActionTracker)
        {
            Reset();
            audioService = new AudioService();
            var playerState = PlayerState.GetInstance();
            playerState.RegisterCombatStateChangeHandler(OnCombatChange!);
            playerState.RegisterDeathStateChangeHandler(OnDeath);
            playerState.RegisterDamageDownHandler(OnDamageDown);
            playerActionTracker.OnGcdDropped += OnGcdDropped;
            playerActionTracker.OnLimitBreakCanceled += OnLimitBreakCanceled;
            playerActionTracker.OnLimitBreak += OnLimitBreak;
            playerActionTracker.OnLimitBreakEffect += OnLimitBreakEffect;
            CurrentStyle = Styles.Head!;
        }

        public void OnDemotion(object? sender, bool playSfx)
        {
            ReturnToPreviousRank(playSfx);
        }

        public void OnPromotion(object? sender, bool playSfx)
        {
            GoToNextRank(playSfx);
        }

        private void OnLimitBreakEffect(object? sender, EventArgs e)
        {
            GoToNextRank(true, false, true);
        }

        private void GoToNextRank(bool playSfx, bool loop = false, bool forceSfx = false)
        {
            if (CurrentStyle.Next == null)
            {
                if (Plugin.Configuration!.PlaySoundEffects && playSfx && forceSfx)
                {
                    audioService.PlaySfx(CurrentStyle.Value);
                }
                if (Styles.Head != null && loop)
                {
                    Reset();
                }

            }
            else if (CurrentStyle.Next != null)
            {
                StyleRankChange?.Invoke(this, new(CurrentStyle.Value, CurrentStyle.Next.Value, false));
                CurrentStyle = CurrentStyle.Next;
                Service.Log.Debug($"New rank reached {CurrentStyle.Value}");
                if (Plugin.Configuration!.PlaySoundEffects && playSfx)
                {
                    audioService.PlaySfx(CurrentStyle.Value);
                }

            }
        }

        private void ReturnToPreviousRank(bool droppedGcd)
        {
            if (CurrentStyle.Previous == null)
            {
                if (droppedGcd)
                {
                    StyleRankChange?.Invoke(this, new(CurrentStyle!.Value, CurrentStyle.Value, droppedGcd));
                }
                return;
            }

            StyleRankChange?.Invoke(this, new(CurrentStyle.Value, CurrentStyle.Previous.Value, droppedGcd));
            CurrentStyle = CurrentStyle.Previous;
        }

        private void Reset()
        {
            CurrentStyle = Styles.Head!;
            StyleRankChange?.Invoke(this, new(CurrentStyle.Value, CurrentStyle.Value, false));
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
                Service.Log.Debug("Forcing sfx to play");
                audioService.PlaySfx(SoundId.DeadWeight, true);
            }

            StyleRankChange?.Invoke(this, new(tempRank.Value, CurrentStyle.Value, isBlunder));
        }

        private void OnCombatChange(object send, bool enteringCombat)
        {
            if (enteringCombat)
            {
                Reset();
                audioService.ResetSfxPlayCounter();
            }
        }

        private void OnGcdDropped(object? sender, EventArgs args)
        {
            if (CurrentStyle.Value != StyleType.NoStyle && Plugin.Configuration!.PlaySoundEffects)
            {
                audioService.PlaySfx(SoundId.DeadWeight);
            }
            ReturnToPreviousRank(true);
            
        }

        private void OnLimitBreakCanceled(object? sender, EventArgs args)
        {
            ForceRankTo(StyleType.D, true);
        }

        private void OnDeath(object? sender, bool isDead)
        {
            if (isDead)
            {
                ForceRankTo(StyleType.D, true);
            }
        }

        private void OnLimitBreak(
            object? sender, PlayerActionTracker.LimitBreakEvent e)
        {
            if (e.IsTankLb && CurrentStyle.Value < StyleType.S)
            {
                ForceRankTo(StyleType.A, false);
            }
        }

        private void OnDamageDown(object? sender, bool hasDamageDown)
        {
            if(!hasDamageDown)
            {
                return;
            }

            ForceRankTo(StyleType.D, true);
        }
    }
}
