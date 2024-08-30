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
        struct ScoringCoefficient
        {
            public readonly float ThresholdCoefficient;
            public readonly float ReductionPerSecondCoefficient;

            public ScoringCoefficient(
                float thresholdCoefficient, float reductionPerSecondCoefficient)
            {
                ThresholdCoefficient = thresholdCoefficient;
                ReductionPerSecondCoefficient = reductionPerSecondCoefficient;
            }
        }

        public static readonly Dictionary<StyleType, ScoreManager.StyleScoring>
            DefaultScoringTable = new Dictionary<StyleType, ScoreManager.StyleScoring>
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

        private static readonly  Dictionary<StyleType, ScoreManager.StyleScoring>
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

        private static readonly Dictionary<StyleType, ScoringCoefficient>
            MeleeScoringTableCoefficient = new Dictionary<StyleType, ScoringCoefficient>
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

        private static readonly Dictionary<StyleType, ScoreManager.StyleScoring>
            CasterScoringTable = new Dictionary<StyleType, ScoreManager.StyleScoring>
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

        private static readonly Dictionary<StyleType, ScoringCoefficient>
            CasterScoringCoefficient = new Dictionary<StyleType, ScoringCoefficient>
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

        private static readonly Dictionary<StyleType, ScoreManager.StyleScoring>
            PhysRangeScoringTable = new Dictionary<StyleType, ScoreManager.StyleScoring>
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

        private static readonly Dictionary<StyleType, ScoringCoefficient>
            PhysRangeScoringCoefficient = new Dictionary<StyleType, ScoringCoefficient>
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

        private static readonly Dictionary<StyleType, ScoreManager.StyleScoring>
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

        private static readonly Dictionary<StyleType, ScoringCoefficient>
            TankScoringCoefficient = new Dictionary<StyleType, ScoringCoefficient>
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

        private static readonly Dictionary<StyleType, ScoreManager.StyleScoring>
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

        private static readonly Dictionary<StyleType, ScoringCoefficient>
            HealerScoringCoefficient = new Dictionary<StyleType, ScoringCoefficient>
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


        public static Dictionary<StyleType, ScoreManager.StyleScoring>
            GenerateMeleeScoring(int ilvl)
        {
            float expectedDpsAtILvl = GetMeleeDpsAtILvl(ilvl);
#if DEBUG
            Service.Log.Debug($"Estimated DPS {expectedDpsAtILvl}");
#endif
            return GenerateScoringTable(expectedDpsAtILvl, MeleeScoringTableCoefficient, MeleeScoringTable);
        }

        public static Dictionary<StyleType, ScoreManager.StyleScoring>
            GenerateCasterScoring(int ilvl)
        {
            float expectedDpsAtILvl = GetCasterDpsAtILvl(ilvl);
#if DEBUG
            Service.Log.Debug($"Estimated DPS {expectedDpsAtILvl}");
#endif
            return GenerateScoringTable(expectedDpsAtILvl, CasterScoringCoefficient, CasterScoringTable);
        }

        public static Dictionary<StyleType, ScoreManager.StyleScoring>
            GeneratePhysRangeScoring(int ilvl)
        {
            float expectedDpsAtILvl = GetPhysRangeDpsAtILvl(ilvl);
#if DEBUG
            Service.Log.Debug($"Estimated DPS {expectedDpsAtILvl}");
#endif
            return GenerateScoringTable(expectedDpsAtILvl, PhysRangeScoringCoefficient, PhysRangeScoringTable);
        }

        public static Dictionary<StyleType, ScoreManager.StyleScoring>
            GenerateTankScoring(int ilvl)
        {
            float expectedDpsAtILvl = GetTankDpsAtILvl(ilvl);
#if DEBUG
            Service.Log.Debug($"Estimated DPS {expectedDpsAtILvl}");
#endif
            return GenerateScoringTable(expectedDpsAtILvl, TankScoringCoefficient, TankScoringTable);
        }

        public static Dictionary<StyleType, ScoreManager.StyleScoring>
            GenerateHealerScoring(int ilvl)
        {
            float expectedDpsAtILvl = GetTankDpsAtILvl(ilvl);
#if DEBUG
            Service.Log.Debug($"Estimated DPS {expectedDpsAtILvl}");
#endif
            return GenerateScoringTable(expectedDpsAtILvl, HealerScoringCoefficient, HealerScoringTable);
        }

        private static Dictionary<StyleType, ScoreManager.StyleScoring> GenerateScoringTable(float expectedDpsAtILvl, Dictionary<StyleType, ScoringCoefficient> scoringCoefficients, Dictionary<StyleType, ScoreManager.StyleScoring> baseScoringTable)
        {
            Dictionary<StyleType, ScoreManager.StyleScoring> scoringTable =
                new Dictionary<StyleType, ScoreManager.StyleScoring>();

            foreach (var entry in baseScoringTable)
            {
                int thresholdForIlvl =
                    (int)Math.Floor(expectedDpsAtILvl *
                                    scoringCoefficients[entry.Key]
                                        .ThresholdCoefficient);
                int reductionPerSecond =
                    (int)Math.Floor(expectedDpsAtILvl * scoringCoefficients[entry.Key]
                                        .ReductionPerSecondCoefficient);
                int demotionThreshold = (int)Math.Ceiling(thresholdForIlvl / 10f);

                ScoreManager.StyleScoring styleScoring= new ScoreManager.StyleScoring(
                    thresholdForIlvl, reductionPerSecond, demotionThreshold,
                    baseScoringTable[entry.Key].PointCoefficient);

                scoringTable[entry.Key] = styleScoring;
            }

            return scoringTable;
        }

        private static float GetMeleeDpsAtILvl(int ilvl)
        {
            return (float)(76.7f * Math.Exp(0.00799f * ilvl));
        }

        private static float GetCasterDpsAtILvl(int ilvl)
        {
            return (float)(64.6f * Math.Exp(0.00804f * ilvl));
        }

        private static float GetPhysRangeDpsAtILvl(int ilvl)
        {
            return (float)(68.7f * Math.Exp(0.00774f * ilvl));
        }

        private static float GetTankDpsAtILvl(int ilvl)
        {
            return (float)(45.1f * Math.Exp(0.00813f * ilvl));
        }

        private static float GetHealerDpsAtILvl(int ilvl)
        {
            return (float)(39.1f * Math.Exp(0.0078f * ilvl));
        }
    }
}
