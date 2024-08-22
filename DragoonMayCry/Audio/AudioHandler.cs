using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Dalamud.Logging;
using DragoonMayCry.Style;
namespace DragoonMayCry.Audio
{

    // Cached sound concept lovingly borrowed from: https://markheath.net/post/fire-and-forget-audio-playback-with
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

    public class AudioHandler
    {
        private readonly IWavePlayer sfxOutputDevice;
        private readonly IWavePlayer bgmOutputDevice;
        private readonly Dictionary<StyleType, CachedSound> sounds;
        private readonly VolumeSampleProvider sfxSampleProvider;
        private readonly VolumeSampleProvider bgmSampleProvider;
        private readonly MixingSampleProvider sfxMixer;
        private readonly MixingSampleProvider bgmMixer;
        private ISampleProvider? bgmLoopStream;
        private string bgmPath;

        public float SFXVolume
        {
            get => sfxSampleProvider.Volume;
            set
            {
                sfxSampleProvider.Volume = value;

            }
        }

        public float BGMVolume {
            get => bgmSampleProvider.Volume;
            set {
                bgmSampleProvider.Volume = value;

            }
        }

        public AudioHandler(string combatMusic, string dAnnouncer, string cAnnouncer, string bAnnouncer, string aAnnouncer, string sAnnouncer, string ssAnnouncer, string sssAnnouncer)
        {
            sfxOutputDevice = new WaveOutEvent();
            bgmOutputDevice = new WaveOutEvent();

            sounds = new Dictionary<StyleType, CachedSound>();
            sounds.Add(StyleType.D, new(dAnnouncer));
            sounds.Add(StyleType.C, new(cAnnouncer));
            sounds.Add(StyleType.B, new(bAnnouncer));
            sounds.Add(StyleType.A, new(aAnnouncer));
            sounds.Add(StyleType.S, new(sAnnouncer));
            sounds.Add(StyleType.SS, new(ssAnnouncer));
            sounds.Add(StyleType.SSS, new(sssAnnouncer));
            bgmPath = combatMusic;

            sfxMixer = new(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2)) {
                ReadFully = true
            };

            bgmMixer = new(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2)) {
                ReadFully = true
            };

            sfxSampleProvider = new(sfxMixer);
            bgmSampleProvider = new(bgmMixer);

            SFXVolume = 0.1f;
            BGMVolume = 0.1f;

            sfxOutputDevice.Init(sfxSampleProvider);
            sfxOutputDevice.Play();

            bgmOutputDevice.Init(bgmSampleProvider);
            bgmOutputDevice.Play();
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
        
        private ISampleProvider AddBGMMixerInput(ISampleProvider input)
        {
            ISampleProvider mixerInput = ConvertToRightChannelCount(input);
            bgmMixer.AddMixerInput(mixerInput);
            return mixerInput;
        }

        public void PlaySFX(StyleType trigger)
        {
            if (trigger == StyleType.NO_STYLE) {
                return;
            }
            AddSFXMixerInput(new CachedSoundSampleProvider(sounds[trigger]));
        }

        public void PlayBGM() {
            var input = new LoopStream(new AudioFileReader(bgmPath));
            var sample = new FadeInOutSampleProvider(input.ToSampleProvider(), true);
            sample.BeginFadeIn(4000);
            bgmLoopStream = AddBGMMixerInput(sample);
        }
        public void StopBGM() {
            ((FadeInOutSampleProvider)bgmLoopStream).BeginFadeOut(4000);
        }
    }
}
