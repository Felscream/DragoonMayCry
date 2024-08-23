using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DragoonMayCry.State
{
    public abstract class StateTracker<T>
    {
        public EventHandler<T> OnChange;
        public T LastValue { get; set; }
        public T CurrentValue { get; set; }

        public abstract void Update(PlayerState playerState);
    }
}
