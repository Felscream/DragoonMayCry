using NAudio.Dsp;
using NAudio.Wave;

namespace DragoonMayCry.Audio.Engine
{
    // It's a mix of low pass filter , reverb and chorus all in one
    // TODO add chorus
    internal class DeathEffect : ISampleProvider
    {
        private readonly ISampleProvider source;
        private readonly BiQuadFilter lowPassFilter;
        //stores samples to create the delay effect
        private readonly float[] reverbDelayBuffer;

        // how much of the delayed feedback is sent back into the buffer
        private readonly float decay;

        private int reverbBufferPosition;

        public DeathEffect(ISampleProvider sampleProvider, float sampleRate, float cutoffFrequency, int reverbDelayTime, float decayFactor)
        {
            source = sampleProvider;
            lowPassFilter = BiQuadFilter.LowPassFilter(sampleRate, cutoffFrequency, 1f);
            decay = decayFactor;
            var reverbSamples = (int)(sampleProvider.WaveFormat.SampleRate * (reverbDelayTime / 1000.0f));

            reverbDelayBuffer = new float[reverbSamples];
        }
        public WaveFormat WaveFormat { get { return source.WaveFormat; } }

        public int Read(float[] buffer, int offset, int count)
        {
            var read = source.Read(buffer, offset, count);
            for (var i = 0; i < read; i++)
            {
                var drySample = buffer[offset + i];
                var wetSample = reverbDelayBuffer[reverbBufferPosition];
                var fedSample = lowPassFilter.Transform(drySample) + wetSample * decay;

                buffer[offset + i] = fedSample;


                reverbDelayBuffer[reverbBufferPosition] = fedSample;
                if (reverbBufferPosition >= reverbDelayBuffer.Length)
                {
                    reverbBufferPosition = 0;
                }

            }
            return read;
        }
    }
}
