using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace DragoonMayCry.Configuration
{
    public class FloatingWindowConfiguration
    {
        public Vector4 TextColor { get; set; } = new(255, 255, 255, 1);
        public Vector4 BackgroundColor { get; set; } = new(0, 0, 0, 0);
    }
}
