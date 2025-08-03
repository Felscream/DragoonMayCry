#region

using DragoonMayCry.Util;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace DragoonMayCry.Audio.BGM.FSM.States.Custom
{
    internal class CustomVerse : VerseFsmState
    {
        private readonly List<CustomBgmTrackData> chorusTransitionsStems;
        private readonly CustomBgmTrackData combatIntroStem;
        private readonly List<List<CustomBgmTrackData>> verseStemGroups;
        public CustomVerse(
            AudioService audioService, CustomBgmTrackData combatIntroStem,
            List<List<CustomBgmTrackData>> verseStemGroups,
            List<CustomBgmTrackData> chorusTransitionsStems,
            CombatEndTransitionTimings combatEndTransitionTimings) : base(
            audioService, 1800, combatEndTransitionTimings)
        {
            this.combatIntroStem = combatIntroStem;
            this.verseStemGroups = verseStemGroups;
            this.chorusTransitionsStems = chorusTransitionsStems;
            Stems = BuildStemDictionary();
        }

        protected override Dictionary<string, BgmTrackData> Stems { get; }
        protected override LinkedList<string> GenerateCombatLoop()
        {
            var combatQueues = CreateCombatLoopQueues();
            var longestQueue = combatQueues.Aggregate((a, b) => a.Count > b.Count ? a : b);
            var loop = new LinkedList<string>();

            while (longestQueue.Count > 0)
            {
                foreach (var queue in combatQueues)
                {
                    var stemId = queue.Dequeue();
                    if (queue != longestQueue)
                    {
                        queue.Enqueue(stemId);
                    }
                    loop.AddLast(stemId);
                }
            }

            return loop;
        }
        protected override LinkedList<string> GenerateCombatIntro()
        {
            var list = new LinkedList<string>();
            list.AddLast(combatIntroStem.StemId);
            return list;
        }
        protected override string SelectChorusTransitionStem()
        {
            return chorusTransitionsStems.PickRandom().StemId;
        }

        private Dictionary<string, BgmTrackData> BuildStemDictionary()
        {
            var stems = new Dictionary<string, BgmTrackData>
            {
                [combatIntroStem.StemId] = combatIntroStem.BgmTrackData,
            };
            foreach (var verseStemGroup in verseStemGroups)
            {
                foreach (var customBgmTrackData in verseStemGroup)
                {
                    stems[customBgmTrackData.StemId] = customBgmTrackData.BgmTrackData;
                }
            }
            foreach (var chorusTransitionsStem in chorusTransitionsStems)
            {
                stems[chorusTransitionsStem.StemId] = chorusTransitionsStem.BgmTrackData;
            }
            return stems;
        }

        private List<Queue<string>> CreateCombatLoopQueues()
        {
            var loop = new List<Queue<string>>();

            foreach (var verseStemGroup in verseStemGroups)
            {
                var queue = RandomizeQueue(verseStemGroup.Select(g => g.StemId).ToList());
                loop.Add(queue);
            }

            return loop;
        }
    }
}
