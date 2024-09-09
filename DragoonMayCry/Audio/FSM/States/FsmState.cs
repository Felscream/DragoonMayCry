using System;
using System.Collections.Generic;

namespace DragoonMayCry.Audio.FSM.States
{
    public interface FsmState
    {
        public BgmState ID { get; }

        public Dictionary<BgmId, string> GetBgmPaths();
        public void Enter(bool fromLoop);
        public void Update();

        public void Reset();
        public int Exit(ExitType exit);
        // Returns whether the transition can be cancelled or not
        public bool CancelExit();
    }
}
