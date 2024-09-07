using System.Collections.Generic;

namespace DragoonMayCry.Audio.FSM.States
{
    public interface FsmState
    {
        // The name for the state.
        public string Name { get; set; }
        // The ID of the state.
        public BgmState ID { get; set; }

        public Dictionary<BgmId, string> GetBgmPaths();

        public void Start();
        public void Update();

        public void Reset();
    }
}
