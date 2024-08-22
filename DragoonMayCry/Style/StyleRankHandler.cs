
using Dalamud.IoC;
using Dalamud.Plugin.Services;
using DragoonMayCry.Util;
using ImGuiScene;
using System.Collections.Generic;
using static Dalamud.Interface.Utility.Raii.ImRaii;

namespace DragoonMayCry.Style {

    public class StyleRankHandler {

        public DoubleNode<StyleRank> CurrentStyle { get; private set; }
        private readonly DoubleLinkedList<StyleRank> styles;

        public StyleRankHandler() {
            styles = new ();

            styles.Add(new (StyleType.NO_STYLE));
            styles.Add(new(StyleType.D));
            styles.Add(new(StyleType.C));
            styles.Add(new(StyleType.B));
            styles.Add(new(StyleType.A));
            styles.Add(new (StyleType.S));
            styles.Add(new(StyleType.SS));
            styles.Add(new(StyleType.SSS));

            CurrentStyle = styles.Head;

        }

        public StyleRank goToNextStyle()
        {
            if(CurrentStyle.Next == null && styles.Head != null) {
                CurrentStyle = styles.Head;
            } else if(CurrentStyle.Next != null) {
                CurrentStyle = CurrentStyle.Next;
            }
            Plugin.Log.Debug($"New rank reached {CurrentStyle.Value.StyleType}");
            return CurrentStyle.Value;
        }
    }
}
