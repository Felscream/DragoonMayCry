namespace DragoonMayCry.Audio.BGM
{
    public class BgmTrackData(
        string audioPath,
        int effectiveStart,
        int transitionStart,
        int possibleTransitionTimeToNewState = int.MaxValue)
    {
        public string AudioPath { get; private set; } = audioPath;
        public int EffectiveStart { get; private set; } = effectiveStart;
        public int TransitionStart { get; private set; } = transitionStart;
        public int PossibleTransitionTimeToNewState { get; private set; } = possibleTransitionTimeToNewState;
    }
}
