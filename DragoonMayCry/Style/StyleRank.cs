using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Interface.Textures.TextureWraps;
using ImGuiScene;

namespace DragoonMayCry.Style {
    
    public enum StyleType { 
        NO_STYLE,
        D,
        C,
        B,
        A,
        S,
        SS,
        SSS
    }

    public class StyleRank {
        public StyleType StyleType { get; init; }
        public string? IconPath { get; init; }
        public string? SfxPath { get; init; }
        
        public StyleRank(StyleType styleType, string iconPath, string sfxPath) {
            StyleType = styleType;
            IconPath = iconPath;
            SfxPath = sfxPath;
        }

        public override bool Equals(object? obj) {
            return obj is StyleRank rank &&
                   StyleType == rank.StyleType;
        }

        public override int GetHashCode() {
            return HashCode.Combine(StyleType);
        }
    }
}
