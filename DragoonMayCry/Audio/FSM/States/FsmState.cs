using System;
using System.Collections.Generic;

namespace DragoonMayCry.Audio.FSM.States
{
    public interface FsmState
    {
        // The ID of the state.
        public BgmState ID { get; }

        public Dictionary<BgmId, string> GetBgmPaths();
        public void Enter(bool fromLoop);
        public void Update();

        public void Reset();
        public int Exit(ExitType exit);
    }
}
