using DragoonMayCry.Audio.StyleAnnouncer;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace DragoonMayCry.Audio.Engine
{
    internal class AudioEngine : IDisposable
    {
        private readonly ConcurrentDictionary<SoundId, CachedSound> announcerSfx;
        private readonly MixingSampleProvider bgmMixer;
        private readonly VolumeSampleProvider bgmSampleProvider;
        private readonly SwappableSampleProvider swappableBgmProvider;
        private MMDeviceEnumerator? deviceEnumerator;
        private DeviceNotificationClient? notificationClient;
        private readonly MixingSampleProvider sfxMixer;
        private readonly VolumeSampleProvider sfxSampleProvider;
        private IWavePlayer bgmOutputDevice;
        private ConcurrentDictionary<string, CachedSound> bgmStems;
        private IWavePlayer sfxOutputDevice;

        public AudioEngine()
        {
            try
            {
                deviceEnumerator = new MMDeviceEnumerator();
                notificationClient = new DeviceNotificationClient();
                notificationClient.DefaultOutputDeviceChanged += OnDefaultDeviceChanged;
                deviceEnumerator.RegisterEndpointNotificationCallback(notificationClient);
            }
            catch (Exception e)
            {
                Service.Log.Error(e, "Failed to create MMDeviceEnumerator. Falling back to WaveOutEvent.");
                deviceEnumerator = null;
                notificationClient = null;
            }
            
            sfxOutputDevice = CreateSfxDevice();
            bgmOutputDevice = CreateBgmDevice();

            announcerSfx = new ConcurrentDictionary<SoundId, CachedSound>();
            bgmStems = new ConcurrentDictionary<string, CachedSound>();

            sfxMixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2))
            {
                ReadFully = true,
            };


            bgmMixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(48000, 2))
            {
                ReadFully = true,
            };

            sfxSampleProvider = new VolumeSampleProvider(sfxMixer);
            bgmSampleProvider = new VolumeSampleProvider(bgmMixer);
            swappableBgmProvider = new SwappableSampleProvider(bgmSampleProvider);

            sfxOutputDevice.Init(sfxSampleProvider);
            bgmOutputDevice.Init(swappableBgmProvider);

            sfxOutputDevice.Play();
            bgmOutputDevice.Play();
        }

        private IWavePlayer CreateSfxDevice()
        {
            if (deviceEnumerator != null)
            {
                try
                {
                    return new WasapiOut(deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console),
                                     AudioClientShareMode.Shared,
                                     true, 200);
                } catch (Exception e)
                {
                    Service.Log.Error(e, "Failed to create WasapiOut for SFX. Falling back to WaveOutEvent.");
                }
                
            }
            return new WaveOutEvent { DesiredLatency = 200 };
        }

        private IWavePlayer CreateBgmDevice()
        {
            if (deviceEnumerator != null)
            {
                try
                {
                    return new WasapiOut(deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console),
                                     AudioClientShareMode.Shared,
                                     true, 20);
                }
                catch (Exception e)
                {
                    Service.Log.Error(e, "Failed to create WasapiOut for BGM. Falling back to WaveOutEvent.");
                }
            }
            return new WaveOutEvent { DesiredLatency = 150 };
        }

        private void OnDefaultDeviceChanged()
        {
            if (deviceEnumerator == null) return;

            bgmOutputDevice.Stop();
            bgmOutputDevice.Dispose();
            sfxOutputDevice.Stop();
            sfxOutputDevice.Dispose();

            bgmOutputDevice = CreateBgmDevice();
            sfxOutputDevice = CreateSfxDevice();

            bgmOutputDevice.Init(swappableBgmProvider);
            sfxOutputDevice.Init(sfxSampleProvider);

            bgmOutputDevice.Play();
            sfxOutputDevice.Play();
        }



        public void Dispose()
        {
            sfxMixer.RemoveAllMixerInputs();
            bgmMixer.RemoveAllMixerInputs();
            sfxOutputDevice.Dispose();
            bgmOutputDevice.Dispose();
            
            if (deviceEnumerator != null && notificationClient != null)
            {
                deviceEnumerator.UnregisterEndpointNotificationCallback(notificationClient);
                deviceEnumerator.Dispose();
            }
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

                var cachedSfx = new CachedSound(entry.Value);
                announcerSfx.AddOrUpdate(entry.Key, cachedSfx, (_, _) => cachedSfx);
            }
        }

        public void UpdateBgmVolume(float value)
        {
            bgmSampleProvider.Volume = value;
        }

        private void AddSfxMixerInput(ISampleProvider input)
        {
            sfxMixer.AddMixerInput(input);
        }

        private ExposedFadeInOutSampleProvider AddBgmMixerInput(
            ISampleProvider input, double fadeInDuration, double fadeOutDelay, double fadeOutDuration)
        {
            var fadingInput = new ExposedFadeInOutSampleProvider(input);
            if (fadeInDuration > 0)
            {
                fadingInput.BeginFadeIn(fadeInDuration);
            }

            if (fadeOutDuration > 0)
            {
                fadingInput.BeginFadeOut(fadeOutDuration, fadeOutDelay);
            }

            bgmMixer.AddMixerInput(fadingInput);
            return fadingInput;
        }

        public void PlaySfx(SoundId trigger)
        {
            if (announcerSfx.TryGetValue(trigger, out var value))
            {
                AddSfxMixerInput(new CachedSoundSampleProvider(value));
            }
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

        public ISampleProvider? PlayBgm(
            string id, double fadeInDuration = 0d, double fadeOutDelay = 0, double fadeOutDuration = 0)
        {
            if (!bgmStems.TryGetValue(id, out var stem))
            {
                Service.Log.Warning($"No BGM registered for {id}");
                return null;
            }

            ISampleProvider sample = new CachedSoundSampleProvider(stem);

            return AddBgmMixerInput(sample, fadeInDuration, fadeOutDelay, fadeOutDuration);
        }

        public ConcurrentDictionary<string, CachedSound> RegisterBgm(Dictionary<string, string> paths)
        {
            ConcurrentDictionary<string, CachedSound> bgm = new();
            foreach (var entry in paths)
            {
                if (!File.Exists(entry.Value))
                {
                    throw new FileNotFoundException($"File {entry.Value} does not exist");
                }

                var part = new CachedSound(entry.Value);
                bgm.AddOrUpdate(entry.Key, part, (_, _) => part);
            }

            bgmStems = bgm;
            return bgm;
        }

        public void LoadBgm(ConcurrentDictionary<string, CachedSound> toLoad)
        {
            bgmStems = toLoad;
        }

        public void ClearSfxCache()
        {
            announcerSfx.Clear();
        }

        public void ApplyDeathEffect()
        {
            var deathEffect = new DeathEffect(bgmSampleProvider, 500, 200, 0.35f);
            swappableBgmProvider.Source = deathEffect;
        }

        [Conditional("DEBUG")]
        public void ApplyDecay(float value)
        {
            var deathEffect = new DeathEffect(bgmSampleProvider, 500, 200, value);
            swappableBgmProvider.Source = deathEffect;
        }

        public void RemoveDeathEffect()
        {
            swappableBgmProvider.Source = bgmSampleProvider;
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

        public void FadeOutBgm(float fadeOutDuration)
        {
            if (fadeOutDuration == 0)
            {
                RemoveAllBgm();
                return;
            }

            var inputs = new List<ISampleProvider>(bgmMixer.MixerInputs);
            foreach (var input in inputs)
            {
                if (input is ExposedFadeInOutSampleProvider fadingInput)
                {
                    if (fadingInput.fadeState == ExposedFadeInOutSampleProvider.FadeState.FullVolume)
                    {
                        fadingInput.BeginFadeOut(fadeOutDuration);
                    }
                    else if (fadingInput.fadeState != ExposedFadeInOutSampleProvider.FadeState.FadingOut)
                    {
                        bgmMixer.RemoveMixerInput(fadingInput);
                    }
                }
                else
                {
                    bgmMixer.RemoveMixerInput(input);
                }
            }
        }

        private class DeviceNotificationClient : IMMNotificationClient
        {
            public delegate void DefaultDeviceChanged();

            public DefaultDeviceChanged? DefaultOutputDeviceChanged;

            public void OnDeviceStateChanged(string deviceId, DeviceState newState) { }

            public void OnDeviceAdded(string pwstrDeviceId) { }

            public void OnDeviceRemoved(string deviceId) { }

            public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
            {
                DefaultOutputDeviceChanged?.Invoke();
            }

            public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key) { }
        }
    }
}
