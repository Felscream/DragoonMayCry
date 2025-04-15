#region

using System;
using System.Collections.Generic;

#endregion

namespace DragoonMayCry.Audio.BGM.FSM.States
{
    internal abstract class VerseFsmState : IFsmState
    {

        public BgmState Id => BgmState.CombatLoop;
        public Dictionary<string, string> GetBgmPaths()
        {
            throw new NotImplementedException();
        }
        public void Enter(bool fromLoop)
        {
            throw new NotImplementedException();
        }
        public void Update()
        {
            throw new NotImplementedException();
        }
        public void Reset()
        {
            throw new NotImplementedException();
        }
        public int Exit(ExitType exit)
        {
            throw new NotImplementedException();
        }
        public bool CancelExit()
        {
            throw new NotImplementedException();
        }
    }
}
