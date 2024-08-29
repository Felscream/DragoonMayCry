namespace DragoonMayCry.State.Tracker
{
    internal class LoginStateTracker : StateTracker<bool>
    {
        public override void Update(PlayerState playerState)
        {
            CurrentValue = playerState.IsLoggedIn;
            if (CurrentValue && !LastValue)
            {
                Service.Log.Debug("Player logged in");
                OnChange?.Invoke(this, CurrentValue);
            }
            else if (!CurrentValue && LastValue)
            {
                Service.Log.Debug("Player logged out");
                OnChange?.Invoke(this, CurrentValue);
            }

            LastValue = CurrentValue;
        }
    }
}
