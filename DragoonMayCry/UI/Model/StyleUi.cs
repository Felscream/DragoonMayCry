using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace DragoonMayCry.UI.Model
{
    internal class StyleUi
    {
        public String IconPath { get; private set; }
        public Vector3 GaugeColor { get; private set; }

        public StyleUi(String iconPath, Vector3 gaugeColor)
        {
            IconPath = iconPath;
            GaugeColor = gaugeColor;
        }
    }
}
