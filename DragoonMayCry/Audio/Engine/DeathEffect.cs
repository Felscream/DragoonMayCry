using NAudio.Dsp;
using NAudio.Wave;

namespace DragoonMayCry.Audio.Engine
{
    // It's a mix of low pass filter and reverb
    internal class DeathEffect : ISampleProvider
    {
        private readonly ISampleProvider source;
        private readonly BiQuadFilter lowPassFilter;
        //stores samples to create the delay effect
        private readonly float[] reverbDelayBuffer;

        // how much of the delayed feedback is sent back into the buffer
        private readonly float decay;

        private int reverbBufferPosition;

        public DeathEffect(ISampleProvider sampleProvider, float cutoffFrequency, int reverbDelayTime, float decayFactor)
        {
            source = sampleProvider;

            lowPassFilter = BiQuadFilter.LowPassFilter(sampleProvider.WaveFormat.SampleRate, cutoffFrequency, 1f);
            decay = decayFactor;
            var reverbSamples = (int)(sampleProvider.WaveFormat.SampleRate * (reverbDelayTime / 1000.0f) * sampleProvider.WaveFormat.Channels);

            reverbDelayBuffer = new float[reverbSamples];
        }

        public WaveFormat WaveFormat { get { return source.WaveFormat; } }

        public int Read(float[] buffer, int offset, int count)
        {
            var read = source.Read(buffer, offset, count);
            for (var i = 0; i < read; i++)
            {
                var drySample = buffer[offset + i];
                var wetSample = reverbDelayBuffer[reverbBufferPosition] * decay;

                var fedSample = lowPassFilter.Transform(drySample + wetSample) * 0.9f;
                buffer[offset + i] = fedSample;

                reverbDelayBuffer[reverbBufferPosition] = fedSample;
                reverbBufferPosition = (reverbBufferPosition + 1) % reverbDelayBuffer.Length;
            }
            return read;
        }
    }
}
