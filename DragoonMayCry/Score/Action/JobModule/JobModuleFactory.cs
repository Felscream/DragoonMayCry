#region

using DragoonMayCry.Data;
using DragoonMayCry.State;

#endregion

namespace DragoonMayCry.Score.Action.JobModule
{
    internal class JobModuleFactory(ScoreManager scoreManager)
    {
        private readonly DmcPlayerState dmcPlayerState = DmcPlayerState.GetInstance();

        public IJobActionModifier? GetJobActionModule()
        {
            var job = dmcPlayerState.GetCurrentJob();
            return job switch
            {
                JobId.AST => new AST(scoreManager),
                JobId.BRD => new BRD(scoreManager),
                JobId.SCH => new SCH(scoreManager),
                JobId.SGE => new SGE(scoreManager),
                JobId.WHM => new WHM(scoreManager),
                _ => null,
            };
        }
    }
}
