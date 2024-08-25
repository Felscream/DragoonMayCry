using DragoonMayCry.Score.Style;
using DragoonMayCry.Util;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DragoonMayCry.Audio
{

    // Stolen from: https://markheath.net/post/fire-and-forget-audio-playback-with
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

        public AudioEngine()
        {
            sfxOutputDevice = new WaveOutEvent();

            sounds = new();

            sfxMixer = new(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2)) {
                ReadFully = true
            };

            sfxSampleProvider = new(sfxMixer);

            sfxOutputDevice.Init(sfxSampleProvider);
            sfxOutputDevice.Play();
        }

        public void Init(DoubleLinkedList<StyleRank> styles)
        {
            DoubleLinkedNode<StyleRank> current = styles.Head;

            while (current != null)
            {
                if (string.IsNullOrEmpty(current.Value.SfxPath))
                {
                    current = current.Next;
                    continue;
                }
                
                StyleRank rank = current.Value;
                Service.Log.Debug($"Registering sound for {rank.StyleType}, {rank.SfxPath}");
                sounds.Add(rank.StyleType, new CachedSound(rank.SfxPath));
                current = current.Next;
            }
        }

        public void PlaySFX(StyleType trigger)
        {
            if (!sounds.ContainsKey(trigger))
            {
                Service.Log.Warning($"Audio trigger {trigger} has no audio associated");
                return;
            }

            sfxSampleProvider.Volume = (Plugin.Configuration.SfxVolume / 100f) * GetGameSfxVolume() ;
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

        private float GetGameSfxVolume()
        {
            if (Service.GameConfig.System.GetBool("IsSndSe") ||
                Service.GameConfig.System.GetBool("IsSndMaster"))
            {
                return 0;
            }
            return Service.GameConfig.System.GetUInt("SoundSe") / 100f * (Service.GameConfig.System.GetUInt("SoundMaster") / 100f);
        }
    }
}