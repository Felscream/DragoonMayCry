using NAudio.Wave;

namespace DragoonMayCry.Audio.Engine
{
    //
    // Summary:
    //     This is a copy of NAudio's FadeInOutSampleProvider with the fading state exposed
    public class ExposedFadeInOutSampleProvider : ISampleProvider
    {
        public enum FadeState
        {
            Silence,
            FadingIn,
            FullVolume,
            FadingOut,
        }

        private readonly object lockObject = new();

        private readonly ISampleProvider source;
        private long fadeOutDelayPosition;
        private long fadeOutDelaySamples;

        private int fadeSampleCount;

        private int fadeSamplePosition;

        //
        // Summary:
        //     Creates a new FadeInOutSampleProvider
        //
        // Parameters:
        //   source:
        //     The source stream with the audio to be faded in or out
        //
        //   initiallySilent:
        //     If true, we start faded out
        public ExposedFadeInOutSampleProvider(ISampleProvider source, bool initiallySilent = false)
        {
            this.source = source;
            fadeState = !initiallySilent ? FadeState.FullVolume : FadeState.Silence;
        }

        public FadeState fadeState { get; private set; }

        //
        // Summary:
        //     WaveFormat of this SampleProvider
        public WaveFormat WaveFormat => source.WaveFormat;

        //
        // Summary:
        //     Reads samples from this sample provider
        //
        // Parameters:
        //   buffer:
        //     Buffer to read into
        //
        //   offset:
        //     Offset within buffer to write to
        //
        //   count:
        //     Number of samples desired
        //
        // Returns:
        //     Number of samples read
        public int Read(float[] buffer, int offset, int count)
        {
            var num = source.Read(buffer, offset, count);
            lock (lockObject)
            {
                if (fadeOutDelaySamples > 0)
                {
                    fadeOutDelayPosition += num / WaveFormat.Channels;
                    if (fadeOutDelayPosition >= fadeOutDelaySamples)
                    {
                        fadeOutDelaySamples = 0;
                        fadeState = FadeState.FadingOut;
                    }
                }

                if (fadeState == FadeState.FadingIn)
                {
                    FadeIn(buffer, offset, num);
                }
                else if (fadeState == FadeState.FadingOut)
                {
                    FadeOut(buffer, offset, num);
                }
                else if (fadeState == FadeState.Silence)
                {
                    ClearBuffer(buffer, offset, count);
                }
            }

            return num;
        }

        //
        // Summary:
        //     Requests that a fade-in begins (will start on the next call to Read)
        //
        // Parameters:
        //   fadeDurationInMilliseconds:
        //     Duration of fade in milliseconds
        public void BeginFadeIn(double fadeDurationInMilliseconds)
        {
            lock (lockObject)
            {
                fadeSamplePosition = 0;
                fadeSampleCount = (int)(fadeDurationInMilliseconds * source.WaveFormat.SampleRate / 1000.0);
                fadeState = FadeState.FadingIn;
            }
        }

        //
        // Summary:
        //     Requests that a fade-out begins (will start on the next call to Read)
        //
        // Parameters:
        //   fadeDurationInMilliseconds:
        //     Duration of fade in milliseconds
        public void BeginFadeOut(double fadeDurationInMilliseconds, double fadeAfterMilliseconds = 0)
        {
            lock (lockObject)
            {
                fadeSamplePosition = 0;
                fadeSampleCount = (int)(fadeDurationInMilliseconds * source.WaveFormat.SampleRate / 1000.0);
                fadeOutDelaySamples = (int)(fadeAfterMilliseconds * source.WaveFormat.SampleRate / 1000.0);
                fadeOutDelayPosition = 0;
                if (fadeOutDelaySamples == 0)
                {
                    fadeState = FadeState.FadingOut;
                }

            }
        }

        private static void ClearBuffer(float[] buffer, int offset, int count)
        {
            for (var i = 0; i < count; i++)
            {
                buffer[i + offset] = 0f;
            }
        }

        private void FadeOut(float[] buffer, int offset, int sourceSamplesRead)
        {
            var num = 0;
            while (num < sourceSamplesRead)
            {
                var num2 = 1f - fadeSamplePosition / (float)fadeSampleCount;
                for (var i = 0; i < source.WaveFormat.Channels; i++)
                {
                    buffer[offset + num++] *= num2;
                }

                fadeSamplePosition++;
                if (fadeSamplePosition > fadeSampleCount)
                {
                    fadeState = FadeState.Silence;
                    ClearBuffer(buffer, num + offset, sourceSamplesRead - num);
                    break;
                }
            }
        }

        private void FadeIn(float[] buffer, int offset, int sourceSamplesRead)
        {
            var num = 0;
            while (num < sourceSamplesRead)
            {
                var num2 = fadeSamplePosition / (float)fadeSampleCount;
                for (var i = 0; i < source.WaveFormat.Channels; i++)
                {
                    buffer[offset + num++] *= num2;
                }

                fadeSamplePosition++;
                if (fadeSamplePosition > fadeSampleCount)
                {
                    fadeState = FadeState.FullVolume;
                    break;
                }
            }
        }
    }
}
