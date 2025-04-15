#region

using DragoonMayCry.Audio.Engine;
using NAudio.Wave;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

#endregion

namespace DragoonMayCry.Audio.BGM.FSM.States
{
    internal abstract class BaseFsmState(AudioService audioService, int cachedSampleFadeOutDuration) : IFsmState
    {
        protected readonly AudioService AudioService = audioService;
        protected readonly int CachedSampleFadeOutDuration = cachedSampleFadeOutDuration;
        protected readonly Stopwatch CurrentTrackStopwatch = new();
        protected readonly Queue<ISampleProvider> Samples = new();
        protected LinkedListNode<string>? CurrentTrack;
        //indicates when the next FSM state transition will take place
        protected int NextStateTransitionTime;

        // indicates when we can change tracks in this state
        protected int TransitionTime;
        protected abstract Dictionary<string, BgmTrackData> Stems { get; }
        public abstract BgmState Id { get; }
        public virtual Dictionary<string, string> GetBgmPaths()
        {
            return Stems.ToDictionary(entry => entry.Key, entry => entry.Value.AudioPath);
        }
        public abstract void Enter(bool fromLoop);
        public abstract void Update();
        public abstract void Reset();
        public abstract int Exit(ExitType exit);
        public abstract bool CancelExit();

        protected virtual void StopCachedSamples()
        {
            while (Samples.TryDequeue(out var sample))
            {
                if (sample is ExposedFadeInOutSampleProvider provider)
                {
                    if (provider.fadeState == ExposedFadeInOutSampleProvider.FadeState.FullVolume)
                    {
                        provider.BeginFadeOut(CachedSampleFadeOutDuration);
                        continue;
                    }
                }
                AudioService.RemoveBgmPart(sample);
            }

            CurrentTrackStopwatch.Reset();
        }
    }
}
