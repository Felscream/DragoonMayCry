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
        public JobIds CurrentJob { get; private set; }
        public override void Update(PlayerState playerState)
        {
            if (!playerState.IsLoggedIn)
            {
                return;
            }
            var job = JobHelper.IdToJob(Service.ClientState.LocalPlayer.ClassJob.Id);
            if (job != CurrentJob)
            {
                CurrentJob = job;
                Service.Log.Debug($"Detected job change to {CurrentJob}");
                OnChange?.Invoke(this, CurrentJob);
            }
        }
    }
}
