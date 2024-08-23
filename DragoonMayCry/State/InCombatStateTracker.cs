using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DragoonMayCry.State
{
    internal class InCombatStateTracker : StateTracker<bool>
    {
        public override void Update(PlayerState playerState)
        {
            CurrentValue = playerState.IsInCombat;
            if (CurrentValue && !LastValue)
            {
                Service.Log.Debug("Entered combat");
                OnChange?.Invoke(this, CurrentValue);
            }
            else if (!CurrentValue && LastValue){
                Service.Log.Debug("Exited combat");
                OnChange?.Invoke(this, CurrentValue);
            }

            LastValue = CurrentValue;
        }
    }
}
