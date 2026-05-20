#region

using System;
using DragoonMayCry.Score.Action;
using DragoonMayCry.State;

#endregion

namespace DragoonMayCry.Score.Rank
{
    public class HitCounter : IDisposable
    {
        private readonly DmcPlayerState dmcPlayerState;

        private readonly PlayerActionTracker playerActionTracker;

        public HitCounter(PlayerActionTracker playerActionTracker)
        {
            this.playerActionTracker = playerActionTracker;
            this.playerActionTracker.ActionFlyTextCreated += OnFlyTextCreated;
            this.playerActionTracker.GcdDropped += OnResetEvent;

            dmcPlayerState = DmcPlayerState.GetInstance();
            dmcPlayerState.RegisterCombatStateChangeHandler(OnResetEventBool);
            dmcPlayerState.RegisterDeathStateChangeHandler(OnResetEventBool);
        }
        public uint HitCount { get; private set; }

        public void Dispose()
        {
            dmcPlayerState.UnregisterCombatStateChangeHandler(OnResetEventBool);
            dmcPlayerState.UnregisterDeathStateChangeHandler(OnResetEventBool);
            playerActionTracker.ActionFlyTextCreated -= OnFlyTextCreated;
            playerActionTracker.GcdDropped -= OnResetEvent;
        }

        private void OnFlyTextCreated(object? sender, EventArgs e) => HitCount++;

        private void OnResetEventBool(object? sender, bool e)
        {
            ResetHitCounter();
        }

        private void OnResetEvent(object? sender, EventArgs e)
        {
            ResetHitCounter();
        }

        private void ResetHitCounter()
        {
            HitCount = 0;
        }
    }
}
