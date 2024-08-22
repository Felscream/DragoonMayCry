using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public TextureWrap? Icon { get; init; }
        public StyleType StyleType { get; init; }
        public StyleRank(StyleType styleType) {
            StyleType = styleType;
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
