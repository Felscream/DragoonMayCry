using DragoonMayCry.Score.Model;
using System;
using System.Collections.Generic;

namespace DragoonMayCry.Score.Table
{
    class MeleeScoringTable : ScoringTable
    {
        private readonly Dictionary<StyleType, StyleScoring>
            meleeScoring = new Dictionary<StyleType, StyleScoring>
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

        private readonly Dictionary<StyleType, ScoringCoefficient>
            meleeScoringCoefficient = new Dictionary<StyleType, ScoringCoefficient>
            {
                { StyleType.NoStyle, new ScoringCoefficient(4f, 0.05f) },
                { StyleType.D, new ScoringCoefficient(4.6f, 0.1f) },
                { StyleType.C, new ScoringCoefficient(5f, 0.26f) },
                { StyleType.B, new ScoringCoefficient(5f, 0.33f) },
                { StyleType.A, new ScoringCoefficient(5.4f, 0.4f) },
                { StyleType.S, new ScoringCoefficient(5f, 0.58f) },
                { StyleType.SS, new ScoringCoefficient(3.4f, 0.58f) },
                { StyleType.SSS, new ScoringCoefficient(3f, 0.5f) },
            };
        protected override Dictionary<StyleType, StyleScoring> RoleScoringTable => meleeScoring;
        protected override Dictionary<StyleType, ScoringCoefficient> ScoringCoefficient => meleeScoringCoefficient;
        protected override Dictionary<int, Dictionary<StyleType, StyleScoring>> Cache => cache;
        private readonly Dictionary<int, Dictionary<StyleType, StyleScoring>> cache = new Dictionary<int, Dictionary<StyleType, StyleScoring>>();

        protected override float GetDpsAtIlvl(int ilvl)
        {
            return (float)(76.7f * Math.Exp(0.00799f * ilvl));
        }
    }
}
