using DragoonMayCry.Data;
using DragoonMayCry.State;

namespace DragoonMayCry.Score.Action.JobModule
{
    internal class JobModuleFactory
    {
        private readonly ScoreManager scoreManager;
        private readonly PlayerState playerState;
        public JobModuleFactory(ScoreManager scoreManager)
        {
            this.scoreManager = scoreManager;
            this.playerState = PlayerState.GetInstance();
        }

        public IJobActionModule? GetJobActionModule()
        {
            var job = playerState.GetCurrentJob();
            return job switch
            {
                JobId.AST => new AST(scoreManager),
                JobId.BRD => new BRD(scoreManager),
                JobId.SCH => new SCH(scoreManager),
                JobId.SGE => new SGE(scoreManager),
                JobId.WHM => new WHM(scoreManager),
                _ => null
            };
        }
    }
}
