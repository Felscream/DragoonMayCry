namespace DragoonMayCry.State.Tracker
{
    internal class InCombatStateTracker : StateTracker<bool>
    {
        public override void Update(DmcPlayerState dmcPlayerState)
        {
            CurrentValue = dmcPlayerState.IsInCombat;
            if (CurrentValue != LastValue)
            {
                OnChange?.Invoke(this, CurrentValue);
            }

            LastValue = CurrentValue;
        }
    }
}
