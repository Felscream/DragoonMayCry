using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DragoonMayCry.Score.Style;

namespace DragoonMayCry.Score
{
    static class ScoringTable
    {
        public static readonly  Dictionary<StyleType, ScoreManager.StyleScoring>
            MeleeScoringTable = new Dictionary<StyleType, ScoreManager.StyleScoring>
            {
                { StyleType.NoStyle, new ScoreManager.StyleScoring(60000, 500, 0, 1) },
                { StyleType.D, new ScoreManager.StyleScoring(80000, 1000, 8000, 1) },
                { StyleType.C, new ScoreManager.StyleScoring(90000, 5000, 9000, 1) },
                { StyleType.B, new ScoreManager.StyleScoring(90000, 6000, 9000, 1) },
                { StyleType.A, new ScoreManager.StyleScoring(100000, 8000, 10000, 1) },
                { StyleType.S, new ScoreManager.StyleScoring(100000, 15000, 10000, 0.75f) },
                { StyleType.SS, new ScoreManager.StyleScoring(70000, 15000, 7000, 0.65f) },
                { StyleType.SSS, new ScoreManager.StyleScoring(60000, 13000, 6000, 0.3f) },
            };

        public static readonly Dictionary<StyleType, ScoreManager.StyleScoring>
            TankScoringTable = new Dictionary<StyleType, ScoreManager.StyleScoring>
            {
                { StyleType.NoStyle, new ScoreManager.StyleScoring(40000, 500, 0, 1) },
                { StyleType.D, new ScoreManager.StyleScoring(50000, 3000, 4000, 1) },
                { StyleType.C, new ScoreManager.StyleScoring(58000, 7000, 4800, 1) },
                { StyleType.B, new ScoreManager.StyleScoring(58000, 8000, 5400, 1) },
                { StyleType.A, new ScoreManager.StyleScoring(60000, 8000, 6000, 1) },
                { StyleType.S, new ScoreManager.StyleScoring(60000, 10000, 6000, 0.75f) },
                { StyleType.SS, new ScoreManager.StyleScoring(60000, 13000, 6000, 0.65f) },
                { StyleType.SSS, new ScoreManager.StyleScoring(48000, 15000, 4800, 0.45f) },
            };

        public static readonly Dictionary<StyleType, ScoreManager.StyleScoring>
            HealerScoringTable = new Dictionary<StyleType, ScoreManager.StyleScoring>
            {
                { StyleType.NoStyle, new ScoreManager.StyleScoring(40000, 500, 0, 1) },
                { StyleType.D, new ScoreManager.StyleScoring(40000, 3000, 4000, 1) },
                { StyleType.C, new ScoreManager.StyleScoring(48000, 7000, 4800, 1) },
                { StyleType.B, new ScoreManager.StyleScoring(54000, 8000, 5400, 1) },
                { StyleType.A, new ScoreManager.StyleScoring(60000, 8000, 6000, 1) },
                { StyleType.S, new ScoreManager.StyleScoring(60000, 12700, 6000, 0.75f) },
                { StyleType.SS, new ScoreManager.StyleScoring(60000, 13000, 6000, 0.65f) },
                { StyleType.SSS, new ScoreManager.StyleScoring(48000, 13000, 4800, 0.45f) },
            };
    }
}
