using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DragoonMayCry.Audio.BGM
{
    public class BgmTrackData
    {
        public int EffectiveStart { get; private set; }
        public int TransitionStart { get; private set; }

        public BgmTrackData(int effectiveStart, int transitionStart)
        {
            EffectiveStart = effectiveStart;
            TransitionStart = transitionStart;
        }
    }
}
