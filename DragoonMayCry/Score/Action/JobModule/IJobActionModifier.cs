namespace DragoonMayCry.Score.Action.JobModule
{
    internal interface IJobActionModifier
    {
        float OnAction(uint actionId);
        float OnActionAppliedOnTarget(uint actionId);
    }
}
