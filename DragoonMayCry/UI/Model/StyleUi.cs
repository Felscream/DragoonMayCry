using System.Numerics;

namespace DragoonMayCry.UI.Model
{
    public class StyleUi
    {
        public string IconPath { get; private set; }
        public Vector3 GaugeColor { get; private set; }

        public StyleUi(string iconPath, Vector3 gaugeColor)
        {
            IconPath = iconPath;
            GaugeColor = gaugeColor;
        }
    }
}
