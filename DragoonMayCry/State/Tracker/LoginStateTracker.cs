namespace DragoonMayCry.State.Tracker
{
    internal class LoginStateTracker : StateTracker<bool>
    {
        public override void Update(DmcPlayerState dmcPlayerState)
        {
            CurrentValue = dmcPlayerState.IsLoggedIn;
            if (CurrentValue != LastValue)
            {
                OnChange?.Invoke(this, CurrentValue);
            }

            LastValue = CurrentValue;
        }
    }
}
