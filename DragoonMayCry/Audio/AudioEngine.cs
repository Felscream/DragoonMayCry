using Dalamud.Plugin.Services;
using DragoonMayCry.Audio.BGM;
using DragoonMayCry.Score.Model;
using DragoonMayCry.Score.Style;
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
    internal class AudioEngine : IDisposable
    {

        private readonly IWavePlayer sfxOutputDevice;
        private readonly IWavePlayer bgmOutputDevice;
        private readonly Dictionary<SoundId, CachedSound> announcerSfx;
        private readonly Dictionary<BgmId, CachedSound> bgmStems;
        private readonly VolumeSampleProvider sfxSampleProvider;
        private readonly MixingSampleProvider sfxMixer;
        private readonly VolumeSampleProvider bgmSampleProvider;
        private readonly MixingSampleProvider bgmMixer;

        public AudioEngine()
        {
            sfxOutputDevice = new WaveOutEvent();
            bgmOutputDevice = new WaveOutEvent();

            announcerSfx = new Dictionary<SoundId, CachedSound>();
            bgmStems = new Dictionary<BgmId, CachedSound>();

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

        public void RegisterAnnouncerSfx(Dictionary<SoundId, string> sfx)
        {
            foreach (KeyValuePair<SoundId, string> entry in sfx)
            {
                if(!File.Exists(entry.Value)) {
                    Service.Log.Error($"Could not find any file at {entry.Value}");
                    continue;
                }
                if (!announcerSfx.ContainsKey(entry.Key))
                {
                    announcerSfx.Add(entry.Key, new(entry.Value));
                }
                else
                {
                    announcerSfx[entry.Key] = new(entry.Value);
                }
                
            }
        }

        public void UpdateBgmVolume(float value)
        {
            bgmSampleProvider.Volume = value;
        }

        private void AddSFXMixerInput(ISampleProvider input)
        {
            sfxMixer.AddMixerInput(input);
        }

        private ISampleProvider AddBGMMixerInput(ISampleProvider input, double fadeInDuration)
        {
            if(fadeInDuration > 0)
            {
                var fadingInput = new FadeInOutSampleProvider(input, true);
                fadingInput.BeginFadeIn(fadeInDuration);
                bgmMixer.AddMixerInput(fadingInput);
                return fadingInput;
            } 
            
            bgmMixer.AddMixerInput(input);
            return input;
        }

        public void PlaySfx(SoundId trigger)
        {
            if (!announcerSfx.ContainsKey(trigger))
            {
                return;
            }
            AddSFXMixerInput(new CachedSoundSampleProvider(announcerSfx[trigger]));
        }

        public ISampleProvider? PlayBgm(BgmId id, double fadeInDuration = 0d)
        {
            if (!bgmStems.ContainsKey(id))
            {
                Service.Log.Warning($"No BGM registered for {id}");
                return null;
            }
            else
            {
                return AddBGMMixerInput(new CachedSoundSampleProvider(bgmStems[id]), fadeInDuration);
            }
        }

        public void RegisterBgmPart(BgmId id, string path)
        {
            var part = new CachedSound(path);
            if(!bgmStems.ContainsKey(id))
            {
                bgmStems.Add(id, part);
            }
            else
            {
                bgmStems[id] = part;
            }
        }

        public void ClearSfxCache()
        {
            announcerSfx.Clear();
        }

        public void ClearBgmCache()
        {
            bgmStems.Clear();
        }

        public void RemoveInput(ISampleProvider sample)
        {
            if(sample == null)
            {
                return;
            }
            bgmMixer.RemoveMixerInput(sample);
        }

        public void RemoveAllBgm()
        {
            bgmMixer.RemoveAllMixerInputs();
        }

        public void Dispose()
        {
            sfxMixer.RemoveAllMixerInputs();
            bgmMixer.RemoveAllMixerInputs();
        }
    }
}
