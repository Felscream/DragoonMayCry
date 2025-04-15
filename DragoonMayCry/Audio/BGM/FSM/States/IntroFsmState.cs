#region

using System;

#endregion

namespace DragoonMayCry.Audio.BGM.FSM.States
{
    internal class EndCombatTiming(
        int transitionTime,
        int nextStateTransitionTime,
        int fadingDuration = 0,
        int fadeOutDelay = 0,
        int fadeOutDuration = 0)
    {
        public int TransitionTime => transitionTime;

        public int NextStateTransitionTime => nextStateTransitionTime;
        public int FadingDuration => fadingDuration;
        public int FadeOutDelay => fadeOutDelay;
        public int FadeOutDuration => fadeOutDuration;
    }

    internal abstract class IntroFsmState(
        AudioService audioService,
        int introFadingDuration,
        int cachedSampleFadeOutDuration,
        EndCombatTiming endCombatTiming) : BaseFsmState(audioService, cachedSampleFadeOutDuration)
    {

        protected IntroState State = IntroState.OutOfCombat;

        public override BgmState Id => BgmState.Intro;

        public override void Enter(bool fromLoop)
        {
            State = IntroState.OutOfCombat;
            var sample = AudioService.PlayBgm(BgmStemIds.Intro, introFadingDuration);
            if (sample != null)
            {
                Samples.Enqueue(sample);
            }

            TransitionTime = Stems[BgmStemIds.Intro].TransitionStart;
            CurrentTrackStopwatch.Restart();
        }
        public override void Update()
        {
            if (!CurrentTrackStopwatch.IsRunning)
            {
                return;
            }

            if (CurrentTrackStopwatch.ElapsedMilliseconds > TransitionTime)
            {

                if (State != IntroState.OutOfCombat)
                {
                    TransitionToNextState();
                }
                else
                {
                    var sample = AudioService.PlayBgm(BgmStemIds.Intro);
                    if (sample != null)
                    {
                        Samples.Enqueue(sample);
                    }
                    CurrentTrackStopwatch.Restart();
                }
            }
        }
        public override void Reset()
        {
            base.StopCachedSamples();
        }

        public override int Exit(ExitType exit)
        {
            if (!CurrentTrackStopwatch.IsRunning)
            {
                return 0;
            }
            if (exit == ExitType.ImmediateExit)
            {
                TransitionTime = 0;
                TransitionToNextState();
                return 0;
            }
            // we are already leaving this state, player transitioned rapidly between multiple ranks
            if (State != IntroState.OutOfCombat)
            {
                NextStateTransitionTime =
                    (int)Math.Max(NextStateTransitionTime - CurrentTrackStopwatch.Elapsed.TotalMilliseconds, 0);
            }
            else if (exit == ExitType.Promotion)
            {
                State = IntroState.CombatStart;
                NextStateTransitionTime = 0;
                TransitionToNextState();
            }

            if (exit == ExitType.EndOfCombat && State != IntroState.EndCombat)
            {
                State = IntroState.EndCombat;
                TransitionTime = endCombatTiming.TransitionTime;
                NextStateTransitionTime = endCombatTiming.NextStateTransitionTime;
                CurrentTrackStopwatch.Restart();
                AudioService.PlayBgm(BgmStemIds.CombatEnd, endCombatTiming.FadingDuration, endCombatTiming.FadeOutDelay,
                                     endCombatTiming.FadeOutDuration);
            }
            return NextStateTransitionTime;
        }
        public override bool CancelExit()
        {
            return false;
        }

        protected virtual void TransitionToNextState()
        {
            base.StopCachedSamples();
        }

        protected enum IntroState
        {
            OutOfCombat,
            CombatStart,
            EndCombat,
        }
    }
}
