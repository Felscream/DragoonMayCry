#region

using DragoonMayCry.Score.Action;
using DragoonMayCry.State;

#endregion

namespace DragoonMayCry.Score.Rank
{
    public class HitCounter
    {
        private readonly DmcPlayerState dmcPlayerState;

        private readonly PlayerActionTracker playerActionTracker;

        public HitCounter(PlayerActionTracker playerActionTracker)
        {
            this.playerActionTracker = playerActionTracker;
            this.playerActionTracker.ActionFlyTextCreated += (_, _) => HitCount++;
            this.playerActionTracker.GcdDropped += (_, _) => HitCount = 0;

            dmcPlayerState = DmcPlayerState.GetInstance();
            dmcPlayerState.RegisterCombatStateChangeHandler((_, _) => HitCount = 0);
            dmcPlayerState.RegisterDeathStateChangeHandler((_, _) => HitCount = 0);
        }
        public uint HitCount { get; private set; }
    }
}
