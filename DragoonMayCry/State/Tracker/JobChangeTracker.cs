using DragoonMayCry.Data;
using DragoonMayCry.Util;

namespace DragoonMayCry.State.Tracker
{
    internal class JobChangeTracker : StateTracker<JobId>
    {
        public JobId CurrentJob { get; private set; } = JobId.OTHER;

        public override void Update(PlayerState playerState)
        {
            if (playerState.Player == null)
            {
                CurrentJob = JobId.OTHER;
                return;
            }

            var job = JobHelper.IdToJob(playerState.Player.ClassJob.RowId);

            if (job != CurrentJob)
            {
                CurrentJob = job;
                OnChange?.Invoke(this, CurrentJob);
            }
        }
    }
}
