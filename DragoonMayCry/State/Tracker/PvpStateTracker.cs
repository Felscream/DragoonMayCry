using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DragoonMayCry.State.Tracker
{
    internal class PvpStateTracker : StateTracker<bool>
    {
        public override void Update(PlayerState playerState)
        {
            if(playerState.Player == null)
            {
                return;
            }
            CurrentValue = playerState.IsInPvp();
            if(CurrentValue != LastValue)
            {
                OnChange?.Invoke(this, CurrentValue);
            }
            LastValue = CurrentValue;
        }
    }
}
