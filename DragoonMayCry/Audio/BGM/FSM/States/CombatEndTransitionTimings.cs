namespace DragoonMayCry.Audio.BGM.FSM.States
{
    internal class CombatEndTransitionTimings(
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
}
