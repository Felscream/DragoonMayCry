using Dalamud.Interface.Animation;
using System;

namespace DragoonMayCry.Util
{
    internal class EaseOutBack : Easing
    {
        public EaseOutBack(TimeSpan duration) : base(duration) { }
        public override void Update()
        {
            var c1 = 2.70158d;
            var c3 = c1 + 1d;
            var progress = Progress;
            ValueUnclamped = 1 + c3 * Math.Pow(progress - 1, 3) + c1 * Math.Pow(progress - 1, 2);
        }
    }
}
