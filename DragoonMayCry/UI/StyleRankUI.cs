using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using Dalamud.Interface.Animation;
using Dalamud.Interface.Animation.EasingFunctions;
using Dalamud.Interface.Textures.TextureWraps;
using DragoonMayCry.Score;
using DragoonMayCry.Score.Model;
using DragoonMayCry.Score.Style;
using DragoonMayCry.State;
using DragoonMayCry.UI.Model;
using DragoonMayCry.Util;
using ImGuiNET;
using Vector2 = System.Numerics.Vector2;
using Vector4 = FFXIVClientStructs.FFXIV.Common.Math.Vector4;

namespace DragoonMayCry.UI
{
    public sealed class StyleRankUI
    {
        private readonly Dictionary<StyleType, StyleUi> styleUis =
            new Dictionary<StyleType, StyleUi>
            {
                { StyleType.D, new("DragoonMayCry.Assets.D.png", new(223, 152, 30)) },
                { StyleType.C , new ("DragoonMayCry.Assets.C.png", new Vector3(95, 160, 213)) },
                { StyleType.B , new ("DragoonMayCry.Assets.B.png", new Vector3(95, 160, 213)) },
                { StyleType.A , new ("DragoonMayCry.Assets.A.png", new Vector3(95, 160, 213)) },
                { StyleType.S , new ("DragoonMayCry.Assets.S.png", new Vector3(233, 216, 95)) },
                { StyleType.SS , new ("DragoonMayCry.Assets.SS.png", new Vector3(233, 216, 95)) },
                { StyleType.SSS , new ("DragoonMayCry.Assets.SSS.png", new Vector3(233, 216, 95)) },
            };
        private readonly ScoreProgressBar scoreProgressBar;
        private readonly ScoreManager scoreManager;
        private readonly Random random;
        private readonly StyleRankHandler styleRankHandler;
        private readonly PlayerState playerState;
        private readonly Vector2 rankPosition = new (8, 8);
        private readonly Vector2 rankSize = new(130, 130);
        private readonly Vector2 rankTransitionStartPosition = new(83, 83);
        private StyleType currentStyle = StyleType.NoStyle;
        private StyleType previousStyle = StyleType.NoStyle;
        private bool isInCombat;
        private float demotionDuration = 1f;

        private readonly Easing rankTransition;
        private readonly Stopwatch shakeStopwatch;
        private readonly Stopwatch demotionStopwatch;
        private readonly float shakeDuration = 300;
        private readonly float shakeIntensity = 6f;
        private readonly string gaugeDefault = "DragoonMayCry.Assets.GaugeDefault.png";

        public StyleRankUI(ScoreProgressBar scoreProgressBar, StyleRankHandler styleRankHandler, ScoreManager scoreManager)
        {
            this.scoreProgressBar = scoreProgressBar;
            this.styleRankHandler = styleRankHandler;
            this.scoreManager = scoreManager;
            this.scoreManager.OnScoring += OnScoring!;
            this.playerState = PlayerState.GetInstance();
            this.playerState.RegisterCombatStateChangeHandler(OnCombatChange!);
            
            this.styleRankHandler.StyleRankChange += OnRankChange!;

            rankTransition = new OutCubic(new(1500000));
            shakeStopwatch = new Stopwatch();
            demotionStopwatch = new Stopwatch();

            this.scoreProgressBar.DemotionCanceled += OnDemotionCanceled;
            this.scoreProgressBar.DemotionStart += OnDemotionStarted;

            random = new();
        }

        public void Draw() {
            

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
                if (!CanRetrieveStyleDisplay(currentStyle))
                {
                    return;
                }

                var style = styleUis[currentStyle];
                if (Service.TextureProvider.GetFromManifestResource(Assembly.GetExecutingAssembly(), style.IconPath).TryGetWrap(out var rankIcon, out var _))
                {
                    DrawCurrentRank(rankIcon);
                }

                // Stolen from https://github.com/marconsou/mp-tick-bar
                if ((isInCombat || Plugin.Configuration.StyleRankUiConfiguration.TestRankDisplay) && Service.TextureProvider
                                            .GetFromManifestResource(Assembly.GetExecutingAssembly(),
                                                gaugeDefault)
                                            .TryGetWrap(out var gauge, out var _))
                {
                    DrawProgressGauge(gauge, scoreProgressBar.Progress, style.GaugeColor);
                }

                if (Plugin.Configuration.StyleRankUiConfiguration
                          .TestRankDisplay)
                {
                    DrawScore();
                }
            }

            if (shakeStopwatch.IsRunning && shakeStopwatch.ElapsedMilliseconds > shakeDuration)
            {
                shakeStopwatch.Reset();
            }

            if (demotionStopwatch.IsRunning && demotionStopwatch.ElapsedMilliseconds > demotionDuration)
            {
                demotionStopwatch.Reset();
            }
        }

        private void DrawMock(ImGuiWindowFlags windowFlags)
        {
            if (ImGui.Begin("DmC", windowFlags))
            {
                var iconPath = CanRetrieveStyleDisplay(currentStyle) ? styleUis[currentStyle].IconPath! : styleUis[StyleType.S].IconPath;
                var progress = scoreProgressBar.Progress;
                if (Service.TextureProvider.GetFromManifestResource(Assembly.GetExecutingAssembly(), iconPath).TryGetWrap(out var rankIcon, out var _))
                {
                    ImGui.Image(rankIcon.ImGuiHandle, rankSize);
                }
                if(Service.TextureProvider
                          .GetFromManifestResource(Assembly.GetExecutingAssembly(),
                                                   gaugeDefault)
                          .TryGetWrap(out var gauge, out var _))
                {
                    DrawProgressGauge(gauge, progress, new(255,255,255));
                }
            }
        }

        private void Shake()
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

            
            var textureUv0 = Vector2.Zero;
            var textureUv1 = Vector2.One;
            var alpha = 1f;
            if (demotionStopwatch.IsRunning)
            {
                alpha = GetDemotionAlpha(
                    demotionStopwatch.ElapsedMilliseconds / 1000f,
                    demotionDuration / 1000f);
            }
            var color = new System.Numerics.Vector4(1,1,1,alpha);

            ImGui.SetCursorPos(pos);
            ImGui.Image(rankIcon.ImGuiHandle, size, textureUv0, textureUv1, color);
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
                    MathFunctionsUtils.InCube(scoreProgressBar.Progress) * 1.5f;
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
            if (!CanRetrieveStyleDisplay(previousStyle))
            { 
                return new(color, 1);
            }

            StyleUi style = styleUis[previousStyle];

            if (rankTransition.IsRunning)
            {
                var normalizedStartingColor =
                    style.GaugeColor / 255;
                color = Vector3.Lerp(normalizedStartingColor, normalizedColor,
                                     (float)rankTransition.Value);
            }
            return new(color, 1);
        }

        private void OnRankChange(object send, StyleRankHandler.RankChangeData data)
        {
            if (currentStyle < data.NewRank)
            {
                rankTransition.Restart();
            }
            else
            {
                rankTransition.Reset();
            }
            previousStyle = currentStyle;
            currentStyle = data.NewRank;
            demotionStopwatch.Reset();
        }

        private void OnCombatChange(object send, bool enteringCombat)
        {
            isInCombat = enteringCombat;
            rankTransition.Reset();
            shakeStopwatch.Reset();
            demotionStopwatch.Reset();
        }

        private void OnScoring(object sender, double points)
        {
            Shake();
        }

        private static float GetDemotionAlpha(float t, float duration)
        {
            return Math.Clamp(1 - (1 / duration) * t, 0, 1);
        }

        private bool CanRetrieveStyleDisplay(StyleType type)
        {
            return styleUis.ContainsKey(type);
        }

        private void OnDemotionStarted(object? sender, float duration)
        {
            demotionDuration = duration;
            demotionStopwatch.Restart();
        }

        private void OnDemotionCanceled(object? sender, EventArgs e)
        {
            demotionStopwatch.Reset();
        }
    }
}
