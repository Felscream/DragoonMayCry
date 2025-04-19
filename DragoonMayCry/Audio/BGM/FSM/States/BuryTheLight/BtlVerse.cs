#region

using System;
using System.Collections.Generic;

#endregion

namespace DragoonMayCry.Audio.BGM.FSM.States.BuryTheLight
{
    internal class BtlVerse : VerseFsmState
    {
        private readonly Random rand;
        public BtlVerse(AudioService audioService) : base(audioService, 1500,
                                                          new CombatEndTransitionTimings(1600, 8000))
        {
            rand = new Random();
            CombatIntro = GenerateCombatIntro();
        }

        protected override Dictionary<string, BgmTrackData> Stems { get; } = new()
        {
            {
                BgmStemIds.CombatEnter1,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("BuryTheLight\\CombatLoop\\111.ogg"), 0, 3200)
            },
            {
                BgmStemIds.CombatEnter2,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("BuryTheLight\\CombatLoop\\029.ogg"), 0, 12800)
            },
            {
                BgmStemIds.CombatVerse1,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("BuryTheLight\\CombatLoop\\017.ogg"), 0, 25600)
            },
            {
                BgmStemIds.CombatVerse2,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("BuryTheLight\\CombatLoop\\040.ogg"), 0, 25600)
            },
            {
                BgmStemIds.CombatCoreLoop,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("BuryTheLight\\CombatLoop\\coreloop.ogg"), 0, 80000)
            },
            {
                BgmStemIds.CombatCoreLoopTransition1,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("BuryTheLight\\CombatLoop\\029.ogg"), 0, 12800)
            },
            {
                BgmStemIds.CombatCoreLoopTransition2,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("BuryTheLight\\CombatLoop\\064.ogg"), 0, 12800)
            },
            {
                BgmStemIds.CombatCoreLoopExit1,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("BuryTheLight\\CombatLoop\\039.ogg"), 1590, 1600)
            },
            {
                BgmStemIds.CombatCoreLoopExit2,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("BuryTheLight\\CombatLoop\\092.ogg"), 1590, 1600)
            },
            {
                BgmStemIds.CombatCoreLoopExit3,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("BuryTheLight\\CombatLoop\\093.ogg"), 1590, 1600)
            },
        };

        protected sealed override LinkedList<string> GenerateCombatIntro()
        {
            LinkedList<string> combatIntro = new();
            combatIntro.AddLast(BgmStemIds.CombatEnter1);
            combatIntro.AddLast(BgmStemIds.CombatEnter2);
            return combatIntro;
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
            var loopTransitionQueue =
                RandomizeQueue(BgmStemIds.CombatCoreLoopTransition1, BgmStemIds.CombatCoreLoopTransition2);
            var loop = new LinkedList<string>();
            loop.AddLast(verseQueue.Dequeue());
            loop.AddLast(BgmStemIds.CombatCoreLoop);
            loop.AddLast(loopTransitionQueue.Dequeue());
            loop.AddLast(verseQueue.Dequeue());
            loop.AddLast(BgmStemIds.CombatCoreLoop);
            loop.AddLast(loopTransitionQueue.Dequeue());
            return loop;
        }
    }
}
