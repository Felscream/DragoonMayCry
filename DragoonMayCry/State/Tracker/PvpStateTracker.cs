namespace DragoonMayCry.State.Tracker
{
    internal class PvpStateTracker : StateTracker<bool>
    {
        public override void Update(DmcPlayerState dmcPlayerState)
        {
            if (dmcPlayerState.Player == null)
            {
                return;
            }
            CurrentValue = dmcPlayerState.IsInPvp();
            if (CurrentValue != LastValue)
            {
                OnChange?.Invoke(this, CurrentValue);
            }
            LastValue = CurrentValue;
        }
    }
}
