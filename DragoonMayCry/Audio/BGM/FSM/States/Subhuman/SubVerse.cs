#region

using System;
using System.Collections.Generic;

#endregion

namespace DragoonMayCry.Audio.BGM.FSM.States.Subhuman
{
    internal class SubVerse : VerseFsmState
    {
        private readonly Random rand;

        public SubVerse(AudioService audioService) : base(audioService, 1800,
                                                          new CombatEndTransitionTimings(1400, 6000, 0, 9000, 6000))
        {
            rand = new Random();
            CombatIntro = GenerateCombatIntro();
        }

        protected override Dictionary<string, BgmTrackData> Stems { get; } = new()
        {
            {
                BgmStemIds.CombatEnter1,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("Subhuman\\Verse\\014.ogg"), 0, 1300)
            },
            {
                BgmStemIds.CombatEnter2,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("Subhuman\\Verse\\097.ogg"), 0, 20650)
            },
            {
                BgmStemIds.CombatEnter3,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("Subhuman\\Verse\\022.ogg"), 0, 5100)
            },
            {
                BgmStemIds.CombatVerse1,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("Subhuman\\Verse\\018.ogg"), 0, 20650)
            },
            {
                BgmStemIds.CombatVerse2,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("Subhuman\\Verse\\015.ogg"), 0, 20650)
            },
            {
                BgmStemIds.CombatVerse3,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("Subhuman\\Verse\\115.ogg"), 0, 20650)
            },
            {
                BgmStemIds.CombatVerse4,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("Subhuman\\Verse\\002.ogg"), 0, 20650)
            },
            {
                BgmStemIds.CombatCoreLoopTransition1,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("Subhuman\\Verse\\071.ogg"), 0, 20650)
            },
            {
                BgmStemIds.CombatCoreLoopTransition2,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("Subhuman\\Verse\\099.ogg"), 0, 20650)
            },
            {
                BgmStemIds.CombatCoreLoopTransition3,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("Subhuman\\Verse\\020.ogg"), 0, 20650)
            },
            {
                BgmStemIds.CombatCoreLoopTransition4,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("Subhuman\\Verse\\067.ogg"), 0, 20650)
            },
            {
                BgmStemIds.CombatCoreLoopExit1,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("Subhuman\\Verse\\053.ogg"), 1290, 2650)
            },
            {
                BgmStemIds.CombatCoreLoopExit2,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("Subhuman\\Verse\\065.ogg"), 1290, 2550)
            },
            {
                BgmStemIds.CombatCoreLoopExit3,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("Subhuman\\Verse\\001.ogg"), 1290, 2600)
            },
        };

        protected sealed override LinkedList<string> GenerateCombatIntro()
        {
            var list = new LinkedList<string>();
            list.AddLast(BgmStemIds.CombatEnter1);
            list.AddLast(BgmStemIds.CombatEnter2);
            list.AddLast(BgmStemIds.CombatEnter3);
            return list;
        }

        protected override string SelectChorusTransitionStem()
        {
            return SelectRandom(BgmStemIds.CombatCoreLoopExit1, BgmStemIds.CombatCoreLoopExit2,
                                BgmStemIds.CombatCoreLoopExit3);
        }

        private string SelectRandom(params string[] bgmIds)
        {
            var index = rand.Next(0, bgmIds.Length);
            return bgmIds[index];
        }

        private Queue<string> RandomizeQueue(params string[] bgmIds)
        {
            var list = new List<string>(bgmIds);
            var queue = new Queue<string>();
            while (list.Count > 0)
            {
                var k = rand.Next(list.Count);
                queue.Enqueue(list[k]);
                list.RemoveAt(k);
            }
            return queue;
        }

        protected override LinkedList<string> GenerateCombatLoop()
        {
            var verseQueue = RandomizeQueue(BgmStemIds.CombatVerse1, BgmStemIds.CombatVerse2);
            var verseQueue2 = RandomizeQueue(BgmStemIds.CombatVerse3, BgmStemIds.CombatVerse4);
            var transitionQueue = RandomizeQueue(BgmStemIds.CombatCoreLoopTransition1,
                                                 BgmStemIds.CombatCoreLoopTransition2,
                                                 BgmStemIds.CombatCoreLoopTransition3);
            var loop = new LinkedList<string>();
            loop.AddLast(verseQueue.Dequeue());
            loop.AddLast(verseQueue2.Dequeue());
            loop.AddLast(transitionQueue.Dequeue());
            loop.AddLast(BgmStemIds.CombatCoreLoopTransition4);
            loop.AddLast(verseQueue.Dequeue());
            loop.AddLast(verseQueue2.Dequeue());
            loop.AddLast(transitionQueue.Dequeue());
            loop.AddLast(BgmStemIds.CombatCoreLoopTransition4);
            return loop;
        }
    }
}
