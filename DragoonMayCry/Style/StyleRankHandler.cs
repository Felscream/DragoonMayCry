
using System;
using DragoonMayCry.Audio;
using DragoonMayCry.Util;
using System.IO;
using DragoonMayCry.Data;
using DragoonMayCry.State;

namespace DragoonMayCry.Style
{
    public class StyleRankHandler {
        private static readonly DoubleLinkedList<StyleRank> DEFAULT_STYLE_RANK = new DoubleLinkedList<StyleRank>(
            new StyleRank(StyleType.NO_STYLE, null, null, 6000),
            new StyleRank(StyleType.D, "DragoonMayCry.Assets.D.png", GetPathToAudio("dirty"), 7000),
            new StyleRank(StyleType.C, "DragoonMayCry.Assets.C.png", GetPathToAudio("cruel"), 8000),
            new StyleRank(StyleType.B, "DragoonMayCry.Assets.B.png", GetPathToAudio("brutal"), 8500),
            new StyleRank(StyleType.A, "DragoonMayCry.Assets.A.png", GetPathToAudio("anarchic"), 11000),
            new StyleRank(StyleType.S, "DragoonMayCry.Assets.S.png", GetPathToAudio("savage"), 12000),
            new StyleRank(StyleType.SS, "DragoonMayCry.Assets.SS.png", GetPathToAudio("sadistic"), 13000),
            new StyleRank(StyleType.SSS, "DragoonMayCry.Assets.SSS.png", GetPathToAudio("sensational"), 14000));

        public EventHandler<StyleRank> OnStyleRankChange;
        public DoubleLinkedNode<StyleRank>? CurrentRank { get; private set; }
        private DoubleLinkedList<StyleRank> styles;
        private readonly AudioEngine audioEngine;

        public StyleRankHandler(PlayerState playerState)
        {
            ChangeStylesTo(DEFAULT_STYLE_RANK);
            audioEngine = new AudioEngine();
            audioEngine.Init(styles);
            
            playerState.RegisterJobChangeHandler(OnJobChange);
            playerState.RegisterLoginStateChangeHandler(OnLogin);
        }

        public void OnJobChange(object sender, JobIds newJob)
        {
            ChangeStylesTo(DEFAULT_STYLE_RANK);
        }

        public void OnLogin(object send, bool loggedIn)
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

        public void GoToNextRank(bool playSfx)
        {
            if (CurrentRank.Next == null && styles.Head != null) {
                Reset();
            } else if(CurrentRank.Next != null) {

                CurrentRank = CurrentRank.Next;
                Service.Log.Debug($"New rank reached {CurrentRank.Value.StyleType}");
                OnStyleRankChange?.Invoke(this, CurrentRank.Value);
                if (Plugin.Configuration.PlaySoundEffects)
                {
                    audioEngine.PlaySFX(CurrentRank.Value.StyleType);
                }
            }
        }

        public void ReturnToPreviousRank()
        {
            if (CurrentRank.Previous == null)
            {
                return;
            }

            CurrentRank = CurrentRank.Previous;
            OnStyleRankChange?.Invoke(this, CurrentRank.Value);
        }

        public void Reset()
        {
            CurrentRank = styles.Head;
            OnStyleRankChange?.Invoke(this, CurrentRank.Value);
        }

        public bool ReachedLastRank()
        {
            return CurrentRank.Next == null;
        }

        public StyleRank? GetPreviousStyleRank()
        {
            if (CurrentRank.Previous == null)
            {
                return null;
            }

            return CurrentRank.Previous.Value;
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
            CurrentRank = styles.Head;
            OnStyleRankChange?.Invoke(this, CurrentRank.Value);
        }
    }
}
