#region

using System.Collections.Generic;
using System.Linq;

#endregion

namespace DragoonMayCry.Audio.BGM.FSM.States.Custom
{
    internal class CustomChorus : ChorusFsmState
    {
        private readonly List<List<CustomBgmTrackData>> chorusStemGroups;
        private readonly CustomBgmTrackData demotionStem;
        public CustomChorus(
            AudioService audioService,
            List<List<CustomBgmTrackData>> chorusStemGroups,
            CustomBgmTrackData demotionStem,
            ExitTimings exitTimings) : base(audioService, 1290, exitTimings)
        {
            this.demotionStem = demotionStem;
            this.chorusStemGroups = chorusStemGroups;
            Stems = BuildStemDictionary();
        }
        protected override Dictionary<string, BgmTrackData> Stems { get; }
        protected override LinkedList<string> GenerateChorusLoop()
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

        private List<Queue<string>> CreateCombatLoopQueues()
        {
            var loop = new List<Queue<string>>();

            foreach (var chorusStemGroup in chorusStemGroups)
            {
                var queue = RandomizeQueue(chorusStemGroup.Select(g => g.StemId).ToList());
                loop.Add(queue);
            }

            return loop;
        }

        private Dictionary<string, BgmTrackData> BuildStemDictionary()
        {
            var stems = new Dictionary<string, BgmTrackData>
            {
                [BgmStemIds.Demotion] = demotionStem.BgmTrackData,
            };
            foreach (var chorusStemGroup in chorusStemGroups)
            {
                foreach (var customBgmTrackData in chorusStemGroup)
                {
                    stems[customBgmTrackData.StemId] = customBgmTrackData.BgmTrackData;
                }
            }
            return stems;
        }
    }
}
