
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
            public StyleRank? PreviousRank;
            public StyleRank NewRank;
            public bool IsBlunder;

            public RankChangeData(
                StyleRank previousRank, StyleRank newRank, bool isBlunder)
            {
                PreviousRank = previousRank;
                NewRank = newRank;
                IsBlunder = isBlunder;
            }

        }
        private static readonly DoubleLinkedList<StyleRank> DEFAULT_STYLE_RANK = new DoubleLinkedList<StyleRank>(
            new StyleRank(StyleType.NO_STYLE, 60000, 500),
            new StyleRank(StyleType.D, 80000, 1000),
            new StyleRank(StyleType.C, 90000, 1500),
            new StyleRank(StyleType.B, 90000, 2000),
            new StyleRank(StyleType.A, 100000, 4000),
            new StyleRank(StyleType.S, 100000, 8000),
            new StyleRank(StyleType.SS, 100000, 10000),
            new StyleRank(StyleType.SSS, 60000, 12000));

        public EventHandler<RankChangeData>? StyleRankChange;
        public DoubleLinkedNode<StyleRank>? CurrentRank { get; private set; }
        public DoubleLinkedNode<StyleRank>? PreviousRank { get; private set; }
        private DoubleLinkedList<StyleRank> styles;

        public StyleRankHandler(ActionTracker actionTracker)
        {
            styles = DEFAULT_STYLE_RANK;
            Reset();

            var playerState = PlayerState.GetInstance();
            playerState.RegisterJobChangeHandler(OnJobChange!);
            playerState.RegisterLoginStateChangeHandler(OnLogin!);
            playerState.RegisterCombatStateChangeHandler(OnCombatChange!);

            actionTracker.OnGcdDropped += OnGcdDropped;
            actionTracker.OnLimitBreakCanceled += OnLimitBreakCanceled;
        }

        public void GoToNextRank(bool playSfx, bool loop, bool forceSfx = false)
        {
            if (CurrentRank?.Next == null)
            {
                if (Plugin.Configuration!.PlaySoundEffects && playSfx && forceSfx && CurrentRank != null)
                {
                    AudioService.PlaySfx(CurrentRank.Value.StyleType);
                }
                if (styles.Head != null && loop)
                {
                    Reset();
                }
                
            }
            else if (CurrentRank?.Next != null)
            {
                CurrentRank = CurrentRank.Next;
                Service.Log.Debug($"New rank reached {CurrentRank.Value.StyleType}");
                StyleRankChange?.Invoke(this, new(CurrentRank!.Previous!.Value, CurrentRank.Value, false));
                if (Plugin.Configuration!.PlaySoundEffects && playSfx)
                {
                    AudioService.PlaySfx(CurrentRank.Value.StyleType);
                }
            }
        }

        public void ReturnToPreviousRank(bool droppedGcd)
        {
            
            if (CurrentRank?.Previous == null)
            {
                if (droppedGcd)
                {
                    StyleRankChange?.Invoke(this, new(CurrentRank!.Value, CurrentRank.Value, droppedGcd));
                }
                return;
            }

            CurrentRank = CurrentRank.Previous;
            Service.Log.Debug($"Going back to rank {CurrentRank.Value.StyleType}");
            StyleRankChange?.Invoke(this, new(CurrentRank!.Next!.Value, CurrentRank.Value, droppedGcd));
        }

        public void Reset()
        {
            CurrentRank = styles.Head;
            StyleRankChange?.Invoke(this, new(null, CurrentRank!.Value, false));
        }

        public void ForceRankTo(StyleType type, bool isBlunder)
        {
            
            if (CurrentRank.Value.StyleType == type)
            {
                return;
            }
            var tempRank = CurrentRank;
            do
            {
                if (CurrentRank.Next != null)
                {
                    CurrentRank = CurrentRank.Next;
                }
                else
                {
                    CurrentRank = styles.Head;
                }
            } while(CurrentRank.Value.StyleType != type && CurrentRank.Value.StyleType != tempRank.Value.StyleType);

            if (isBlunder)
            {
                AudioService.PlaySfx(StyleType.DEAD_WEIGHT, true);
            }

            StyleRankChange?.Invoke(this, new(tempRank.Value, CurrentRank.Value, isBlunder));
        }

        private void ChangeStylesTo(DoubleLinkedList<StyleRank> newStyles)
        {
            styles = newStyles;
            Reset();
        }

        private void OnCombatChange(object send, bool enteringCombat)
        {
            if (enteringCombat)
            {
                Reset();
            }
        }
        private void OnJobChange(object sender, JobIds newJob)
        {
            ChangeStylesTo(DEFAULT_STYLE_RANK);
        }

        private void OnLogin(object send, bool loggedIn)
        {
            if (!loggedIn)
            {
                return;
            }
            var currentJob = JobHelper.GetCurrentJob();
            if (currentJob == JobIds.OTHER)
            {
                ChangeStylesTo(DEFAULT_STYLE_RANK);
                return;
            }
            ChangeStylesTo(DEFAULT_STYLE_RANK);

        }

        private void OnGcdDropped(object? sender, EventArgs args)
        {
            if (CurrentRank?.Value.StyleType != StyleType.NO_STYLE && Plugin.Configuration!.PlaySoundEffects)
            {
                AudioService.PlaySfx(StyleType.DEAD_WEIGHT);
            }
            ReturnToPreviousRank(true);
            
        }

        private void OnLimitBreakCanceled(object? sender, EventArgs args)
        {
            ForceRankTo(StyleType.D, true);
        }
    }
}
