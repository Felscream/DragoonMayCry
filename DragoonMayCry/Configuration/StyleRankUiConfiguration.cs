using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace DragoonMayCry.Configuration
{
    public class StyleRankUiConfiguration
    {
        public bool LockScoreWindow { get; set; } = true;
        public bool TestRankDisplay { get; set; } = false;
        public float DebugProgressValue { get; set; } = 0.5f;
        public Vector4 ProgressBarTint { get; set; } = Vector4.One;
    }
}
