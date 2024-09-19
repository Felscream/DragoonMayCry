using DragoonMayCry.Audio.BGM;
using DragoonMayCry.Audio.StyleAnnouncer;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.IO;

namespace DragoonMayCry.Audio.Engine
{
    internal class AudioEngine : IDisposable
    {

        private WasapiOut sfxOutputDevice;
        private WasapiOut bgmOutputDevice;
        private readonly Dictionary<SoundId, CachedSound> announcerSfx;
        private readonly VolumeSampleProvider sfxSampleProvider;
        private readonly MixingSampleProvider sfxMixer;
        private readonly VolumeSampleProvider bgmSampleProvider;
        private readonly MixingSampleProvider bgmMixer;
        private readonly MMDeviceEnumerator deviceEnumerator;
        private readonly DeviceNotificationClient notificationClient;
        private Dictionary<BgmId, CachedSound> bgmStems;

        public AudioEngine()
        {
            sfxOutputDevice = new WasapiOut();
            bgmOutputDevice = new WasapiOut();

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

            deviceEnumerator = new MMDeviceEnumerator();
            notificationClient = new DeviceNotificationClient();
            notificationClient.OnDefaultOutputDeviceChanged += OnDefaultDeviceChanged;
            deviceEnumerator.RegisterEndpointNotificationCallback(notificationClient);
        }

        private void OnDefaultDeviceChanged()
        {
            bgmOutputDevice.Stop();
            sfxOutputDevice.Stop();
            bgmOutputDevice = new WasapiOut();
            sfxOutputDevice = new WasapiOut();
            bgmOutputDevice.Init(bgmSampleProvider);
            sfxOutputDevice.Init(sfxSampleProvider);
            bgmOutputDevice.Play();
            sfxOutputDevice.Play();
        }

        public void UpdateSfxVolume(float value)
        {
            sfxSampleProvider.Volume = value;
        }

        public void RegisterAnnouncerSfx(Dictionary<SoundId, string> sfx)
        {
            foreach (var entry in sfx)
            {
                if (!File.Exists(entry.Value))
                {
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
            if (fadeInDuration > 0)
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

        public void PlaySfx(string path)
        {
            ISampleProvider sample;
            try
            {
                var sound = new CachedSound(path);
                sample = new CachedSoundSampleProvider(sound);
            }
            catch (Exception e)
            {
                Service.Log.Error(e, $"Error while reading file ${path}");
                return;
            }

            sfxMixer.AddMixerInput(sample);
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

        public Dictionary<BgmId, CachedSound> RegisterBgm(Dictionary<BgmId, string> paths)
        {
            Dictionary<BgmId, CachedSound> bgm = new();
            foreach (var entry in paths)
            {
                if (!File.Exists(entry.Value))
                {
                    throw new FileNotFoundException($"File {entry.Value} does not exist");
                }

                var part = new CachedSound(entry.Value);
                if (!bgm.ContainsKey(entry.Key))
                {
                    bgm.Add(entry.Key, part);
                }
                else
                {
                    bgm[entry.Key] = part;
                }
            }

            bgmStems = bgm;
            return bgm;
        }

        public void LoadBgm(Dictionary<BgmId, CachedSound> toLoad)
        {
            bgmStems = toLoad;
        }

        public void ClearSfxCache()
        {
            announcerSfx.Clear();
        }

        public void RemoveInput(ISampleProvider sample)
        {
            if (sample == null)
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
            deviceEnumerator.UnregisterEndpointNotificationCallback(notificationClient);
            deviceEnumerator.Dispose();
            sfxMixer.RemoveAllMixerInputs();
            bgmMixer.RemoveAllMixerInputs();
            sfxOutputDevice.Dispose();
            bgmOutputDevice.Dispose();
        }

        class DeviceNotificationClient : NAudio.CoreAudioApi.Interfaces.IMMNotificationClient
        {
            public delegate void DefaultDeviceChanged();
            public DefaultDeviceChanged? OnDefaultOutputDeviceChanged;
            public void OnDeviceStateChanged(string deviceId, DeviceState newState)
            {
                return;
            }

            public void OnDeviceAdded(string pwstrDeviceId)
            {
                return;
            }

            public void OnDeviceRemoved(string deviceId)
            {
                return;
            }

            public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
            {
                OnDefaultOutputDeviceChanged?.Invoke();
            }

            public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key)
            {
                return;
            }
        }
    }
}
