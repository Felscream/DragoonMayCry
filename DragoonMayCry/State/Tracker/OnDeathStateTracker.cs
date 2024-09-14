using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DragoonMayCry.State.Tracker
{
    internal class OnDeathStateTracker : StateTracker<bool>
    {
        public override void Update(PlayerState playerState)
        {
            if (!playerState.IsInCombat)
            {
                return;
            }
            CurrentValue = playerState.IsDead;
            if (CurrentValue != LastValue)
            {
                OnChange?.Invoke(this, CurrentValue);
            }
            LastValue = CurrentValue;
        }
    }
}
