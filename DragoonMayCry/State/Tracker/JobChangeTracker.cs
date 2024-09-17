using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using DragoonMayCry.Data;
using DragoonMayCry.Util;

namespace DragoonMayCry.State.Tracker
{
    internal class JobChangeTracker: StateTracker<JobIds>, IDisposable
    {
        internal JobIds CurrentJob { get; private set; } = JobIds.OTHER;
        private readonly IClientState clientState;
        private bool initialized;

        public JobChangeTracker(IClientState clientState) { 
            this.clientState = clientState;
            this.clientState.ClassJobChanged += OnJobChange;
        }

        private void OnJobChange(uint jobId)
        {
            JobIds job = JobHelper.IdToJob(jobId);
            CurrentJob = job;
            OnChange?.Invoke(this, job);
        }
        public override void Update(PlayerState playerState)
        {
            if(initialized || playerState.Player == null)
            {
                return;
            }
            CurrentJob = JobHelper.IdToJob(playerState.Player.ClassJob.Id);
            OnChange?.Invoke(this, CurrentJob);
            initialized = true;
        }

        public void Dispose()
        {
            this.clientState.ClassJobChanged -= OnJobChange;
        }
    }
}
