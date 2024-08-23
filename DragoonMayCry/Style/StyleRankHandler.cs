
using DragoonMayCry.Audio;
using DragoonMayCry.Util;
using System.IO;

namespace DragoonMayCry.Style
{

    public class StyleRankHandler {

        public DoubleLinkedNode<StyleRank> CurrentRank { get; private set; }
        private readonly DoubleLinkedList<StyleRank> styles;
        private readonly AudioEngine audioEngine;

        public StyleRankHandler()
        {
            styles = new ();
            styles.Add(new (StyleType.NO_STYLE, null, null));
            styles.Add(new(StyleType.D,  "DragoonMayCry.Assets.D.png", GetPathToAudio("dirty")));
            styles.Add(new(StyleType.C, "DragoonMayCry.Assets.C.png", GetPathToAudio("cruel")));
            styles.Add(new(StyleType.B, "DragoonMayCry.Assets.B.png", GetPathToAudio("brutal")));
            styles.Add(new(StyleType.A, "DragoonMayCry.Assets.A.png", GetPathToAudio("anarchic")));
            styles.Add(new (StyleType.S, "DragoonMayCry.Assets.S.png", GetPathToAudio("savage")));
            styles.Add(new(StyleType.SS, "DragoonMayCry.Assets.SS.png", GetPathToAudio("sadistic")));
            styles.Add(new(StyleType.SSS, "DragoonMayCry.Assets.SSS.png", GetPathToAudio("sensational")));

            CurrentRank = styles.Head;

            audioEngine = new AudioEngine();
            audioEngine.Init(styles);

        }

        public StyleRank GoToNextRank(bool playSfx)
        {
            
            if (CurrentRank.Next == null && styles.Head != null) {
                CurrentRank = styles.Head;
            } else if(CurrentRank.Next != null) {
                CurrentRank = CurrentRank.Next;
            }
            Service.Log.Debug($"New rank reached {CurrentRank.Value.StyleType}");
            if (Plugin.Configuration.PlaySoundEffects)
            {
                audioEngine.PlaySFX(CurrentRank.Value.StyleType);
            }
            
            return CurrentRank.Value;
        }

        public void Reset()
        {
            CurrentRank = styles.Head;
        }

        public bool ReachedLastRank()
        {
            return CurrentRank.Next == null;
        }

        private string GetPathToAudio(string name)
        {
            return Path.Combine(
                Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!,
                $"Assets\\Audio\\{name}.wav");
        }
    }
}
