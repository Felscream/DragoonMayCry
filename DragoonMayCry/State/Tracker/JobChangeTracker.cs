#region

using DragoonMayCry.Data;
using DragoonMayCry.Util;

#endregion

namespace DragoonMayCry.State.Tracker
{
    internal class JobChangeTracker : StateTracker<JobId>
    {
        public JobId CurrentJob { get; private set; } = JobId.OTHER;

        public override void Update(DmcPlayerState dmcPlayerState)
        {
            if (dmcPlayerState.Player == null)
            {
                CurrentJob = JobId.OTHER;
                return;
            }

            var job = JobHelper.IdToJob(dmcPlayerState.Player.ClassJob.RowId);

            if (job != CurrentJob)
            {
                CurrentJob = job;
                OnChange?.Invoke(this, CurrentJob);
            }
        }
    }
}
