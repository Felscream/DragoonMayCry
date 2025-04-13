using System.Collections.Generic;

namespace DragoonMayCry.Audio.BGM.FSM.States
{
    public interface IFsmState
    {
        public BgmState Id { get; }

        public Dictionary<string, string> GetBgmPaths();
        public void Enter(bool fromLoop);
        public void Update();

        public void Reset();
        public int Exit(ExitType exit);
        // Returns whether the transition can be cancelled or not
        public bool CancelExit();
    }
}
