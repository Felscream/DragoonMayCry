using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DragoonMayCry.Style;
using Dalamud.Interface.Utility;
using Dalamud.Plugin.Services;
using System.Reflection;
using FFXIVClientStructs.FFXIV.Common.Math;

namespace DragoonMayCry.UI
{
    public sealed class StyleRankUI
    {

        public void Draw() {
            var flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoScrollbar |
                                     ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize;
            if (Plugin.Configuration.StyleRankUiConfiguration.LockScoreWindow)
            {
                flags |= ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoMove  | ImGuiWindowFlags.NoMouseInputs |
                         ImGuiWindowFlags.NoInputs;
            } else
            {
                flags &= ImGuiWindowFlags.NoBackground & ImGuiWindowFlags.NoMove & ImGuiWindowFlags.NoMouseInputs &
                         ImGuiWindowFlags.NoInputs;
            }


            StyleRank currentStyleRank = Plugin.ScoreManager.CurrentRank;

            if (ImGui.Begin("DragoonMayCry score", flags))
            {
                if (Service.TextureProvider.GetFromManifestResource(Assembly.GetExecutingAssembly(), currentStyleRank.IconPath).TryGetWrap(out var rankIcon, out var _))
                {
                    var size = new Vector2(130, 130);
                    ImGui.Image(rankIcon.ImGuiHandle, size);
                }
            }
        }
    }
}
