#region

using System;

#endregion

namespace DragoonMayCry.State.Tracker
{
    public abstract class StateTracker<T>
    {
        public EventHandler<T>? OnChange;
        public T? LastValue { get; set; }
        public T? CurrentValue { get; set; }

        public abstract void Update(DmcPlayerState dmcPlayerState);
    }
}
