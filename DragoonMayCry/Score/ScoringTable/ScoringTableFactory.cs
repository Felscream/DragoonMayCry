using DragoonMayCry.Data;
using DragoonMayCry.Score.Model;
using DragoonMayCry.Util;
using System.Collections.Generic;

namespace DragoonMayCry.Score.ScoringTable
{
    internal class ScoringTableFactory
    {
        public static readonly Dictionary<StyleType, StyleScoring>
            DefaultScoringTable = new()
            {
                { StyleType.NoStyle, new StyleScoring(60000, 500, 0, 1) },
                { StyleType.D, new StyleScoring(80000, 1000, 8000, 1) },
                { StyleType.C, new StyleScoring(90000, 5000, 9000, 1) },
                { StyleType.B, new StyleScoring(90000, 6000, 9000, 1) },
                { StyleType.A, new StyleScoring(100000, 8000, 10000, 1) },
                { StyleType.S, new StyleScoring(100000, 15000, 10000, 0.75f) },
                { StyleType.SS, new StyleScoring(70000, 15000, 7000, 0.65f) },
                { StyleType.SSS, new StyleScoring(60000, 13000, 6000, 0.3f) },
            };
        private readonly ScoringTable casterScoringTable = new CasterScoringTable();
        private readonly ScoringTable healerScoringTable = new HealerScoringTable();

        private readonly ScoringTable meleeScoringTable = new MeleeScoringTable();
        private readonly ScoringTable physRangeScoringTable = new PhysRangeScoringTable();
        private readonly ScoringTable tankScoringTable = new TankScoringTable();

        public Dictionary<StyleType, StyleScoring> GetScoringTable(int ilvl, JobId job)
        {
            if (!JobHelper.IsCombatJob(job))
            {
                return DefaultScoringTable;
            }

            if (JobHelper.IsTank(job))
            {
                return tankScoringTable.GetScoringTable(ilvl);
            }

            if (JobHelper.IsHealer(job))
            {
                return healerScoringTable.GetScoringTable(ilvl);
            }

            //MCH raw damage output comparable to casters
            if (JobHelper.IsCaster(job) || job == JobId.MCH)
            {
                //BLM damage output comparable to melee
                return job == JobId.BLM ?
                           meleeScoringTable.GetScoringTable(ilvl) :
                           casterScoringTable.GetScoringTable(ilvl);
            }

            if (JobHelper.IsPhysRange(job))
            {
                return physRangeScoringTable.GetScoringTable(ilvl);
            }

            return meleeScoringTable.GetScoringTable(ilvl);
        }
    }
}
