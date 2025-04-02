using DragoonMayCry.Score.Model;
using System;
using System.Collections.Generic;

namespace DragoonMayCry.Score.ScoringTable
{
    class MeleeScoringTable : ScoringTable
    {
        private readonly Dictionary<StyleType, StyleScoring>
            meleeScoring = new()
            {
                { StyleType.NoStyle, new StyleScoring(60000, 500, 0, 1) },
                { StyleType.D, new StyleScoring(80000, 1000, 8000, 1) },
                { StyleType.C, new StyleScoring(90000, 5000, 9000, 1) },
                { StyleType.B, new StyleScoring(90000, 6000, 9000, 1) },
                { StyleType.A, new StyleScoring(100000, 8000, 10000, 0.97f) },
                { StyleType.S, new StyleScoring(100000, 15000, 10000, 0.85f) },
                { StyleType.SS, new StyleScoring(70000, 15000, 7000, 0.75f) },
                { StyleType.SSS, new StyleScoring(60000, 13000, 6000, 0.4f) },
            };

        private readonly Dictionary<StyleType, ScoringCoefficient>
            meleeScoringCoefficient = new()
            {
                { StyleType.NoStyle, new ScoringCoefficient(3f, 0.05f) },
                { StyleType.D, new ScoringCoefficient(6.6f, 0.1f) },
                { StyleType.C, new ScoringCoefficient(7.4f, 0.26f) },
                { StyleType.B, new ScoringCoefficient(8f, 0.33f) },
                { StyleType.A, new ScoringCoefficient(11.8f, 0.38f) },
                { StyleType.S, new ScoringCoefficient(5.5f, 0.42f) },
                { StyleType.SS, new ScoringCoefficient(4.5f, 0.5f) },
                { StyleType.SSS, new ScoringCoefficient(3f, 0.56f) },
            };

        private readonly Dictionary<StyleType, ScoringCoefficient>
            emdScoringCoefficient = new()
            {
                { StyleType.NoStyle, new ScoringCoefficient(3f, 0.05f) },
                { StyleType.D, new ScoringCoefficient(7.92f, 0.1f) },
                { StyleType.C, new ScoringCoefficient(8.88f, 0.26f) },
                { StyleType.B, new ScoringCoefficient(9.6f, 0.33f) },
                { StyleType.A, new ScoringCoefficient(14.16f, 0.38f) },
                { StyleType.S, new ScoringCoefficient(6.5f, 0.42f) },
                { StyleType.SS, new ScoringCoefficient(5.2f, 0.5f) },
                { StyleType.SSS, new ScoringCoefficient(4f, 0.56f) },
            };
        protected override Dictionary<StyleType, ScoringCoefficient> EmdScoringCoefficient => emdScoringCoefficient;
        protected override Dictionary<StyleType, StyleScoring> RoleScoringTable => meleeScoring;
        protected override Dictionary<StyleType, ScoringCoefficient> ScoringCoefficient => meleeScoringCoefficient;

        protected override float GetDpsAtIlvl(int ilvl)
        {
            return (float)(75.9f * Math.Exp(0.00801f * ilvl));
        }
    }
}
