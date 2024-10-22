using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            
            var job = JobHelper.IdToJob(playerState.Player.ClassJob.Id);
            
            if (job != CurrentJob)
            {
                CurrentJob = job;
                OnChange?.Invoke(this, CurrentJob);
            }
        }
    }
}
