namespace DragoonMayCry.State.Tracker
{
    internal class PvpStateTracker : StateTracker<bool>
    {
        public override void Update(PlayerState playerState)
        {
            if (playerState.Player == null)
            {
                return;
            }
            CurrentValue = playerState.IsInPvp();
            if (CurrentValue != LastValue)
            {
                OnChange?.Invoke(this, CurrentValue);
            }
            LastValue = CurrentValue;
        }
    }
}
