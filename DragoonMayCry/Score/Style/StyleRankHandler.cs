
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
            new StyleRank(StyleType.NO_STYLE, null, null, 60000, 500, new(135, 135, 135)),
            new StyleRank(StyleType.D, "DragoonMayCry.Assets.D.png", GetPathToAudio("dirty"), 80000, 1000, new(223, 152, 30)),
            new StyleRank(StyleType.C, "DragoonMayCry.Assets.C.png", GetPathToAudio("cruel"), 90000, 1500, new(95, 160, 213)),
            new StyleRank(StyleType.B, "DragoonMayCry.Assets.B.png", GetPathToAudio("brutal"), 90000, 2000, new(95, 160, 213)),
            new StyleRank(StyleType.A, "DragoonMayCry.Assets.A.png", GetPathToAudio("anarchic"), 100000, 4000, new(95, 160, 213)),
            new StyleRank(StyleType.S, "DragoonMayCry.Assets.S.png", GetPathToAudio("savage"), 100000, 8000, new(233, 216, 95)),
            new StyleRank(StyleType.SS, "DragoonMayCry.Assets.SS.png", GetPathToAudio("sadistic"), 100000, 10000, new(233, 216, 95)),
            new StyleRank(StyleType.SSS, "DragoonMayCry.Assets.SSS.png", GetPathToAudio("sensational"), 80000, 12000, new(233, 216, 95)));

        public EventHandler<RankChangeData>? StyleRankChange;
        public DoubleLinkedNode<StyleRank>? CurrentRank { get; private set; }
        public DoubleLinkedNode<StyleRank>? PreviousRank { get; private set; }
        private DoubleLinkedList<StyleRank> styles;
        private readonly AudioEngine audioEngine;

        public StyleRankHandler(ActionTracker actionTracker)
        {
            styles = DEFAULT_STYLE_RANK;
            Reset();
            audioEngine = new AudioEngine();
            audioEngine.Init(styles!);
            audioEngine.AddSfx(StyleType.DEAD_WEIGHT, GetPathToAudio("dead_weight"));

            var playerState = PlayerState.GetInstance();
            playerState.RegisterJobChangeHandler(OnJobChange!);
            playerState.RegisterLoginStateChangeHandler(OnLogin!);
            playerState.RegisterCombatStateChangeHandler(OnCombatChange!);

            actionTracker.OnGcdDropped += OnGcdDropped;
        }

        public void GoToNextRank(bool playSfx, bool loop)
        {
            if (CurrentRank?.Next == null && styles.Head != null && loop)
            {
                Reset();
            }
            else if (CurrentRank?.Next != null)
            {
                CurrentRank = CurrentRank.Next;
                Service.Log.Debug($"New rank reached {CurrentRank.Value.StyleType}");
                StyleRankChange?.Invoke(this, new(CurrentRank!.Previous!.Value, CurrentRank.Value, false));
                if (Plugin.Configuration!.PlaySoundEffects)
                {
                    audioEngine.PlaySfx(CurrentRank.Value.StyleType);
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

        private static string GetPathToAudio(string name)
        {
            return Path.Combine(
                Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!,
                $"Assets\\Audio\\{name}.wav");
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
            ReturnToPreviousRank(true);
            if (Plugin.Configuration!.PlaySoundEffects)
            {
                audioEngine.PlaySfx(StyleType.DEAD_WEIGHT);
            }
        }
    }
}
