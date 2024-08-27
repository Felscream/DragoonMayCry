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
                { StyleType.NoStyle, new ScoreManager.StyleScoring(60000, 500, 0) },
                { StyleType.D, new ScoreManager.StyleScoring(80000, 1000, 8000) },
                { StyleType.C, new ScoreManager.StyleScoring(90000, 5000, 9000) },
                { StyleType.B, new ScoreManager.StyleScoring(90000, 6000, 9000) },
                { StyleType.A, new ScoreManager.StyleScoring(100000, 8000, 10000) },
                { StyleType.S, new ScoreManager.StyleScoring(100000, 15000, 10000) },
                { StyleType.SS, new ScoreManager.StyleScoring(70000, 18000, 7000) },
                { StyleType.SSS, new ScoreManager.StyleScoring(38000, 25000, 3800) },
            };

        public static readonly Dictionary<StyleType, ScoreManager.StyleScoring>
            TankScoringTable = new Dictionary<StyleType, ScoreManager.StyleScoring>
            {
                { StyleType.NoStyle, new ScoreManager.StyleScoring(40000, 500, 0) },
                { StyleType.D, new ScoreManager.StyleScoring(40000, 3000, 4000) },
                { StyleType.C, new ScoreManager.StyleScoring(48000, 7000, 4800) },
                { StyleType.B, new ScoreManager.StyleScoring(54000, 8000, 5400) },
                { StyleType.A, new ScoreManager.StyleScoring(60000, 8000, 6000) },
                { StyleType.S, new ScoreManager.StyleScoring(60000, 10000, 6000) },
                { StyleType.SS, new ScoreManager.StyleScoring(60000, 13000, 6000) },
                { StyleType.SSS, new ScoreManager.StyleScoring(48000, 15000, 4800) },
            };

        public static readonly Dictionary<StyleType, ScoreManager.StyleScoring>
            HealerScoringTable = new Dictionary<StyleType, ScoreManager.StyleScoring>
            {
                { StyleType.NoStyle, new ScoreManager.StyleScoring(40000, 500, 0) },
                { StyleType.D, new ScoreManager.StyleScoring(40000, 3000, 4000) },
                { StyleType.C, new ScoreManager.StyleScoring(48000, 7000, 4800) },
                { StyleType.B, new ScoreManager.StyleScoring(54000, 8000, 5400) },
                { StyleType.A, new ScoreManager.StyleScoring(60000, 8000, 6000) },
                { StyleType.S, new ScoreManager.StyleScoring(60000, 12700, 6000) },
                { StyleType.SS, new ScoreManager.StyleScoring(60000, 13000, 6000) },
                { StyleType.SSS, new ScoreManager.StyleScoring(48000, 14500, 4800) },
            };
    }
}
