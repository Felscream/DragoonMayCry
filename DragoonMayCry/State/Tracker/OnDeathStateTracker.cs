namespace DragoonMayCry.State.Tracker
{
    internal class OnDeathStateTracker : StateTracker<bool>
    {
        public override void Update(PlayerState playerState)
        {
            if (playerState.Player == null)
            {
                return;
            }

            CurrentValue = playerState.IsDead;
            if (CurrentValue != LastValue)
            {
                Service.Log.Debug("Player died");
                OnChange?.Invoke(this, CurrentValue);
            }
            LastValue = CurrentValue;
        }
    }
}
