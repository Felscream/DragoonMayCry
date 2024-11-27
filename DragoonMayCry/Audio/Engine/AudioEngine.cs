using DragoonMayCry.Audio.BGM;
using DragoonMayCry.Audio.StyleAnnouncer;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private ISampleProvider lastBgmSampleApplied;

        public AudioEngine()
        {
            deviceEnumerator = new MMDeviceEnumerator();
            sfxOutputDevice = new WasapiOut(deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console),
                                            AudioClientShareMode.Shared,
                                            true, 200);
            bgmOutputDevice = new WasapiOut(deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console),
                                            AudioClientShareMode.Shared,
                                            true, 20);

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


            notificationClient = new DeviceNotificationClient();
            notificationClient.DefaultOutputDeviceChanged += OnDefaultDeviceChanged;
            deviceEnumerator.RegisterEndpointNotificationCallback(notificationClient);

            lastBgmSampleApplied = bgmSampleProvider;
        }

        private void OnDefaultDeviceChanged()
        {
            bgmOutputDevice.Stop();
            sfxOutputDevice.Stop();
            bgmOutputDevice = new WasapiOut(deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console),
                                            AudioClientShareMode.Shared,
                                            true, 20);
            sfxOutputDevice = new WasapiOut(deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console),
                                            AudioClientShareMode.Shared,
                                            true, 200);
            bgmOutputDevice.Init(lastBgmSampleApplied);
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
            if (!announcerSfx.ContainsKey(trigger))
            {
                return;
            }

            AddSfxMixerInput(new CachedSoundSampleProvider(announcerSfx[trigger]));
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
            BgmId id, double fadeInDuration = 0d, double fadeOutDelay = 0, double fadeOutDuration = 0)
        {
            if (!bgmStems.ContainsKey(id))
            {
                Service.Log.Warning($"No BGM registered for {id}");
                return null;
            }

            ISampleProvider sample = new CachedSoundSampleProvider(bgmStems[id]);

            return AddBgmMixerInput(sample, fadeInDuration, fadeOutDelay, fadeOutDuration);
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
                bgm.Add(entry.Key, part);
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

        public void ApplyDeathEffect()
        {
            var deathEffect = new DeathEffect(bgmSampleProvider, 500, 200, 0.35f);
            bgmOutputDevice.Stop();
            bgmOutputDevice.Dispose();
            bgmOutputDevice = new WasapiOut(deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console),
                                            AudioClientShareMode.Shared,
                                            true, 20);
            bgmOutputDevice.Init(deathEffect);
            bgmOutputDevice.Play();
            lastBgmSampleApplied = deathEffect;
        }

        [Conditional("DEBUG")]
        public void ApplyDecay(float value)
        {
            var deathEffect = new DeathEffect(bgmSampleProvider, 500, 200, value);
            bgmOutputDevice.Stop();
            bgmOutputDevice.Dispose();
            bgmOutputDevice = new WasapiOut(deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console),
                                            AudioClientShareMode.Shared,
                                            true, 20);
            bgmOutputDevice.Init(deathEffect);
            bgmOutputDevice.Play();
            lastBgmSampleApplied = deathEffect;
        }

        public void RemoveDeathEffect()
        {
            bgmOutputDevice.Stop();
            bgmOutputDevice.Dispose();
            bgmOutputDevice = new WasapiOut(deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console),
                                            AudioClientShareMode.Shared,
                                            true, 20);
            bgmOutputDevice.Init(bgmSampleProvider);
            bgmOutputDevice.Play();
            lastBgmSampleApplied = bgmSampleProvider;
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
                if (input is ExposedFadeInOutSampleProvider)
                {
                    var fadingInput = (ExposedFadeInOutSampleProvider)input;
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

        public void Dispose()
        {
            sfxMixer.RemoveAllMixerInputs();
            bgmMixer.RemoveAllMixerInputs();
            sfxOutputDevice.Dispose();
            bgmOutputDevice.Dispose();
            deviceEnumerator.UnregisterEndpointNotificationCallback(notificationClient);
            deviceEnumerator.Dispose();
        }

        private class DeviceNotificationClient : NAudio.CoreAudioApi.Interfaces.IMMNotificationClient
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
