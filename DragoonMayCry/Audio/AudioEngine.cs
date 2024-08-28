using DragoonMayCry.Score.Style;
using NAudio.Wave;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace DragoonMayCry.Audio
{
    internal class AudioEngine
    {
        private readonly IDictionary<SoundId, byte> soundState = new ConcurrentDictionary<SoundId, byte>();

        // Copied from PeepingTom plugin, by ascclemens:
        // https://git.anna.lgbt/anna/PeepingTom/src/commit/b1de54bcae64edf97c9f90614a588e64b5d0ae34/Peeping%20Tom/TargetWatcher.cs#L161
        public void PlaySfx(SoundId trigger, string path, float volume)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                Service.Log.Error($"Could not find audio file: [{path}]");
                return;
            }

            var soundDevice = DirectSoundOut.DSDEVID_DefaultPlayback;
            new Thread(() =>
            {
                WaveStream reader;
                try
                {
                    reader = new MediaFoundationReader(path);
                }
                catch (Exception e)
                {
                    Service.Log.Error(e.Message);
                    return;
                }
                using var channel = new WaveChannel32(reader)
                {
                    Volume = volume,
                    PadWithZeroes = false,
                };

                using (reader)
                {
                    using var output = new DirectSoundOut(soundDevice);

                    try
                    {
                        output.Init(channel);
                        output.Play();
                        soundState[trigger] = 1;

                        while (output.PlaybackState == PlaybackState.Playing)
                        {
                            if (!soundState.ContainsKey(trigger))
                            {
                                output.Stop();
                            }

                            Thread.Sleep(500);
                        }
                        soundState.Remove(trigger);
                    }
                    catch (Exception ex)
                    {
                        Service.Log.Error(ex, "Exception playing sound");
                    }
                }
            }).Start();
        }
    }
}
