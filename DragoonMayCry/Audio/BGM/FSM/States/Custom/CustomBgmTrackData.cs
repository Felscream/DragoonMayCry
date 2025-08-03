namespace DragoonMayCry.Audio.BGM.FSM.States.Custom
{
    public class CustomBgmTrackData(string stemId, BgmTrackData bgmTrackData)
    {
        public string StemId { get; private set; } = stemId;
        public BgmTrackData BgmTrackData { get; private set; } = bgmTrackData;
    }
}
