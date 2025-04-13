using DragoonMayCry.Audio.Engine;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DragoonMayCry.Audio.BGM.FSM.States.Subhuman
{
    internal class SubChorus(AudioService audioService) : ChorusFsmState(audioService, 1290,
                                                                         new ExitTimings(
                                                                             1300, 6000, 6000, 0, 9000, 6000))
    {
        protected override Dictionary<string, BgmTrackData> Stems { get; } = new()
        {
            { BgmStemIds.ChorusIntro, new BgmTrackData(DynamicBgmService.GetPathToAudio("Subhuman\\Chorus\\009.ogg"), 0, 20650, 20650) },
            { BgmStemIds.ChorusIntro2, new BgmTrackData(DynamicBgmService.GetPathToAudio("Subhuman\\Chorus\\050.ogg"), 0, 20650, 20650) },
            { BgmStemIds.Riff, new BgmTrackData(DynamicBgmService.GetPathToAudio("Subhuman\\Chorus\\090.ogg"), 0, 23250, 23200) },
            { BgmStemIds.Chorus2, new BgmTrackData(DynamicBgmService.GetPathToAudio("Subhuman\\Chorus\\104.ogg"), 0, 23250, 23250) },
            { BgmStemIds.Chorus3, new BgmTrackData(DynamicBgmService.GetPathToAudio("Subhuman\\Chorus\\051.ogg"), 0, 23250, 23250) },
            { BgmStemIds.Chorus, new BgmTrackData(DynamicBgmService.GetPathToAudio("Subhuman\\Chorus\\105.ogg"), 0, 20650, 20650) },
            { BgmStemIds.ChorusTransition1, new BgmTrackData(DynamicBgmService.GetPathToAudio("Subhuman\\Verse\\053.ogg"), 0, 2650, 2640) },
            { BgmStemIds.ChorusTransition2, new BgmTrackData(DynamicBgmService.GetPathToAudio("Subhuman\\Verse\\065.ogg"), 0, 2550, 2540) },
            { BgmStemIds.ChorusTransition3, new BgmTrackData(DynamicBgmService.GetPathToAudio("Subhuman\\Verse\\001.ogg"), 0, 2600, 2590) },
            { BgmStemIds.Demotion, new BgmTrackData(DynamicBgmService.GetPathToAudio("Subhuman\\Chorus\\013.ogg"), 0, 2600) },
        };

        /*private readonly Dictionary<BgmId, int> possibleTransitionTimesToNewState = new()
        {
            { BgmId.ChorusIntro1, 20650 },
            { BgmId.ChorusIntro2, 20650 },
            { BgmId.Riff, 23200 },
            { BgmId.Chorus2, 23250 },
            { BgmId.Chorus3, 23250 },
            { BgmId.Chorus, 20650 },
            { BgmId.ChorusTransition1, 2640 },
            { BgmId.ChorusTransition2,  2540 },
            { BgmId.ChorusTransition3,  2590 },
        };

        private readonly Dictionary<BgmId, string> bgmPaths = new()
        {
            { BgmId.ChorusIntro1, DynamicBgmService.GetPathToAudio("Subhuman\\Chorus\\009.ogg") },
            { BgmId.ChorusIntro2, DynamicBgmService.GetPathToAudio("Subhuman\\Chorus\\050.ogg") },
            { BgmId.Riff, DynamicBgmService.GetPathToAudio("Subhuman\\Chorus\\090.ogg") },
            { BgmId.Chorus2, DynamicBgmService.GetPathToAudio("Subhuman\\Chorus\\104.ogg") },
            { BgmId.Chorus3, DynamicBgmService.GetPathToAudio("Subhuman\\Chorus\\051.ogg") },
            { BgmId.Demotion, DynamicBgmService.GetPathToAudio("Subhuman\\Chorus\\013.ogg") },
            { BgmId.ChorusTransition1, DynamicBgmService.GetPathToAudio("Subhuman\\Verse\\053.ogg") },
            { BgmId.ChorusTransition2, DynamicBgmService.GetPathToAudio("Subhuman\\Verse\\065.ogg") },
            { BgmId.ChorusTransition3, DynamicBgmService.GetPathToAudio("Subhuman\\Verse\\001.ogg") },
            { BgmId.Chorus, DynamicBgmService.GetPathToAudio("Subhuman\\Chorus\\105.ogg") },
        };*/

        private readonly Random random = new();

        protected override LinkedList<string> GenerateChorusLoop()
        {
            var chorusIntro = RandomizeQueue(BgmStemIds.ChorusIntro, BgmStemIds.ChorusIntro2);
            var chorus = RandomizeQueue(BgmStemIds.Chorus2, BgmStemIds.Chorus3);
            var loop = new LinkedList<string>();
            loop.AddLast(chorusIntro.Dequeue());
            loop.AddLast(BgmStemIds.Riff);
            loop.AddLast(chorus.Dequeue());
            loop.AddLast(BgmStemIds.Chorus);
            loop.AddLast(SelectRandom(BgmStemIds.ChorusTransition1, BgmStemIds.ChorusTransition2, BgmStemIds.ChorusTransition3));
            loop.AddLast(BgmStemIds.Riff);
            loop.AddLast(chorus.Dequeue());
            loop.AddLast(BgmStemIds.Chorus);
            loop.AddLast(SelectRandom(BgmStemIds.ChorusTransition1, BgmStemIds.ChorusTransition2, BgmStemIds.ChorusTransition3));
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
