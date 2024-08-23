using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DragoonMayCry.State.Tracker
{
    internal class OnEnteringInstanceStateTracker : StateTracker<bool>
    {
        public override void Update(PlayerState playerState)
        {
            CurrentValue = playerState.IsInsideInstance;
            if (CurrentValue && !LastValue)
            {
                Service.Log.Debug("Player entered instance");
                OnChange?.Invoke(this, CurrentValue);
            }
            else if (!CurrentValue && LastValue)
            {
                Service.Log.Debug("Player left instance");
                OnChange?.Invoke(this, CurrentValue);
            }

            LastValue = CurrentValue;
        }
    }
}
