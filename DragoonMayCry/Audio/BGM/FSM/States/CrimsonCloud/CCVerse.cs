#region

using System;
using System.Collections.Generic;

#endregion

namespace DragoonMayCry.Audio.BGM.FSM.States.CrimsonCloud
{
    internal class CcVerse : VerseFsmState
    {
        private readonly Random rand;
        public CcVerse(AudioService audioService) : base(audioService, 1500,
                                                         new CombatEndTransitionTimings(1, 4500))
        {
            rand = new Random();
            CombatIntro = GenerateCombatIntro();
        }

        protected override Dictionary<string, BgmTrackData> Stems { get; } = new()
        {
            {
                BgmStemIds.CombatEnter1,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("CrimsonCloud\\Verse\\004.ogg"), 0, 2500)
            },
            {
                BgmStemIds.CombatEnter2,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("CrimsonCloud\\Verse\\069.ogg"), 0, 10400)
            },
            {
                BgmStemIds.CombatEnter3,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("CrimsonCloud\\Verse\\036.ogg"), 0, 2590)
            },
            {
                BgmStemIds.CombatVerse1,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("CrimsonCloud\\Verse\\110.ogg"), 0, 20650)
            },
            {
                BgmStemIds.CombatVerse2,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("CrimsonCloud\\Verse\\056.ogg"), 0, 20650)
            },
            {
                BgmStemIds.CombatVerse3,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("CrimsonCloud\\Verse\\007.ogg"), 0, 20600)
            },
            {
                BgmStemIds.CombatVerse4,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("CrimsonCloud\\Verse\\074.ogg"), 0, 20600)
            },
            {
                BgmStemIds.CombatCoreLoopTransition1,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("CrimsonCloud\\Verse\\052.ogg"), 0, 19355)
            },
            {
                BgmStemIds.CombatCoreLoopTransition2,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("CrimsonCloud\\Verse\\060.ogg"), 1300, 21900)
            },
            {
                BgmStemIds.CombatCoreLoopTransition3,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("CrimsonCloud\\Verse\\108.ogg"), 0, 10295)
            },
            {
                BgmStemIds.CombatCoreLoopExit1,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("CrimsonCloud\\Verse\\038.ogg"), 1, 1000)
            },
            {
                BgmStemIds.CombatCoreLoopExit2,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("CrimsonCloud\\Verse\\045.ogg"), 1, 2555)
            },
        };

        protected sealed override LinkedList<string> GenerateCombatIntro()
        {
            LinkedList<string> combatIntro = new();
            combatIntro.AddLast(BgmStemIds.CombatEnter1);
            combatIntro.AddLast(BgmStemIds.CombatEnter2);
            combatIntro.AddLast(BgmStemIds.CombatEnter3);
            return combatIntro;
        }

        protected override string SelectChorusTransitionStem()
        {
            return SelectRandom(BgmStemIds.CombatCoreLoopExit1, BgmStemIds.CombatCoreLoopExit2);
        }

        private string SelectRandom(params string[] bgmIds)
        {
            var index = rand.Next(0, bgmIds.Length);
            return bgmIds[index];
        }

        private Queue<string> RandomizeQueue(params string[] bgmIds)
        {
            var k = rand.Next(2);
            var queue = new Queue<string>();
            if (k < 1)
            {
                for (var i = 0; i < bgmIds.Length; i++)
                {
                    queue.Enqueue(bgmIds[i]);
                }
            }
            else
            {
                for (var i = bgmIds.Length - 1; i >= 0; i--)
                {
                    queue.Enqueue(bgmIds[i]);
                }
            }
            return queue;
        }

        protected override LinkedList<string> GenerateCombatLoop()
        {
            var verseQueue = RandomizeQueue(BgmStemIds.CombatVerse1, BgmStemIds.CombatVerse2);
            var verse2Queue = RandomizeQueue(BgmStemIds.CombatVerse3, BgmStemIds.CombatVerse4);
            var verse3Queue =
                RandomizeQueue(BgmStemIds.CombatCoreLoopTransition1, BgmStemIds.CombatCoreLoopTransition2);
            var loop = new LinkedList<string>();
            loop.AddLast(verseQueue.Dequeue());
            loop.AddLast(verse2Queue.Dequeue());
            loop.AddLast(verse3Queue.Dequeue());
            loop.AddLast(BgmStemIds.CombatCoreLoopTransition3);
            loop.AddLast(verseQueue.Dequeue());
            loop.AddLast(verse2Queue.Dequeue());
            loop.AddLast(verse3Queue.Dequeue());
            loop.AddLast(BgmStemIds.CombatCoreLoopTransition3);
            return loop;
        }
    }
}
