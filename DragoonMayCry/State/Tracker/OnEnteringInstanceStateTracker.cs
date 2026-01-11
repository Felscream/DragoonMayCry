namespace DragoonMayCry.State.Tracker
{
    internal class OnEnteringInstanceStateTracker : StateTracker<bool>
    {
        public override void Update(DmcPlayerState dmcPlayerState)
        {
            CurrentValue = dmcPlayerState.IsInsideInstance;
            if (CurrentValue != LastValue)
            {
                OnChange?.Invoke(this, CurrentValue);
            }
            LastValue = CurrentValue;
        }
    }
}
