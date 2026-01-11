namespace DragoonMayCry.State.Tracker
{
    internal class OnDeathStateTracker : StateTracker<bool>
    {
        public override void Update(DmcPlayerState dmcPlayerState)
        {
            if (dmcPlayerState.Player == null)
            {
                return;
            }

            // using playerState.Player.IsDead triggers death
            // too early from the player's PoV
            CurrentValue = dmcPlayerState.Player.CurrentHp == 0;
            if (CurrentValue != LastValue)
            {
                OnChange?.Invoke(this, CurrentValue);
            }
            LastValue = CurrentValue;
        }
    }
}
