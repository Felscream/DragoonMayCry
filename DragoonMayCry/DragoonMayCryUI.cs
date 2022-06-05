using ImGuiNET;
using Dalamud.Interface;
using ImGuiScene;
using System;
using System.Numerics;

namespace DragoonMayCry
{
    // It is good to have this be disposable in general, in case you ever need it
    // to do any cleanup
    class DragoonMayCryUI : IDisposable
    {
        public Style.StyleRank CurrentRank { get; set; }
        public float Progress { get; set; } = 0;
        private Configuration configuration;

        private bool visible = false;
        private TextureWrap gauge;
        
        public bool Visible
        {
            get { return this.visible; }
            set { this.visible = value; }
        }

        private bool settingsVisible = false;
        public bool SettingsVisible
        {
            get { return this.settingsVisible; }
            set { this.settingsVisible = value; }
        }

        public DragoonMayCryUI(Configuration configuration, TextureWrap gauge)
        {
            this.configuration = configuration;
            this.gauge = gauge;
        }

        public void Dispose()
        {
            this.gauge.Dispose();
        }

        public void Draw()
        {
            var windowFlags =
               ImGuiWindowFlags.NoTitleBar |
               ImGuiWindowFlags.NoResize |
               ImGuiWindowFlags.NoMove |
               ImGuiWindowFlags.NoScrollbar |
               ImGuiWindowFlags.NoScrollWithMouse |
               ImGuiWindowFlags.NoCollapse |
               ImGuiWindowFlags.NoDecoration |
               ImGuiWindowFlags.NoBackground |
               ImGuiWindowFlags.NoMouseInputs |
               ImGuiWindowFlags.NoFocusOnAppearing |
               ImGuiWindowFlags.NoBringToFrontOnFocus |
               ImGuiWindowFlags.NoNavInputs |
               ImGuiWindowFlags.NoNavFocus |
               ImGuiWindowFlags.NoNav |
               ImGuiWindowFlags.NoInputs |
               ImGuiWindowFlags.NoDocking;

            ImGui.SetNextWindowPos(new(700.0f, 450.0f), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSize(new(250.0f, 70.0f), ImGuiCond.FirstUseEver);
            if (ImGui.Begin("DmC", windowFlags)) {
                if (ImGui.BeginChild("MP Tick Bar (Child)", Vector2.Zero, true, windowFlags))
                    this.DrawRank();
                ImGui.EndChild();
            }
            ImGui.End();
        }

        private void DrawRank() {
            var textureToElementScale = 0.5f;
            var gaugeWidth = gauge.Width * textureToElementScale;
            var gaugeHeight = gauge.Height * textureToElementScale;
            var offsetX = 20f;
            var offsetY = 20f;
            RenderBackgroundUIElement(gauge, offsetX, offsetY, gaugeWidth, gaugeHeight, textureToElementScale, true);
            RenderBarUIElement(gauge, offsetX, offsetY, gaugeWidth, gaugeHeight, textureToElementScale, Progress, true);
            RenderBackgroundUIElement(gauge, offsetX, offsetY, gaugeWidth, gaugeHeight, textureToElementScale, false);
        }

        private void RenderBackgroundUIElement(TextureWrap gauge, float offsetX, float offsetY, float gaugeWidth, float gaugeHeight, float textureToElementScale, bool isBackground) {
            var x = offsetX;
            var y = offsetY;
            var width = gaugeWidth;
            var height = gaugeHeight;
            var textureElementHeight = gaugeHeight / textureToElementScale;
            var textureX = 0.0f;
            var textureY = (textureElementHeight * (!isBackground ? 0 : 5)) / gauge.Height;
            var textureW = 1.0f;
            var textureH = textureY + (textureElementHeight / gauge.Height);
            ImGui.SetCursorPos(new(x, y));
            ImGui.Image(gauge.ImGuiHandle, new(width, height), new(textureX, textureY), new(textureW, textureH), Vector4.One);
        }

        private void RenderBarUIElement(TextureWrap gauge, float offsetX, float offsetY, float gaugeWidth, float gaugeHeight, float textureToElementScale, float progress, bool isProgress) {
            var barTextureOffsetX = 12.0f * textureToElementScale;
            var x = offsetX + barTextureOffsetX;
            var y = offsetY;
            var width = (float)((gaugeWidth - (barTextureOffsetX * 2.0f)) * progress);
            var height = gaugeHeight;
            var textureElementX = barTextureOffsetX / textureToElementScale;
            var textureElementHeight = gaugeHeight / textureToElementScale;
            var textureX = textureElementX / gauge.Width;
            var textureY = (textureElementHeight * (isProgress ? 2 : 4)) / gauge.Height;
            var textureW = textureX + (float)((1.0f - (textureX * 2.0f)) * (isProgress ? progress : 1.0f));
            var textureH = textureY + (textureElementHeight / gauge.Height);
            ImGui.SetCursorPos(new(x, y));
            ImGui.Image(gauge.ImGuiHandle, new(width, height), new(textureX, textureY), new(textureW, textureH), new(0.0f, 1.0f, 1.0f, 1.0f));
        }

    }
}
