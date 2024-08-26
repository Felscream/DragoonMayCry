using Dalamud.Interface.Animation;
using Dalamud.Interface.Animation.EasingFunctions;
using Dalamud.Interface.Textures.TextureWraps;
using DragoonMayCry.Score;
using DragoonMayCry.Score.Style;
using DragoonMayCry.State;
using DragoonMayCry.Util;
using ImGuiNET;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using Vector2 = System.Numerics.Vector2;
using Vector4 = FFXIVClientStructs.FFXIV.Common.Math.Vector4;

namespace DragoonMayCry.UI
{
    public sealed class StyleRankUI
    {
        private readonly ScoreProgressBar scoreProgressBar;
        private readonly ScoreManager scoreManager;
        private readonly Random random;
        private readonly StyleRankHandler styleRankHandler;
        private readonly PlayerState playerState;
        private readonly Vector2 rankPosition = new (8, 8);
        private readonly Vector2 rankSize = new(130, 130);
        private readonly Vector2 rankTransitionStartPosition = new(83, 83);
        private StyleRank? currentStyleRank;
        private StyleRank? previousStyle;
        private bool showProgressGauge;

        private readonly Easing rankTransition;
        private readonly Stopwatch shakeStopwatch;
        private readonly float shakeDuration = 400f;
        private readonly float shakeIntensity = 6f;

        private static readonly string DefaultRankIconPath =
            "DragoonMayCry.Assets.S.png";

        public StyleRankUI(ScoreProgressBar scoreProgressBar, StyleRankHandler styleRankHandler, ScoreManager scoreManager)
        {
            this.scoreProgressBar = scoreProgressBar;
            this.styleRankHandler = styleRankHandler;
            this.scoreManager = scoreManager;
            this.scoreManager.OnScoring += OnScoring!;
            this.playerState = PlayerState.GetInstance();
            this.playerState.RegisterCombatStateChangeHandler(OnCombatChange!);
            
            this.styleRankHandler.StyleRankChange += OnRankChange!;

            long duration = 1500000;
            rankTransition = new OutCubic(new(duration));
            shakeStopwatch = new Stopwatch();

            random = new();
        }

        public void Draw() {
            if (shakeStopwatch.IsRunning && shakeStopwatch.ElapsedMilliseconds > shakeDuration)
            {
                shakeStopwatch.Reset();
            }

            var windowFlags = Plugin.Configuration!.StyleRankUiConfiguration.LockScoreWindow ?
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
                                  ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize;

            if (rankTransition.IsRunning)
            {
                rankTransition.Update();
                if (rankTransition.IsDone)
                {
                    rankTransition.Stop();
                }
            }

            ImGui.SetNextWindowSize(Vector2.Zero, ImGuiCond.Always);
            if (!Plugin.Configuration.StyleRankUiConfiguration.LockScoreWindow)
            {
                DrawMock(windowFlags);
                return;
            }
            

            if (ImGui.Begin("DmC", windowFlags))
            {
                if (currentStyleRank == null || currentStyleRank.IconPath == null)
                {
                    return;
                }
                if (Service.TextureProvider.GetFromManifestResource(Assembly.GetExecutingAssembly(), currentStyleRank.IconPath).TryGetWrap(out var rankIcon, out var _))
                {
                    DrawCurrentRank(rankIcon);
                }

                // Stolen from https://github.com/marconsou/mp-tick-bar
                if ((showProgressGauge || Plugin.Configuration.StyleRankUiConfiguration.TestRankDisplay) && Service.TextureProvider
                                            .GetFromManifestResource(Assembly.GetExecutingAssembly(),
                                                "DragoonMayCry.Assets.GaugeDefault.png")
                                            .TryGetWrap(out var gauge, out var _))
                {
                    DrawProgressGauge(gauge, scoreProgressBar.Progress, currentStyleRank.ProgressBarColor);
                }

                if (Plugin.Configuration.StyleRankUiConfiguration
                          .TestRankDisplay)
                {
                    DrawScore();
                }
            }
        }

        private void DrawMock(ImGuiWindowFlags windowFlags)
        {
            if (ImGui.Begin("DmC", windowFlags))
            {
                var iconPath = playerState.IsInCombat ? currentStyleRank!.IconPath! : DefaultRankIconPath;
                var progress = scoreProgressBar.Progress;
                if (Service.TextureProvider.GetFromManifestResource(Assembly.GetExecutingAssembly(), iconPath).TryGetWrap(out var rankIcon, out var _))
                {
                    ImGui.Image(rankIcon.ImGuiHandle, rankSize);

                        
                }
                if(Service.TextureProvider
                          .GetFromManifestResource(Assembly.GetExecutingAssembly(),
                                                   "DragoonMayCry.Assets.GaugeDefault.png")
                          .TryGetWrap(out var gauge, out var _))
                {
                    DrawProgressGauge(gauge, progress, new(255,255,255));
                }
            }
        }

        public void Shake()
        {
            shakeStopwatch.Restart();
        }

        private void DrawScore()
        {
            var score = scoreManager.CurrentScoreRank.Score;
            ImGui.Text($"{score}");
        }

        private void DrawProgressGauge(IDalamudTextureWrap gauge, float progress, Vector3 color)
        {
            var textureToElementScale = 0.39f;
            var gaugeWidth = gauge.Width * textureToElementScale;
            var gaugeHeight = (gauge.Height / 6.0f) * textureToElementScale;
            var offsetX = 10f;
            var offsetY = 150f;
            RenderBackgroundUIElement(gauge, offsetX, offsetY, gaugeWidth, gaugeHeight, textureToElementScale, true);
            RenderBarUIElement(gauge, offsetX, offsetY, gaugeWidth,
                               gaugeHeight, textureToElementScale, progress, color);
            RenderBackgroundUIElement(gauge, offsetX, offsetY, gaugeWidth, gaugeHeight, textureToElementScale, false);
        }

        private void DrawCurrentRank(IDalamudTextureWrap rankIcon)
        {
            var pos = ComputeRankPosition();
            var animationTransitionValue = rankTransition.IsRunning ? (float)rankTransition.Value : 1f;
            var size = rankSize * animationTransitionValue;

            
            var textureUV0 = Vector2.Zero;
            var textureUV1 = Vector2.One;
            var color = scoreProgressBar.DemotionAlertStarted
                            ? new Vector4(1, 1, 1,
                                          AlphaModificationFunction(
                                              (float)ImGui.GetTime()))
                            : Vector4.One;

            ImGui.SetCursorPos(pos);
            ImGui.Image(rankIcon.ImGuiHandle, size, textureUV0, textureUV1, color);
        }

        private Vector2 ComputeRankPosition()
        {
            var pos = rankPosition;
            if (playerState.IsInCombat)
            {
                var lerpedCoordinates = (float)double.Lerp(
                    rankTransitionStartPosition.X, rankPosition.X,
                    rankTransition.Value);

                var transitionPosition =
                    new Vector2(lerpedCoordinates, lerpedCoordinates);

                var intensity =
                    CustomEasing.InCube(scoreProgressBar.Progress) * 1.5f;
                if (shakeStopwatch.IsRunning)
                {
                    intensity = shakeIntensity;
                }
                var offset = new Vector2(
                    random.NextSingle() * intensity * 2 - intensity / 2,
                    random.NextSingle() * intensity * 2 - intensity / 2);

                pos = transitionPosition + offset;
            }

            return pos;
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
        private void RenderBarUIElement(IDalamudTextureWrap texture, float offsetX, float offsetY, float gaugeWidth, float gaugeHeight, float textureToElementScale, double progress, Vector3 rankColor)
        {
            var barTextureOffsetX = 12.0f * textureToElementScale;
            var x = offsetX + barTextureOffsetX;
            var y = offsetY;
            var width = (float)((gaugeWidth - (barTextureOffsetX * 2.0f)) * progress);
            var height = gaugeHeight;
            var textureElementX = barTextureOffsetX / textureToElementScale;
            var textureElementHeight = gaugeHeight / textureToElementScale;
            var textureX = textureElementX / texture.Width;
            var textureY = (textureElementHeight * 2) / texture.Height;
            var textureW = textureX + (float)((1.0f - (textureX * 2.0f)) * progress);
            var textureH = textureY + (textureElementHeight / texture.Height);
            var color = ComputeProgressBarColor(rankColor);
            ImGui.SetCursorPos(new(x, y));
            ImGui.Image(texture.ImGuiHandle, new(width, height), new(textureX, textureY), new(textureW, textureH), color);
        }

        private Vector4 ComputeProgressBarColor(Vector3 rankColor)
        {
            var normalizedColor = rankColor / 255;
            var color = normalizedColor;
            if (rankTransition.IsRunning && previousStyle != null)
            {
                var normalizedStartingColor =
                    previousStyle.ProgressBarColor / 255;
                color = Vector3.Lerp(normalizedStartingColor, normalizedColor,
                                     (float)rankTransition.Value);
            }
            return new(color, 1);
        }

        private void OnRankChange(object send, StyleRankHandler.RankChangeData data)
        {
            if (currentStyleRank == null)
            {
                currentStyleRank = data.NewRank;
            }

            if (currentStyleRank.StyleType < data.NewRank.StyleType)
            {
                rankTransition.Restart();
            }
            else
            {
                rankTransition.Reset();
            }

            previousStyle = currentStyleRank;
            currentStyleRank = data.NewRank;
        }

        private void OnCombatChange(object send, bool enteringCombat)
        {
            showProgressGauge = enteringCombat;
        }

        private void OnScoring(object sender, double points)
        {
            Shake();
        }

        private float AlphaModificationFunction(float t)
        {
            return (float)((Math.Sin(3*Math.PI * t + Math.PI / 2) + 1) / 4) + 0.5f;
        }
    }
}
