#region

using System;
using System.Collections.Generic;

#endregion

namespace DragoonMayCry.Audio.BGM.FSM.States.DevilTrigger
{
    internal class DtVerse : VerseFsmState
    {
        private readonly Random rand;
        public DtVerse(AudioService audioService) : base(audioService, 1500, new CombatEndTransitionTimings(1, 4500))
        {
            rand = new Random();
            CombatIntro = GenerateCombatIntro();
        }

        protected override Dictionary<string, BgmTrackData> Stems { get; } = new()
        {
            {
                BgmStemIds.CombatEnter1,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("DevilTrigger\\Verse\\028.ogg"), 0, 3000)
            },
            {
                BgmStemIds.CombatEnter2,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("DevilTrigger\\Verse\\101.ogg"), 0, 10500)
            },
            {
                BgmStemIds.CombatVerse1,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("DevilTrigger\\Verse\\116.ogg"), 0, 22500)
            },
            {
                BgmStemIds.CombatVerse2,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("DevilTrigger\\Verse\\021.ogg"), 0, 22500)
            },
            {
                BgmStemIds.CombatCoreLoopTransition1,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("DevilTrigger\\Verse\\109.ogg"), 0, 25500)
            },
            {
                BgmStemIds.CombatCoreLoopTransition2,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("DevilTrigger\\Verse\\106.ogg"), 0, 24000)
            },
            {
                BgmStemIds.CombatCoreLoopTransition3,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("DevilTrigger\\Verse\\066.ogg"), 0, 24000)
            },
            {
                BgmStemIds.CombatCoreLoopExit1,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("DevilTrigger\\Verse\\070.ogg"), 1, 1550)
            },
            {
                BgmStemIds.CombatCoreLoopExit2,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("DevilTrigger\\Verse\\075.ogg"), 1, 1550)
            },
            {
                BgmStemIds.CombatCoreLoopExit3,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("DevilTrigger\\Verse\\085.ogg"), 1, 1550)
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
            var loop = new LinkedList<string>();
            loop.AddLast(BgmStemIds.CombatCoreLoopTransition1);
            loop.AddLast(verseQueue.Dequeue());
            loop.AddLast(BgmStemIds.CombatCoreLoopTransition2);
            loop.AddLast(BgmStemIds.CombatCoreLoopTransition3);
            loop.AddLast(BgmStemIds.CombatCoreLoopTransition1);
            loop.AddLast(verseQueue.Dequeue());
            loop.AddLast(BgmStemIds.CombatCoreLoopTransition2);
            loop.AddLast(BgmStemIds.CombatCoreLoopTransition3);
            return loop;
        }
    }
}
