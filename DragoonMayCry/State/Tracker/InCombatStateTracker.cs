using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DragoonMayCry.State.Tracker
{
    internal class InCombatStateTracker : StateTracker<bool>
    {
        public override void Update(PlayerState playerState)
        {
            CurrentValue = playerState.IsInCombat;
            if (CurrentValue != LastValue)
            {
                OnChange?.Invoke(this, CurrentValue);
            }

            LastValue = CurrentValue;
        }
    }
}
