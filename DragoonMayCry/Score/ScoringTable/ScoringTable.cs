using DragoonMayCry.Score.Model;
using System;
using System.Collections.Generic;
namespace DragoonMayCry.Score.Table
{
    public abstract class ScoringTable
    {
        protected abstract Dictionary<StyleType, StyleScoring> RoleScoringTable { get; }
        protected abstract Dictionary<StyleType, ScoringCoefficient> ScoringCoefficient { get; }
        protected abstract Dictionary<StyleType, ScoringCoefficient> EmdScoringCoefficient { get; }
        protected abstract float GetDpsAtIlvl(int ilvl);
        public Dictionary<StyleType, StyleScoring> GetScoringTable(int ilvl)
        {
            return GenerateScoringTable(ilvl);
        }

        protected Dictionary<StyleType, StyleScoring> GenerateScoringTable(int ilvl)
        {
            var scoringTable = new Dictionary<StyleType, StyleScoring>();
            var expectedDpsAtILvl = GetDpsAtIlvl(ilvl);

            var coefficientTable = Plugin.IsEmdModeEnabled() ? EmdScoringCoefficient : ScoringCoefficient;

#if DEBUG
            Service.Log.Debug($"Estimated DPS {expectedDpsAtILvl}");
#endif

            foreach (var entry in RoleScoringTable)
            {
                var thresholdForIlvl =
                    (int)Math.Floor(expectedDpsAtILvl *
                                    coefficientTable[entry.Key]
                                        .ThresholdCoefficient);
                var reductionPerSecond =
                    (int)Math.Floor(expectedDpsAtILvl * coefficientTable[entry.Key]
                                        .ReductionPerSecondCoefficient);
                var demotionThreshold = (int)Math.Ceiling(thresholdForIlvl / 10f);

                var styleScoring = new StyleScoring(
                    thresholdForIlvl, reductionPerSecond, demotionThreshold,
                    RoleScoringTable[entry.Key].PointCoefficient);

                scoringTable[entry.Key] = styleScoring;
            }
            Service.Log.Debug($"{scoringTable[StyleType.NoStyle].Threshold}");

            return scoringTable;
        }
    }
}
