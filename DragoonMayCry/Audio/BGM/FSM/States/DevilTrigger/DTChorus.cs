using System;
using System.Collections.Generic;

namespace DragoonMayCry.Audio.BGM.FSM.States.DevilTrigger
{
    internal class DtChorus(AudioService audioService) : ChorusFsmState(audioService, 1590,
                                                                        new ExitTimings(1, 4500, 4500), 500)
    {

        private readonly Random random = new();

        protected override Dictionary<string, BgmTrackData> Stems { get; } = new()
        {
            {
                BgmStemIds.ChorusIntro1,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("DevilTrigger\\Chorus\\058.ogg"), 0, 12000, 13500)
            },
            {
                BgmStemIds.ChorusIntro2,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("DevilTrigger\\Chorus\\061.ogg"), 0, 12000, 13500)
            },
            {
                BgmStemIds.Riff,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("DevilTrigger\\Chorus\\003.ogg"), 0, 24000, 25500)
            },
            {
                BgmStemIds.ChorusTransition1,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("DevilTrigger\\Chorus\\098.ogg"), 0, 24000, 25500)
            },
            {
                BgmStemIds.ChorusTransition2,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("DevilTrigger\\Chorus\\072.ogg"), 0, 24000, 25500)
            },
            {
                BgmStemIds.ChorusTransition3,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("DevilTrigger\\Chorus\\041.ogg"), 0, 24000, 25500)
            },
            {
                BgmStemIds.Demotion,
                new BgmTrackData(DynamicBgmService.GetPathToAudio("DevilTrigger\\Chorus\\030.ogg"), 0, 1500)
            },
        };

        protected override LinkedList<string> GenerateChorusLoop()
        {
            var chorusIntro = RandomizeQueue(BgmStemIds.ChorusIntro1, BgmStemIds.ChorusIntro2);
            var chorus = RandomizeQueue(BgmStemIds.ChorusTransition1, BgmStemIds.ChorusTransition2);
            var loop = new LinkedList<string>();
            loop.AddLast(chorusIntro.Dequeue());
            loop.AddLast(BgmStemIds.Riff);
            loop.AddLast(chorus.Dequeue());
            loop.AddLast(BgmStemIds.ChorusTransition3);
            loop.AddLast(chorusIntro.Dequeue());
            loop.AddLast(BgmStemIds.Riff);
            loop.AddLast(chorus.Dequeue());
            loop.AddLast(BgmStemIds.ChorusTransition3);
            return loop;
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
