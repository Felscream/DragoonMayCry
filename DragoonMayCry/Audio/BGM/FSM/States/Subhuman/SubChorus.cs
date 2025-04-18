using System;
using System.Collections.Generic;

namespace DragoonMayCry.Audio.BGM.FSM.States.Subhuman
{
    internal class SubChorus(AudioService audioService) : ChorusFsmState(audioService, 1290,
                                                                         new ExitTimings(
                                                                             1300, 6000, 6000, 0, 9000, 6000))
    {

        private readonly Random random = new();
        protected override Dictionary<string, BgmTrackData> Stems { get; } = new()
        {
            {
                BgmStemIds.ChorusIntro1,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("Subhuman\\Chorus\\009.ogg"), 0, 20650, 20650)
            },
            {
                BgmStemIds.ChorusIntro2,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("Subhuman\\Chorus\\050.ogg"), 0, 20650, 20650)
            },
            {
                BgmStemIds.Riff,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("Subhuman\\Chorus\\090.ogg"), 0, 23250, 23200)
            },
            {
                BgmStemIds.Chorus2,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("Subhuman\\Chorus\\104.ogg"), 0, 23250, 23250)
            },
            {
                BgmStemIds.Chorus3,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("Subhuman\\Chorus\\051.ogg"), 0, 23250, 23250)
            },
            {
                BgmStemIds.Chorus,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("Subhuman\\Chorus\\105.ogg"), 0, 20650, 20650)
            },
            {
                BgmStemIds.ChorusTransition1,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("Subhuman\\Verse\\053.ogg"), 0, 2650, 2640)
            },
            {
                BgmStemIds.ChorusTransition2,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("Subhuman\\Verse\\065.ogg"), 0, 2550, 2540)
            },
            {
                BgmStemIds.ChorusTransition3,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("Subhuman\\Verse\\001.ogg"), 0, 2600, 2590)
            },
            {
                BgmStemIds.Demotion,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("Subhuman\\Chorus\\013.ogg"), 0, 2600)
            },
        };

        protected override LinkedList<string> GenerateChorusLoop()
        {
            var chorusIntro = RandomizeQueue(BgmStemIds.ChorusIntro1, BgmStemIds.ChorusIntro2);
            var chorus = RandomizeQueue(BgmStemIds.Chorus2, BgmStemIds.Chorus3);
            var loop = new LinkedList<string>();
            loop.AddLast(chorusIntro.Dequeue());
            loop.AddLast(BgmStemIds.Riff);
            loop.AddLast(chorus.Dequeue());
            loop.AddLast(BgmStemIds.Chorus);
            loop.AddLast(SelectRandom(BgmStemIds.ChorusTransition1, BgmStemIds.ChorusTransition2,
                                      BgmStemIds.ChorusTransition3));
            loop.AddLast(BgmStemIds.Riff);
            loop.AddLast(chorus.Dequeue());
            loop.AddLast(BgmStemIds.Chorus);
            loop.AddLast(SelectRandom(BgmStemIds.ChorusTransition1, BgmStemIds.ChorusTransition2,
                                      BgmStemIds.ChorusTransition3));
            return loop;
        }

        private string SelectRandom(params string[] bgmIds)
        {
            var index = random.Next(bgmIds.Length);
            return bgmIds[index];
        }

        private Queue<string> RandomizeQueue(params string[] bgmIds)
        {
            var k = random.Next(2);
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
    }
}
