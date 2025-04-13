using DragoonMayCry.Data;
using System.Collections.Generic;

namespace DragoonMayCry.Util
{
    public class JobHelper
    {

        private static readonly ISet<JobId> Tanks = new HashSet<JobId>
        {
            JobId.GNB, JobId.DRK, JobId.PLD, JobId.WAR,
        };

        private static readonly ISet<JobId> Healers = new HashSet<JobId>
        {
            JobId.AST, JobId.WHM, JobId.SCH, JobId.SGE,
        };

        private static readonly ISet<JobId> PhysRanges = new HashSet<JobId>
        {
            JobId.BRD, JobId.DNC, JobId.MCH,
        };

        private static readonly ISet<JobId> Casters = new HashSet<JobId>
        {
            JobId.RDM, JobId.SMN, JobId.BLM, JobId.PCT,
        };
        public static JobId IdToJob(uint job)
        {
            return job < 19 ? JobId.OTHER : (JobId)job;
        }

        public static bool IsTank(JobId job)
        {
            return Tanks.Contains(job);
        }

        public static bool IsHealer(JobId job)
        {
            return Healers.Contains(job);
        }

        public static bool IsDps(JobId job)
        {
            return job != JobId.OTHER && !IsTank(job) && !IsHealer(job);
        }

        public static bool IsPhysRange(JobId job)
        {
            return PhysRanges.Contains(job);
        }

        public static bool IsCaster(JobId job)
        {
            return Casters.Contains(job);
        }

        public static bool IsMelee(JobId job)
        {
            return job != JobId.OTHER
                   && !IsTank(job)
                   && !IsHealer(job)
                   && !IsCaster(job)
                   && !IsPhysRange(job);
        }

        public static bool IsCombatJob(JobId job)
        {
            return job != JobId.OTHER;
        }
    }
}
