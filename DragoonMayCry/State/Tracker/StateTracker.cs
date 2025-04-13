using System;

namespace DragoonMayCry.State.Tracker
{
    public abstract class StateTracker<T>
    {
        public EventHandler<T>? OnChange;
        public T? LastValue { get; set; }
        public T? CurrentValue { get; set; }

        public abstract void Update(PlayerState playerState);
    }
}
