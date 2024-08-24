
using System;
using DragoonMayCry.Audio;
using DragoonMayCry.Util;
using System.IO;
using DragoonMayCry.Data;
using DragoonMayCry.State;
using DragoonMayCry.Score;
using static FFXIVClientStructs.FFXIV.Client.UI.RaptureAtkHistory.Delegates;

namespace DragoonMayCry.Style
{
    public class StyleRankHandler {
        private static readonly DoubleLinkedList<StyleRank> DEFAULT_STYLE_RANK = new DoubleLinkedList<StyleRank>(
            new StyleRank(StyleType.NO_STYLE, null, null, 6000, 500),
            new StyleRank(StyleType.D, "DragoonMayCry.Assets.D.png", GetPathToAudio("dirty"), 13000, 1000),
            new StyleRank(StyleType.C, "DragoonMayCry.Assets.C.png", GetPathToAudio("cruel"), 20000, 2000),
            new StyleRank(StyleType.B, "DragoonMayCry.Assets.B.png", GetPathToAudio("brutal"), 32000, 3900),
            new StyleRank(StyleType.A, "DragoonMayCry.Assets.A.png", GetPathToAudio("anarchic"), 48000, 4700),
            new StyleRank(StyleType.S, "DragoonMayCry.Assets.S.png", GetPathToAudio("savage"), 56000, 5500),
            new StyleRank(StyleType.SS, "DragoonMayCry.Assets.SS.png", GetPathToAudio("sadistic"), 65000, 8000),
            new StyleRank(StyleType.SSS, "DragoonMayCry.Assets.SSS.png", GetPathToAudio("sensational"), 80000, 10000));

        public EventHandler<StyleRank> OnStyleRankChange;
        public DoubleLinkedNode<StyleRank>? CurrentRank { get; private set; }
        public DoubleLinkedNode<StyleRank>? PreviousRank { get; private set; }
        private DoubleLinkedList<StyleRank> styles;
        private readonly AudioEngine audioEngine;

        public StyleRankHandler(PlayerState playerState)
        {
            ChangeStylesTo(DEFAULT_STYLE_RANK);
            audioEngine = new AudioEngine();
            audioEngine.Init(styles);

            playerState.RegisterJobChangeHandler(OnJobChange);
            playerState.RegisterLoginStateChangeHandler(OnLogin);
            playerState.RegisterCombatStateChangeHandler(OnCombatChange);
        }

        public void GoToNextRank(bool playSfx, bool loop)
        {
            if (CurrentRank.Next == null && styles.Head != null && loop) {
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
            Service.Log.Debug($"Going back to rank {CurrentRank.Value.StyleType}");
            OnStyleRankChange?.Invoke(this, CurrentRank.Value);
        }

        public void Reset()
        {
            CurrentRank = styles.Head;
            OnStyleRankChange?.Invoke(this, CurrentRank.Value);
        }

        public void GoToLastStyleNode()
        {
            CurrentRank = styles.Tail;
            OnStyleRankChange?.Invoke(this, CurrentRank.Value);
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
    }
}
