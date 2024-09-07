using Dalamud.Plugin.Services;
using DragoonMayCry.Audio.FSM;
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
    internal class AudioEngine : IDisposable
    {

        private readonly IWavePlayer sfxOutputDevice;
        private readonly IWavePlayer bgmOutputDevice;
        private readonly Dictionary<SoundId, CachedSound> sounds;
        private readonly Dictionary<BgmId, CachedSound> bgmParts;
        private readonly VolumeSampleProvider sfxSampleProvider;
        private readonly MixingSampleProvider sfxMixer;
        private readonly VolumeSampleProvider bgmSampleProvider;
        private readonly MixingSampleProvider bgmMixer;

        public AudioEngine()
        {
            sfxOutputDevice = new WaveOutEvent();
            bgmOutputDevice = new WaveOutEvent();
            sounds = new Dictionary<SoundId, CachedSound>();

            sfxMixer = new(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2))
            {
                ReadFully = true
            };

            bgmMixer = new(WaveFormat.CreateIeeeFloatWaveFormat(48000, 2))
            {
                ReadFully = true
            };

            sfxSampleProvider = new(sfxMixer);
            bgmSampleProvider = new(bgmMixer);

            sfxOutputDevice.Init(sfxSampleProvider);
            bgmOutputDevice.Init(bgmSampleProvider);

            sfxOutputDevice.Play();
            bgmOutputDevice.Play();
        }

        public void UpdateSfxVolume(float value)
        {
            sfxSampleProvider.Volume = value;
        }

        public void RegisterSfx(Dictionary<SoundId, string> sfx)
        {
            foreach (KeyValuePair<SoundId, string> entry in sfx)
            {
                if(!File.Exists(entry.Value)) {
                    Service.Log.Error($"Could not find any file at {entry.Value}");
                    continue;
                }
                if (!sounds.ContainsKey(entry.Key))
                {
                    sounds.Add(entry.Key, new(entry.Value));
                }
                else
                {
                    sounds[entry.Key] = new(entry.Value);
                }
                
            }
        }

        public void UpdateBgmVolume(float value)
        {
            bgmSampleProvider.Volume = value;
        }

        private ISampleProvider ConvertToRightChannelCount(MixingSampleProvider mixer, ISampleProvider input)
        {
            if (input.WaveFormat.Channels == mixer.WaveFormat.Channels)
            {
                return input;
            }
            if (input.WaveFormat.Channels == 1 && mixer.WaveFormat.Channels == 2)
            {
                return new MonoToStereoSampleProvider(input);
            }
            throw new NotImplementedException("Not yet implemented this channel count conversion");
        }

        private void AddSFXMixerInput(ISampleProvider input)
        {
            ISampleProvider mixerInput = ConvertToRightChannelCount(sfxMixer, input);
            sfxMixer.AddMixerInput(mixerInput);
        }

        private ISampleProvider AddBGMMixerInput(ISampleProvider input)
        {
            ISampleProvider mixerInput = ConvertToRightChannelCount(bgmMixer, input);
            bgmMixer.AddMixerInput(mixerInput);
            return mixerInput;
        }

        public void PlaySfx(SoundId trigger)
        {
            if (!sounds.ContainsKey(trigger))
            {
                return;
            }
            AddSFXMixerInput(new CachedSoundSampleProvider(sounds[trigger]));
        }

        public ISampleProvider? PlayBgm(BgmId id)
        {
            if (!bgmParts.ContainsKey(id))
            {
                Service.Log.Warning($"No BGM registered for {id}");
                return null;
            }
            else
            {
                return AddBGMMixerInput(new CachedSoundSampleProvider(bgmParts[id]));
            }
        }

        public void RegisterBgmPart(BgmId id, string path)
        {
            var part = new CachedSound(path);
            if(!bgmParts.ContainsKey(id))
            {
                bgmParts.Add(id, part);
            }
            else
            {
                bgmParts[id] = part;
            }
        }

        public void removeInput()
        {

        }

        public void RemoveAllBgm()
        {
            bgmMixer.RemoveAllMixerInputs();
        }

        public void Dispose()
        {
            Service.Log.Debug($"Dispose");
            sfxMixer.RemoveAllMixerInputs();
            bgmMixer.RemoveAllMixerInputs();
        }
    }
}
