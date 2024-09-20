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

            // using playerState.Player.IsDead triggers death
            // too early from the player's PoV
            CurrentValue = playerState.Player.CurrentHp == 0;
            if (CurrentValue != LastValue)
            {
                Service.Log.Debug("Player died");
                OnChange?.Invoke(this, CurrentValue);
            }
            LastValue = CurrentValue;
        }
    }
}
