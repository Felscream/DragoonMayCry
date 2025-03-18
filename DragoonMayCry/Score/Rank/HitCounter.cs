using DragoonMayCry.Score.Action;
using DragoonMayCry.Score.Model;
using DragoonMayCry.State;

namespace DragoonMayCry.Score.Rank;

public class HitCounter
{
    public uint HitCount { get; private set; }

    private readonly PlayerActionTracker playerActionTracker;
    private readonly PlayerState playerState;

    public HitCounter(PlayerActionTracker playerActionTracker)
    {
        this.playerActionTracker = playerActionTracker;
        this.playerActionTracker.ActionFlyTextCreated += (_, _) => HitCount++;
        this.playerActionTracker.GcdDropped += (_, _) => HitCount = 0;

        playerState = PlayerState.GetInstance();
        playerState.RegisterCombatStateChangeHandler((_, _) => HitCount = 0);
        playerState.RegisterDeathStateChangeHandler((_, _) => HitCount = 0);
    }
}
