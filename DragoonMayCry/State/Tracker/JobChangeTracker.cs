using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DragoonMayCry.Data;
using DragoonMayCry.Util;

namespace DragoonMayCry.State.Tracker
{
    internal class JobChangeTracker : StateTracker<JobIds>
    {
        public JobIds CurrentJob { get; private set; } = JobIds.OTHER;
        public override void Update(PlayerState playerState)
        {
            
            if (playerState.Player == null || !playerState.IsLoggedIn)
            {
                CurrentJob = JobIds.OTHER;
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
