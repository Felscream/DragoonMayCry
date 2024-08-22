using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DragoonMayCry.UI
{
    public sealed class StyleRankUI
    {

        public void Draw() {
            ImGui.PushStyleColor(ImGuiCol.WindowBg, Plugin.Configuration.FloatingWindowConfiguration.BackgroundColor);

            if (ImGui.Begin("DragoonMayCry score"))
            {
                ImGui.Text(Plugin.StyleRankHandler.CurrentStyle.Value.StyleType.ToString());
            }
        }
    }
}
