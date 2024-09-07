using Dalamud.Plugin.Services;
using DragoonMayCry.Score.Model;
using DragoonMayCry.Score.Style;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
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

        private readonly IWavePlayer sfxOutputDevice;
        private readonly Dictionary<SoundId, CachedSound> sounds;
        private readonly VolumeSampleProvider sfxSampleProvider;
        private readonly MixingSampleProvider sfxMixer;

        public AudioEngine(Dictionary<SoundId, string> sfx)
        {
            sfxOutputDevice = new WaveOutEvent();

            sounds = new Dictionary<SoundId, CachedSound>();
            foreach(KeyValuePair<SoundId, string> entry in sfx)
            {
                sounds.Add(entry.Key, new(entry.Value));
            }

            sfxMixer = new(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2))
            {
                ReadFully = true
            };

            sfxSampleProvider = new(sfxMixer);

            sfxOutputDevice.Init(sfxSampleProvider);
            sfxOutputDevice.Play();
        }

        public void UpdateSfxVolume(float value)
        {
            sfxSampleProvider.Volume = value;
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

        public void PlaySfx(SoundId trigger)
        {
            if (!sounds.ContainsKey(trigger))
            {
                return;
            }
            AddSFXMixerInput(new CachedSoundSampleProvider(sounds[trigger]));
        }
    }
}
