using System;
using Dalamud.Interface;
using Dalamud.Interface.Textures.TextureWraps;
using DragoonMayCry.Style;
using FFXIVClientStructs.FFXIV.Common.Math;
using ImGuiNET;
using System.Reflection;
using Dalamud.Interface.Animation;
using Dalamud.Interface.Animation.EasingFunctions;
using DragoonMayCry.Score;
using DragoonMayCry.State;
using DragoonMayCry.Util;
using static DragoonMayCry.Score.ScoreManager;
using Vector2 = System.Numerics.Vector2;

namespace DragoonMayCry.UI
{
    public sealed class StyleRankUI
    {
        private readonly ScoreProgressBar scoreProgressBar;
        private readonly Random random;
        private readonly StyleRankHandler styleRankHandler;
        private readonly Vector2 rankPosition = new (8, 8);
        private readonly Vector2 rankSize = new(130, 130);
        private readonly Vector2 rankTransitionStartPosition = new(83, 83);
        private StyleRank currentStyleRank;
        private bool showProgressGauge;

        private Easing rankTransition;
        public StyleRankUI(ScoreProgressBar scoreProgressBar, StyleRankHandler styleRankHandler, PlayerState playerState)
        {
            this.scoreProgressBar = scoreProgressBar;
            this.styleRankHandler = styleRankHandler;
            playerState.RegisterCombatStateChangeHandler(OnCombatChange);
            
            this.styleRankHandler.OnStyleRankChange += OnRankChange;

            long duration = 2500000;
            rankTransition = new OutCubic(new(duration));

            random = new();
        }

        public void Draw() {
            var windowFlags = Plugin.Configuration.StyleRankUiConfiguration.LockScoreWindow ?
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
                                  ImGuiWindowFlags.NoDocking
                                  :
                                  ImGuiWindowFlags.NoTitleBar;

            if (rankTransition.IsRunning)
            {
                rankTransition.Update();
                if (rankTransition.IsDone)
                {
                    rankTransition.Stop();
                }
            }


            if (currentStyleRank != null && ImGui.Begin("DragoonMayCry score", windowFlags))
            {
                if (Service.TextureProvider.GetFromManifestResource(Assembly.GetExecutingAssembly(), currentStyleRank.IconPath).TryGetWrap(out var rankIcon, out var _))
                {
                    DrawCurrentRank(rankIcon);
                }

                // Stolen from https://github.com/marconsou/mp-tick-bar
                if (Service.TextureProvider
                           .GetFromManifestResource(Assembly.GetExecutingAssembly(),
                                                    "DragoonMayCry.Assets.GaugeDefault.png")
                           .TryGetWrap(out var gauge, out var _))
                {
                    DrawProgressGauge(gauge);
                }
                DrawScore();
            }
        }

        private void DrawScore()
        {
            var score = Plugin.ScoreManager.CurrentScoreRank.Score;
            ImGui.Text($"{score}");
        }

        private void DrawProgressGauge(IDalamudTextureWrap gauge)
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

        private void DrawCurrentRank(IDalamudTextureWrap rankIcon)
        {
            var animationTransitionValue = rankTransition.IsRunning ? (float)rankTransition.Value : 1f;
            var size = rankSize * animationTransitionValue;

            var lerpedCoordinates = (float)double.Lerp(
                rankTransitionStartPosition.X, rankPosition.X,
                rankTransition.Value);

            var transitionPosition =
                new Vector2(lerpedCoordinates, lerpedCoordinates);

            var intensity =
                CustomEasing.InCube((float)scoreProgressBar.Progress) * 0.6f;
            var offset = new System.Numerics.Vector2(
                random.NextSingle() * intensity * 2 - intensity / 2,
                random.NextSingle() * intensity * 2 - intensity / 2);
                    
            var pos = transitionPosition + offset;
            ImGui.SetCursorPos(pos);
                    
            ImGui.Image(rankIcon.ImGuiHandle, size);
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

        private void OnRankChange(object send, StyleRank rank)
        {
            if (currentStyleRank == null)
            {
                currentStyleRank = rank;
            }

            if (currentStyleRank.StyleType < rank.StyleType)
            {
                rankTransition.Reset();
                rankTransition.Start();
            }
            else
            {
                rankTransition.Stop();
                rankTransition.Reset();
            }
            currentStyleRank = rank;
            
        }

        private void OnCombatChange(object send, bool inCombat)
        {
            showProgressGauge = inCombat;
        }
    }
}
