
using System.IO;
using DragoonMayCry.Audio;
using DragoonMayCry.Util;

namespace DragoonMayCry.Style {

    public class StyleRankHandler {

        public DoubleLinkedNode<StyleRank> CurrentStyle { get; private set; }
        private readonly DoubleLinkedList<StyleRank> styles;
        private readonly AudioHandler audioHandler;

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

            CurrentStyle = styles.Head;

            audioHandler = new AudioHandler();
            audioHandler.Init(styles);

        }

        public StyleRank GoToNextRank(bool playSfx)
        {
            
            if (CurrentStyle.Next == null && styles.Head != null) {
                CurrentStyle = styles.Head;
            } else if(CurrentStyle.Next != null) {
                CurrentStyle = CurrentStyle.Next;
            }
            Service.Log.Debug($"New rank reached {CurrentStyle.Value.StyleType}");
            audioHandler.PlaySFX(CurrentStyle.Value.StyleType);
            return CurrentStyle.Value;
        }

        public void Reset()
        {
            CurrentStyle = styles.Head;
        }

        private string GetPathToAudio(string name)
        {
            return Path.Combine(
                Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!,
                $"Assets\\Audio\\{name}.wav");
        }
    }
}
