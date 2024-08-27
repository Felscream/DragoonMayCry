using DragoonMayCry.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.JobGauge.Types;
using static FFXIVClientStructs.FFXIV.Client.Game.Character.VfxContainer;

namespace DragoonMayCry.Util
{
    public class JobHelper
    {
        
        public static JobIds IdToJob(uint job) => job < 19 ? JobIds.OTHER : (JobIds)job;

        private static readonly ISet<JobIds> Tanks = new HashSet<JobIds>
        {
            JobIds.GNB, JobIds.DRK, JobIds.PLD, JobIds.WAR
        };

        private static readonly ISet<JobIds> Healers = new HashSet<JobIds>
        {
            JobIds.AST, JobIds.WHM, JobIds.SCH, JobIds.SGE
        };

        public static JobIds GetCurrentJob()
        {
            if (Service.ClientState.LocalPlayer == null)
            {
                Service.Log.Debug("Cannot find current player job, character not found");
                return JobIds.OTHER;
            }

            return IdToJob(Service.ClientState.LocalPlayer.ClassJob.Id);
        }

        public static bool IsTank(JobIds job)
        {
            return Tanks.Contains(job);
        }

        public static bool IsTank()
        {
            return IsTank(GetCurrentJob());
        }

        public static bool IsHealer(JobIds job)
        {
            return Healers.Contains(job);
        }

        public static bool IsDps(JobIds job)
        {
            return job != JobIds.OTHER && !IsTank(job) && !IsHealer(job);
        }

        public static bool IsCombatJob(JobIds job)
        {
            return job != JobIds.OTHER;
        }

        public static bool IsCombatJob()
        {
            return IsCombatJob(GetCurrentJob());
        }
    }
}
