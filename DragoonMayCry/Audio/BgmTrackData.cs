using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DragoonMayCry.Audio
{
    public class BgmTrackData
    {
        public double EffectiveStart { get; private set; }
        public double TransitionStart { get; private set; }

        public BgmTrackData(double effectiveStart, double transitionStart)
        {
            EffectiveStart = effectiveStart;
            TransitionStart = transitionStart;
        }
    }
}
