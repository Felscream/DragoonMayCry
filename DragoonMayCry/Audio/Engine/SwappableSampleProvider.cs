using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Text;

namespace DragoonMayCry.Audio.Engine
{
    internal class SwappableSampleProvider : ISampleProvider
    {
        public ISampleProvider Source { get; set; }
        public WaveFormat WaveFormat => Source?.WaveFormat ?? throw new InvalidOperationException("Source sample provider is null.");

        public SwappableSampleProvider(ISampleProvider source)
        {
            Source = source;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            if (Source == null) throw new InvalidOperationException("Source sample provider is null.");
            return Source.Read(buffer, offset, count);
        }
    }
}
