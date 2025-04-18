using NAudio.Vorbis;
using NAudio.Wave;
using System.Collections.Generic;
using System.Linq;

namespace DragoonMayCry.Audio.Engine
{
    internal class CachedSound
    {
        internal CachedSound(string audioFileName)
        {
            using (var audioFileReader = new VorbisWaveReader(audioFileName))
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
        internal float[] AudioData { get; private set; }
        internal WaveFormat WaveFormat { get; private set; }
    }
}
