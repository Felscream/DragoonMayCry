using DragoonMayCry.Score.Model;
using System;
using System.Collections.Generic;

namespace DragoonMayCry.Score.Table
{
    class HealerScoringTable : ScoringTable
    {

        private readonly Dictionary<StyleType, StyleScoring>
                scoringTable = new Dictionary<StyleType, StyleScoring>
                {
                { StyleType.NoStyle, new StyleScoring(40000, 500, 0, 1) },
                { StyleType.D, new StyleScoring(40000, 3000, 4000, 1) },
                { StyleType.C, new StyleScoring(48000, 7000, 4800, 1) },
                { StyleType.B, new StyleScoring(54000, 8000, 5400, 1) },
                { StyleType.A, new StyleScoring(60000, 8000, 6000, 1) },
                { StyleType.S, new StyleScoring(60000, 12700, 6000, 0.75f) },
                { StyleType.SS, new StyleScoring(60000, 13000, 6000, 0.65f) },
                { StyleType.SSS, new StyleScoring(48000, 13000, 4800, 0.45f) },
                };

        private readonly Dictionary<StyleType, ScoringCoefficient>
            scoringCoefficient = new Dictionary<StyleType, ScoringCoefficient>
            {
                { StyleType.NoStyle, new ScoringCoefficient(4f, 0f) },
                { StyleType.D, new ScoringCoefficient(4.6f, 0.1f) },
                { StyleType.C, new ScoringCoefficient(5f, 0.26f) },
                { StyleType.B, new ScoringCoefficient(5f, 0.33f) },
                { StyleType.A, new ScoringCoefficient(5.4f, 0.4f) },
                { StyleType.S, new ScoringCoefficient(5f, 0.58f) },
                { StyleType.SS, new ScoringCoefficient(3.4f, 0.58f) },
                { StyleType.SSS, new ScoringCoefficient(3f, 0.5f) },
            };

        protected override Dictionary<StyleType, StyleScoring> RoleScoringTable => scoringTable;

        protected override Dictionary<StyleType, ScoringCoefficient> ScoringCoefficient => scoringCoefficient;

        protected override Dictionary<int, Dictionary<StyleType, StyleScoring>> Cache => cache;

        private readonly Dictionary<int, Dictionary<StyleType, StyleScoring>> cache = new Dictionary<int, Dictionary<StyleType, StyleScoring>>();

        protected override float GetDpsAtIlvl(int ilvl)
        {
            return (float)(39.1f * Math.Exp(0.0078f * ilvl));
        }
    }
}
