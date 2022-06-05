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

        public StyleRank? NextStyle { get; set; }
        public StyleRank? PreviousStyle { get; init; }
        public float PointsPerAction { get; init; }
        public double SafeGuardDuration { get; init; }
        public float SpamMalusMultiplier { get; init; }
        public StyleRank(TextureWrap? icon, StyleType styleType, StyleRank? previousStyle, float points, double safeGuard, float spamMalus) {
            Icon = icon;
            StyleType = styleType;
            PreviousStyle = previousStyle;
            PointsPerAction = points;
            SafeGuardDuration = safeGuard;
            SpamMalusMultiplier = spamMalus;
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
