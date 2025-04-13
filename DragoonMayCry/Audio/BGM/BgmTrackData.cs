using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DragoonMayCry.Audio.BGM
{
    public class BgmTrackData(string audioPath, int effectiveStart, int transitionStart, int possibleTransitionTimeToNewState = int.MaxValue)
    {
        public string AudioPath => audioPath;
        public int EffectiveStart { get; private set; } = effectiveStart;
        public int TransitionStart { get; private set; } = transitionStart;
        public int PossibleTransitionTimeToNewState { get; private set; } = possibleTransitionTimeToNewState;
    }
}
