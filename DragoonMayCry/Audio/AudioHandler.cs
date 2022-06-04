using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Dalamud.Logging;
namespace DragoonMayCry.Audio
{
    public enum AudioTrigger
    {
        BGM,
        D,
        C,
        B,
        A,
        S,
        SS,
        SSS
    }

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

    class AutoDisposeFileReader : ISampleProvider {
        private readonly AudioFileReader reader;
        private bool isDisposed;
        public AutoDisposeFileReader(AudioFileReader reader) {
            this.reader = reader;
            this.WaveFormat = reader.WaveFormat;
        }

        public int Read(float[] buffer, int offset, int count) {
            if (isDisposed)
                return 0;
            int read = reader.Read(buffer, offset, count);
            if (read == 0) {
                reader.Dispose();
                isDisposed = true;
            }
            return read;
        }

        public WaveFormat WaveFormat { get; private set; }
    }

    public class AudioHandler
    {
        private readonly IWavePlayer outputDevice;
        private readonly IWavePlayer bgmOutputDevice;
        private readonly Dictionary<AudioTrigger, CachedSound> sounds;
        private readonly VolumeSampleProvider sampleProvider;
        private readonly MixingSampleProvider mixer;
        private ISampleProvider bgmSampleProvider;
        private string bgmPath;

        public float SFXVolume
        {
            get => sampleProvider.Volume;
            set
            {
                sampleProvider.Volume = value;

            }
        }

        public AudioHandler(string combatMusic, string dAnnouncer, string cAnnouncer, string bAnnouncer, string aAnnouncer, string sAnnouncer, string ssAnnouncer, string sssAnnouncer)
        {
            outputDevice = new WaveOutEvent();
            bgmOutputDevice = new WaveOutEvent();
            sounds = new Dictionary<AudioTrigger, CachedSound>();
            //sounds.Add(AudioTrigger.BGM, new(combatMusic));
            mixer = new(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));
            mixer.ReadFully = true;

            bgmPath = combatMusic;
            sampleProvider = new(mixer);

            outputDevice.Init(sampleProvider);
            outputDevice.Play();
        }

        private ISampleProvider ConvertToRightChannelCount(ISampleProvider input)
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


        private ISampleProvider AddMixerInput(ISampleProvider input)
        {
            ISampleProvider mixerInput = ConvertToRightChannelCount(input);
            mixer.AddMixerInput(mixerInput);
            return mixerInput;
        }

        public void PlaySound(AudioTrigger trigger)
        {
            AddMixerInput(new CachedSoundSampleProvider(sounds[trigger]));
        }


        public void PlayBGM() {
            var input = new AudioFileReader(bgmPath);
            bgmSampleProvider = AddMixerInput(new AutoDisposeFileReader(input));
            PluginLog.Debug("Playing BGM");
        }
        public void StopBGM() {
            mixer.RemoveMixerInput(bgmSampleProvider);
            PluginLog.Debug("Stopping BGM");
        }
    }
}