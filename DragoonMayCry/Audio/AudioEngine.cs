using DragoonMayCry.Score.Style;
using DragoonMayCry.Util;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace DragoonMayCry.Audio
{
    class CachedSound
    {
        internal float[] AudioData { get; private set; }
        internal WaveFormat WaveFormat { get; private set; }
        internal CachedSound(string audioFileName)
        {
            using (var audioFileReader = new AudioFileReader(audioFileName))
            {
                WaveFormat = audioFileReader.WaveFormat;
                var wholeFile = new List<float>((int)(audioFileReader.Length / 4));
                var readBuffer = new float[audioFileReader.WaveFormat.SampleRate * audioFileReader.WaveFormat.Channels];
                int samplesRead;
                while ((samplesRead = audioFileReader.Read(readBuffer, 0, readBuffer.Length)) > 0)
                {
                    wholeFile.AddRange(readBuffer.Take(samplesRead));
                }
                AudioData = wholeFile.ToArray();
            }
        }
    }

    class CachedSoundSampleProvider : ISampleProvider
    {
        private readonly CachedSound cachedSound;
        private long position;

        public CachedSoundSampleProvider(CachedSound cachedSound)
        {
            this.cachedSound = cachedSound;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            var availableSamples = cachedSound.AudioData.Length - position;
            var samplesToCopy = Math.Min(availableSamples, count);
            Array.Copy(cachedSound.AudioData, position, buffer, offset, samplesToCopy);
            position += samplesToCopy;
            return (int)samplesToCopy;
        }

        public WaveFormat WaveFormat { get { return cachedSound.WaveFormat; } }
    }

    public class AudioEngine
    {
        private readonly IWavePlayer sfxOutputDevice;
        private readonly VolumeSampleProvider sfxSampleProvider;
        private readonly MixingSampleProvider sfxMixer;
        private Dictionary<StyleType, CachedSound> sounds;

        public AudioEngine(Dictionary<StyleType, String> sfx)
        {
            sfxOutputDevice = new WaveOutEvent();

            sounds = new();

            sfxMixer = new(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2))
            {
                ReadFully = true
            };

            sfxSampleProvider = new(sfxMixer);

            sfxOutputDevice.Init(sfxSampleProvider);
            sfxOutputDevice.Play();

            foreach (var entry in sfx)
            {
                RegisterSfx(entry.Key, entry.Value);
            }
        }
        private void RegisterSfx(StyleType type, string path)
        {
            if (sounds.ContainsKey(type))
            {
                return;
            }
            Service.Log.Debug($"Registering sound for {type}, {path}");
            sounds.Add(type, new CachedSound(path));
        }

        public void PlaySfx(StyleType trigger, float volume)
        {
            if (!sounds.ContainsKey(trigger))
            {
                Service.Log.Warning($"Audio trigger {trigger} has no audio associated");
                return;
            }

            sfxSampleProvider.Volume = volume;
            Service.Log.Debug($"Playing audio for trigger {trigger}");
            AddSFXMixerInput(new CachedSoundSampleProvider(sounds[trigger]));
        }

        private ISampleProvider ConvertToRightChannelCount(ISampleProvider input)
        {
            if (input.WaveFormat.Channels == sfxMixer.WaveFormat.Channels)
            {
                return input;
            }
            if (input.WaveFormat.Channels == 1 && sfxMixer.WaveFormat.Channels == 2)
            {
                return new MonoToStereoSampleProvider(input);
            }
            throw new NotImplementedException("Not yet implemented this channel count conversion");
        }
        private void AddSFXMixerInput(ISampleProvider input)
        {
            ISampleProvider mixerInput = ConvertToRightChannelCount(input);
            sfxMixer.AddMixerInput(mixerInput);
        }
    }
}
