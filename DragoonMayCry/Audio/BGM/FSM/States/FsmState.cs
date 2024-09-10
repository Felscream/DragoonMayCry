using System;
using System.Collections.Generic;
using DragoonMayCry.Audio.BGM;
using DragoonMayCry.Audio.BGM.FSM;

namespace DragoonMayCry.Audio.BGM.FSM.States
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
