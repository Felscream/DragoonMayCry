using Dalamud.Interface.Animation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DragoonMayCry.Util
{
    internal class EaseOutBack : Easing
    {
        public EaseOutBack(TimeSpan duration):base(duration) { }
        public override void Update()
        {
            var c1 = 1.70158d;
            var c3 = c1 + 1d;
            double progress = base.Progress;
            base.Value = 1 + c3 * Math.Pow(progress - 1, 3) + c1 * Math.Pow(progress - 1, 2);
        }
    }
}
