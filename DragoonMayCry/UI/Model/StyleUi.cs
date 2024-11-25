using System.Numerics;

namespace DragoonMayCry.UI.Model
{
    public class StyleUi(string iconPath, Vector3 gaugeColor, uint goldSaucerIconId)
    {
        public string IconPath { get; private set; } = iconPath;
        public Vector3 GaugeColor { get; private set; } = gaugeColor;
        public uint GoldSaucerEditionIconId { get; private set; } = goldSaucerIconId;
    }
}
