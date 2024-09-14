namespace DragoonMayCry.State.Tracker
{
    internal class LoginStateTracker : StateTracker<bool>
    {
        public override void Update(PlayerState playerState)
        {
            CurrentValue = playerState.IsLoggedIn;
            if (CurrentValue != LastValue)
            {
                OnChange?.Invoke(this, CurrentValue);
            }

            LastValue = CurrentValue;
        }
    }
}
