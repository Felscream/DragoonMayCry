using Dalamud.Interface;
using Dalamud.Interface.Textures.TextureWraps;
using DragoonMayCry.Style;
using FFXIVClientStructs.FFXIV.Common.Math;
using ImGuiNET;
using System.Reflection;
using DragoonMayCry.Score;
using static DragoonMayCry.Score.ScoreManager;

namespace DragoonMayCry.UI
{
    public sealed class StyleRankUI
    {
        private readonly ScoreProgressBar scoreProgressBar;
        public StyleRankUI(ScoreProgressBar scoreProgressBar)
        {
            this.scoreProgressBar = scoreProgressBar;
        }

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


            ScoreRank rank = Plugin.ScoreManager.GetScoreRankToDisplay();
            //ImGui.SetNextWindowSize(new System.Numerics.Vector2(150,180));
            if (rank != null && ImGui.Begin("DragoonMayCry score", flags))
            {
                ImGui.Text($"{rank.Score}");
                if (Service.TextureProvider.GetFromManifestResource(Assembly.GetExecutingAssembly(), rank.Rank.IconPath).TryGetWrap(out var rankIcon, out var _))
                {
                    var size = new Vector2(130, 130);
                    ImGui.Image(rankIcon.ImGuiHandle, size);
                }
                // Stolen from https://github.com/marconsou/mp-tick-bar
                if (Service.TextureProvider
                           .GetFromManifestResource(Assembly.GetExecutingAssembly(),
                                                    "DragoonMayCry.Assets.GaugeDefault.png")
                           .TryGetWrap(out var gauge, out var _))
                {
                    var textureToElementScale = 0.39f;
                    var gaugeWidth = gauge.Width * textureToElementScale;
                    var gaugeHeight = (gauge.Height / 6.0f) * textureToElementScale;
                    var offsetX = 10f;
                    var offsetY = 150f;
                    var progress = scoreProgressBar.Progress;
                    RenderBackgroundUIElement(gauge, offsetX, offsetY, gaugeWidth, gaugeHeight, textureToElementScale, true);
                    RenderBarUIElement(gauge, offsetX, offsetY, gaugeWidth,
                                       gaugeHeight, textureToElementScale, progress);
                    RenderBackgroundUIElement(gauge, offsetX, offsetY, gaugeWidth, gaugeHeight, textureToElementScale, false);
                }
            }

            
        }

        
        private void RenderBackgroundUIElement(IDalamudTextureWrap texture, float offsetX, float offsetY, float gaugeWidth, float gaugeHeight, float textureToElementScale, bool isBackground)
        {
            var x = offsetX;
            var y = offsetY;
            var width = gaugeWidth;
            var height = gaugeHeight;
            var textureElementHeight = gaugeHeight / textureToElementScale;
            var textureX = 0.0f;
            var textureY = (textureElementHeight * (!isBackground ? 0 : 5)) / texture.Height;
            var textureW = 1.0f;
            var textureH = textureY + (textureElementHeight / texture.Height);
            ImGui.SetCursorPos(new(x, y));
            ImGui.Image(texture.ImGuiHandle, new(width, height), new(textureX, textureY), new(textureW, textureH), Vector4.One);
        }
        private void RenderBarUIElement(IDalamudTextureWrap texture, float offsetX, float offsetY, float gaugeWidth, float gaugeHeight, float textureToElementScale, double progress)
        {
            var barTextureOffsetX = 12.0f * textureToElementScale;
            var x = offsetX + barTextureOffsetX;
            var y = offsetY;
            var width = (float)((gaugeWidth - (barTextureOffsetX * 2.0f)) * progress);
            var height = gaugeHeight;
            var textureElementX = barTextureOffsetX / textureToElementScale;
            var textureElementHeight = gaugeHeight / textureToElementScale;
            var textureX = textureElementX / texture.Width;
            var textureY = (textureElementHeight * 4) / texture.Height;
            var textureW = textureX + (float)((1.0f - (textureX * 2.0f)) * progress);
            var textureH = textureY + (textureElementHeight / texture.Height);
            var color = Vector4.One;
            ImGui.SetCursorPos(new(x, y));
            ImGui.Image(texture.ImGuiHandle, new(width, height), new(textureX, textureY), new(textureW, textureH), color);
        }

    }
}
