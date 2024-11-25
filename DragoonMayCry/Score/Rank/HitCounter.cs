using DragoonMayCry.Score.Action;
using DragoonMayCry.Score.Model;
using DragoonMayCry.State;

namespace DragoonMayCry.Score.Rank;

public class HitCounter
{
    public uint HitCount { get; private set; }

    private readonly PlayerActionTracker playerActionTracker;
    private readonly StyleRankHandler styleRankHandler;
    private readonly PlayerState playerState;

    public HitCounter(
        PlayerActionTracker playerActionTracker, StyleRankHandler styleRankHandler)
    {
        this.playerActionTracker = playerActionTracker;
        this.playerActionTracker.ActionFlyTextCreated += (sender, args) => HitCount++;

        this.styleRankHandler = styleRankHandler;
        this.styleRankHandler.StyleRankChange += OnRankChange;

        playerState = PlayerState.GetInstance();
        playerState.RegisterCombatStateChangeHandler(OnCombat);
    }

    private void OnCombat(object? sender, bool enteredCombat)
    {
        HitCount = 0;
    }

    private void OnRankChange(object? sender, StyleRankHandler.RankChangeData rankChangeData)
    {
        if (rankChangeData.NewRank > rankChangeData.PreviousRank || rankChangeData.NewRank > StyleType.A)
        {
            return;
        }

        HitCount = 0;
    }
}
