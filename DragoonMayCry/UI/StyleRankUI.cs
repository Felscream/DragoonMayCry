using Dalamud.Interface.Animation;
using Dalamud.Interface.Animation.EasingFunctions;
using Dalamud.Interface.Textures.TextureWraps;
using DragoonMayCry.Score;
using DragoonMayCry.Score.Action;
using DragoonMayCry.Score.Model;
using DragoonMayCry.Score.Rank;
using DragoonMayCry.State;
using DragoonMayCry.UI.Model;
using DragoonMayCry.Util;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using Vector2 = System.Numerics.Vector2;
using Vector4 = FFXIVClientStructs.FFXIV.Common.Math.Vector4;

namespace DragoonMayCry.UI
{
    public sealed class StyleRankUI : IDisposable
    {
        public static readonly Dictionary<StyleType, StyleUi> StyleUis =
            new()
            {
                { StyleType.D, new("DragoonMayCry.Assets.D.png", new Vector3(223, 152, 30), 121514) },
                { StyleType.C, new("DragoonMayCry.Assets.C.png", new Vector3(95, 160, 213), 121583) },
                { StyleType.B, new("DragoonMayCry.Assets.B.png", new Vector3(95, 160, 213), 121533) },
                { StyleType.A, new("DragoonMayCry.Assets.A.png", new Vector3(95, 160, 213), 121532) },
                { StyleType.S, new("DragoonMayCry.Assets.S.png", new Vector3(233, 216, 95), 121581) },
                { StyleType.SS, new("DragoonMayCry.Assets.SS.png", new Vector3(233, 216, 95), 121531) },
                { StyleType.SSS, new("DragoonMayCry.Assets.SSS.png", new Vector3(233, 216, 95), 121511) },
            };

        private readonly ScoreProgressBar scoreProgressBar;
        private readonly ScoreManager scoreManager;
        private readonly Random random;
        private readonly StyleRankHandler styleRankHandler;
        private readonly PlayerState playerState;
        private readonly FinalRankCalculator finalRankCalculator;
        private readonly PlayerActionTracker playerActionTracker;
        private readonly HitCounter hitCounter;
        private readonly Fonts fonts;

        private readonly Vector2 rankPosition = new(8, 8);
        private readonly Vector2 rankSize = new(130, 130);
        private readonly Vector2 goldSaucerEditionSize = new(130, 80);
        private readonly Vector2 rankTransitionStartPosition = new(83, 83);
        private readonly Vector2 hitCounterPosition = new(16, 125);
        private readonly Vector2 hitCounterSize = new(120, 32);

        private StyleType currentStyle = StyleType.NoStyle;
        private StyleType previousStyle = StyleType.NoStyle;
        private bool isInCombat;
        private float demotionDuration = 1f;

        private readonly Easing rankTransition;
        private readonly Easing finalRankTransition;
        private readonly Stopwatch shakeStopwatch;
        private readonly Stopwatch demotionStopwatch;

        private const float ShakeDuration = 300;
        private const float RankShakeIntensity = 6f;
        private const float HitCounterShakeIntensity = 1.5f;
        private const string GaugeDefault = "DragoonMayCry.Assets.GaugeDefault.png";

        private const ImGuiWindowFlags UnlockedWindowFlags = ImGuiWindowFlags.NoCollapse |
                                                             ImGuiWindowFlags.NoDocking |
                                                             ImGuiWindowFlags.NoDecoration |
                                                             ImGuiWindowFlags.NoResize |
                                                             ImGuiWindowFlags.NoScrollbar |
                                                             ImGuiWindowFlags.NoScrollWithMouse |
                                                             ImGuiWindowFlags.NoTitleBar;
                                                             
        
        private const ImGuiWindowFlags LockedWindowFlags = UnlockedWindowFlags |
                                                           ImGuiWindowFlags.NoMove |
                                                           ImGuiWindowFlags.NoBackground |
                                                           ImGuiWindowFlags.NoMouseInputs |
                                                           ImGuiWindowFlags.NoFocusOnAppearing |
                                                           ImGuiWindowFlags.NoBringToFrontOnFocus |
                                                           ImGuiWindowFlags.NoNavInputs |
                                                           ImGuiWindowFlags.NoNavFocus |
                                                           ImGuiWindowFlags.NoNav |
                                                           ImGuiWindowFlags.NoInputs;
        public StyleRankUI(
            ScoreProgressBar scoreProgressBar, StyleRankHandler styleRankHandler, ScoreManager scoreManager,
            FinalRankCalculator finalRankCalculator, PlayerActionTracker playerActionTracker,
            HitCounter hitCounter)
        {
            this.scoreProgressBar = scoreProgressBar;
            this.styleRankHandler = styleRankHandler;
            this.scoreManager = scoreManager;
            this.finalRankCalculator = finalRankCalculator;
            this.scoreManager.Scoring += OnScoring!;
            this.finalRankCalculator.FinalRankCalculated += OnFinalRankCalculated!;
            this.playerState = PlayerState.GetInstance();
            this.playerState.RegisterCombatStateChangeHandler(OnCombatChange!);
            this.playerActionTracker = playerActionTracker;
            this.playerActionTracker.ActionFlyTextCreated += OnActionFlyTextCreated!;
            this.hitCounter = hitCounter;
            fonts = new Fonts();

            this.styleRankHandler.StyleRankChange += OnRankChange!;

            rankTransition = new OutCubic(new TimeSpan(1500000));
            finalRankTransition = new EaseOutBack(new TimeSpan(3000000));
            shakeStopwatch = new Stopwatch();
            demotionStopwatch = new Stopwatch();

            this.scoreProgressBar.DemotionCanceled += OnDemotionCanceled;
            this.scoreProgressBar.DemotionStart += OnDemotionStarted;

            random = new Random();
        }

        public void Draw()
        {
            var windowFlags = Plugin.Configuration!.LockScoreWindow? LockedWindowFlags : UnlockedWindowFlags;
            UpdateRankTransitionAnimation();
            UpdateFinalRankTransitionAnimation();

            
            if (!Plugin.Configuration.LockScoreWindow)
            {
                if (Plugin.Configuration.SplitLayout)
                {
                    DrawSplitMock(windowFlags);
                }
                else
                {
                    DrawCompactMock(windowFlags);
                }
                return;
            }
            
            if (Plugin.Configuration.SplitLayout)
            {
                DrawSplit(windowFlags);
            }
            else
            {
                DrawCompact(windowFlags);
            }
            
            if (shakeStopwatch.IsRunning && shakeStopwatch.ElapsedMilliseconds > ShakeDuration)
            {
                shakeStopwatch.Reset();
            }

            if (demotionStopwatch.IsRunning && demotionStopwatch.ElapsedMilliseconds > demotionDuration)
            {
                demotionStopwatch.Reset();
            }
        }

        private void DrawCompact(ImGuiWindowFlags windowFlags)
        {
            if (!CanRetrieveStyleDisplay(currentStyle))
            {
                return;
            }
            
            if (ImGui.Begin("##DmC", windowFlags))
            {
                var style = StyleUis[currentStyle];
                if (TryGetRankTexture(style, out var rankIcon))
                {
                    DrawCurrentRank(rankIcon, Plugin.Configuration!.RankDisplayScale.Value);
                }

                // Stolen from https://github.com/marconsou/mp-tick-bar
                if (Plugin.Configuration!.EnableProgressGauge && isInCombat && Service.TextureProvider
                        .GetFromManifestResource(Assembly.GetExecutingAssembly(),
                                                 GaugeDefault)
                        .TryGetWrap(out var gauge, out var _))
                {
                    DrawProgressGauge(gauge, scoreProgressBar.Progress, style.GaugeColor, Plugin.Configuration!.RankDisplayScale.Value);
                }

                DrawHitCounter(Plugin.Configuration!.RankDisplayScale.Value);
            }
        }

        private void DrawSplit(ImGuiWindowFlags windowFlags)
        {
            if (!CanRetrieveStyleDisplay(currentStyle))
            {
                return;
            }
            
            var style = StyleUis[currentStyle];
            ImGui.SetNextWindowSize(new Vector2(0, 0), ImGuiCond.Always);
            if (ImGui.Begin("##StyleIcon", windowFlags))
            {
                if (TryGetRankTexture(style, out var rankIcon))
                {
                    DrawCurrentRank(rankIcon, Plugin.Configuration!.SplitLayoutRankDisplayScale.Value);
                    DrawHitCounter(Plugin.Configuration!.SplitLayoutRankDisplayScale.Value);
                }
            }

            ImGui.SetNextWindowSize(new Vector2(0, 0), ImGuiCond.Always);
            if (ImGui.Begin("##StyleProgress", windowFlags))
            {
                if (Plugin.Configuration!.EnableProgressGauge && isInCombat && Service.TextureProvider
                        .GetFromManifestResource(Assembly.GetExecutingAssembly(),
                                                 GaugeDefault)
                        .TryGetWrap(out var gauge, out var _))
                {
                    DrawProgressGauge(gauge, scoreProgressBar.Progress, style.GaugeColor, Plugin.Configuration.SplitLayoutProgressGaugeScale.Value);
                }
            }
        }

        private static bool TryGetRankTexture(StyleUi style, out IDalamudTextureWrap texture)
        {
            texture = null!;
            if (Plugin.Configuration!.GoldSaucerEdition
                && Service.TextureProvider.TryGetFromGameIcon(style.GoldSaucerEditionIconId, out var tex)
                && tex.TryGetWrap(out var gsIcon, out var _))
            {
                texture = gsIcon;
                return true;
            }
            else if (Service.TextureProvider.GetFromManifestResource(Assembly.GetExecutingAssembly(), style.IconPath)
                            .TryGetWrap(out var rankIcon, out var _))
            {
                texture = rankIcon;
                return true;
            }

            return false;
        }

        private void UpdateFinalRankTransitionAnimation()
        {
            if (finalRankTransition.IsDone)
            {
                finalRankTransition.Stop();
            }

            if (finalRankTransition.IsRunning)
            {
                finalRankTransition.Update();
            }
        }

        private void UpdateRankTransitionAnimation()
        {
            if (rankTransition.IsDone)
            {
                rankTransition.Stop();
            }

            if (rankTransition.IsRunning)
            {
                rankTransition.Update();
            }
        }

        private void DrawCompactMock(ImGuiWindowFlags windowFlags)
        {
            ImGui.SetNextWindowSize(new Vector2(0, 0), ImGuiCond.Always);
            if (ImGui.Begin("##DmC", windowFlags))
            {
                var iconPath = CanRetrieveStyleDisplay(currentStyle)
                                   ? StyleUis[currentStyle].IconPath!
                                   : StyleUis[StyleType.S].IconPath;
                var progress = scoreProgressBar.Progress;
                if (Service.TextureProvider.GetFromManifestResource(Assembly.GetExecutingAssembly(), iconPath)
                           .TryGetWrap(out var rankIcon, out var _))
                {
                    ImGui.Image(rankIcon.ImGuiHandle, rankSize * Plugin.Configuration!.RankDisplayScale.Value / 100f);
                }

                if (Plugin.Configuration!.EnableProgressGauge && Service.TextureProvider
                                                                        .GetFromManifestResource(
                                                                            Assembly.GetExecutingAssembly(),
                                                                            GaugeDefault)
                                                                        .TryGetWrap(out var gauge, out var _))
                {
                    DrawProgressGauge(gauge, progress, new Vector3(255, 255, 255), Plugin.Configuration!.RankDisplayScale.Value);
                }
            }
        }
        
        private void DrawSplitMock(ImGuiWindowFlags windowFlags)
        {
            
            var iconPath = CanRetrieveStyleDisplay(currentStyle)
                               ? StyleUis[currentStyle].IconPath!
                               : StyleUis[StyleType.S].IconPath;
            var progress = scoreProgressBar.Progress;
            
            ImGui.SetNextWindowSize(new Vector2(0, 0), ImGuiCond.Always);
            if (ImGui.Begin("##StyleIcon", windowFlags))
            {
                if (Service.TextureProvider.GetFromManifestResource(Assembly.GetExecutingAssembly(), iconPath)
                           .TryGetWrap(out var rankIcon, out var _))
                {
                    ImGui.Image(rankIcon.ImGuiHandle, rankSize * Plugin.Configuration!.SplitLayoutRankDisplayScale.Value / 100f);
                }
            }

            ImGui.SetNextWindowSize(new Vector2(0, 0), ImGuiCond.Always);
            if (Plugin.Configuration!.EnableProgressGauge && ImGui.Begin("##StyleProgress", windowFlags))
            {
                if (Service.TextureProvider.GetFromManifestResource(Assembly.GetExecutingAssembly(),
                                                                    GaugeDefault)
                                                                    .TryGetWrap(out var gauge, out var _))
                {
                    DrawProgressGauge(gauge, progress, new Vector3(255, 255, 255), Plugin.Configuration!.SplitLayoutProgressGaugeScale.Value);
                }
            }
        }

        private void Shake()
        {
            shakeStopwatch.Restart();
        }

        private void DrawProgressGauge(IDalamudTextureWrap gauge, float progress, Vector3 color, int scale)
        {
            const float textureToElementScale = 0.39f;
            var gaugeWidth = gauge.Width * textureToElementScale;
            var gaugeHeight = (gauge.Height / 6.0f) * textureToElementScale;
            const float offsetX = 10f;
            var offsetY = Plugin.Configuration!.SplitLayout ? 10f : 150f;
            RenderBackgroundUiElement(gauge, offsetX, offsetY, gaugeWidth, gaugeHeight, textureToElementScale, true, scale);
            RenderBarUiElement(gauge, offsetX, offsetY, gaugeWidth,
                               gaugeHeight, textureToElementScale, progress, color, scale);
            RenderBackgroundUiElement(gauge, offsetX, offsetY, gaugeWidth, gaugeHeight, textureToElementScale, false, scale);
        }

        private void DrawCurrentRank(IDalamudTextureWrap rankIcon, int scale)
        {
            var textureSize = Plugin.Configuration!.GoldSaucerEdition ? goldSaucerEditionSize : rankSize;
            var normalizedScale = scale / 100f;
            var currentAnimation = isInCombat ? rankTransition : finalRankTransition;
            var pos = ComputeRankPosition(currentAnimation);
            var animationTransitionValue = GetAnimationTransitionValue(currentAnimation);
            var size = textureSize * (float)animationTransitionValue;


            var textureUv0 = Vector2.Zero;
            var textureUv1 = Vector2.One;
            var alpha = 1f;
            if (demotionStopwatch.IsRunning)
            {
                alpha = GetDemotionAlpha(
                    demotionStopwatch.ElapsedMilliseconds / 1000f,
                    demotionDuration / 1000f);
            }

            var color = new System.Numerics.Vector4(1, 1, 1, alpha);

            ImGui.SetCursorPos(pos);
            ImGui.Image(rankIcon.ImGuiHandle, size * normalizedScale, textureUv0, textureUv1, color);
        }

        private float GetAnimationTransitionValue(Easing currentAnimation)
        {
            if (currentAnimation.IsRunning)
            {
                return (float)currentAnimation.Value;
            }

            return 1f;
        }

        private Vector2 ComputeRankPosition(Easing currentAnimation)
        {
            var pos = rankPosition;
            if (isInCombat)
            {
                var transitionPosition = GetPositionByAnimation(currentAnimation);

                var intensity =
                    MathFunctionsUtils.InCube(scoreProgressBar.Progress) * 1.5f;
                if (shakeStopwatch.IsRunning)
                {
                    intensity = RankShakeIntensity;
                }

                var offset = new Vector2(
                    random.NextSingle() * intensity * 2 - intensity / 2,
                    random.NextSingle() * intensity * 2 - intensity / 2);

                pos = transitionPosition + offset;
            }
            else if (currentAnimation.IsRunning)
            {
                pos = GetPositionByAnimation(currentAnimation);
            }

            if (Plugin.Configuration!.GoldSaucerEdition)
            {
                pos.Y += 40;
            }

            return pos * Plugin.Configuration!.RankDisplayScale.Value / 100f;
        }

        private Vector2 GetPositionByAnimation(Easing currentAnimation)
        {
            var lerpedCoordinatesX = (float)double.Lerp(
                rankTransitionStartPosition.X, rankPosition.X,
                currentAnimation.Value);
            var lerpedCoordinatesY = (float)double.Lerp(
                rankTransitionStartPosition.Y, rankPosition.Y,
                currentAnimation.Value);

            var transitionPosition =
                new Vector2(lerpedCoordinatesX, lerpedCoordinatesY);
            return transitionPosition;
        }

        private static void RenderBackgroundUiElement(
            IDalamudTextureWrap texture, float offsetX, float offsetY, float gaugeWidth, float gaugeHeight,
            float textureToElementScale, bool isBackground, int scale)
        {
            var normalizedScale = scale / 100f;
            var x = offsetX * normalizedScale;
            var y = offsetY * normalizedScale;
            var width = gaugeWidth * normalizedScale;
            var height = gaugeHeight * normalizedScale;
            var textureElementHeight =
                gaugeHeight / textureToElementScale;
            var textureX = 0.0f;
            var textureY = (textureElementHeight * (!isBackground ? 0 : 5)) / texture.Height;
            var textureW = 1.0f;
            var textureH = textureY + (textureElementHeight / texture.Height);
            ImGui.SetCursorPos(new(x, y));
            ImGui.Image(texture.ImGuiHandle, new(width, height), new(textureX, textureY), new(textureW, textureH),
                        Vector4.One);
        }

        private void RenderBarUiElement(
            IDalamudTextureWrap texture, float offsetX, float offsetY, float gaugeWidth, float gaugeHeight,
            float textureToElementScale, double progress, Vector3 rankColor, int scale)
        {
            var normalizedScale = scale / 100f;
            var barTextureOffsetX = 12.0f * textureToElementScale;
            var x = (offsetX + barTextureOffsetX) * normalizedScale;
            var y = offsetY * normalizedScale;
            var width = (float)((gaugeWidth - (barTextureOffsetX * 2.0f)) * progress) * normalizedScale;
            var height = gaugeHeight * normalizedScale;
            var textureElementX = barTextureOffsetX / textureToElementScale;
            var textureElementHeight = gaugeHeight / textureToElementScale;
            var textureX = textureElementX / texture.Width;
            var textureY = (textureElementHeight * 2) / texture.Height;
            var textureW = textureX + (float)((1.0f - (textureX * 2.0f)) * progress);
            var textureH = textureY + (textureElementHeight / texture.Height);
            var color = ComputeProgressBarColor(rankColor);
            ImGui.SetCursorPos(new(x, y));
            ImGui.Image(texture.ImGuiHandle, new(width, height), new(textureX, textureY), new(textureW, textureH),
                        color);
        }

        private Vector4 ComputeProgressBarColor(Vector3 rankColor)
        {
            var normalizedColor = rankColor / 255;
            var color = normalizedColor;
            if (!CanRetrieveStyleDisplay(previousStyle))
            {
                return new Vector4(color, 1);
            }

            var style = StyleUis[previousStyle];

            if (rankTransition.IsRunning)
            {
                var normalizedStartingColor =
                    style.GaugeColor / 255;
                color = Vector3.Lerp(normalizedStartingColor, normalizedColor,
                                     (float)rankTransition.Value);
            }

            return new Vector4(color, 1);
        }

        private void DrawHitCounter(int scale)
        {
            if (!Plugin.Configuration!.EnableHitCounter || hitCounter.HitCount < 2)
            {
                return;
            }
            
            var position = GetHitCounterPosition();
            var scaling = scale / 100f;
            var scaledSize = hitCounterSize * scaling;
            ImGui.SetCursorPos(position * scaling);
            if (ImGui.BeginChild("Hit Counter", scaledSize, false, ImGuiWindowFlags.NoScrollbar))
            {
                var font = fonts.GetHitCountFontByScale(scale);
                font.Push();
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.92f, 0.92f, 0.92f, 1));
                var text = $"x {hitCounter.HitCount}";
                var textSize = ImGui.CalcTextSize(text);
                ImGui.SetCursorPosX(scaledSize.X - textSize.X - 8f * scaling);
                ImGui.TextUnformatted(text);
                ImGui.PopStyleColor();
                font.Pop();
                ImGui.EndChild();
            }
        }

        private Vector2 GetHitCounterPosition()
        {
            if (shakeStopwatch.IsRunning)
            {
                var offset = new Vector2(
                    random.NextSingle() * HitCounterShakeIntensity * 2 - HitCounterShakeIntensity / 2,
                    random.NextSingle() * HitCounterShakeIntensity * 2 - HitCounterShakeIntensity / 2);

                return hitCounterPosition + offset;
            }

            return hitCounterPosition;
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
            if (enteringCombat)
            {
                finalRankTransition.Reset();
            }
        }

        private void OnScoring(object sender, double points)
        {
            if (Plugin.IsMultiHitLoaded())
            {
                return;
            }

            Shake();
        }

        private void OnActionFlyTextCreated(object sender, EventArgs e)
        {
            Shake();
        }

        private static float GetDemotionAlpha(float t, float duration)
        {
            return Math.Clamp(1 - (1 / duration) * t, 0, 1);
        }

        private static bool CanRetrieveStyleDisplay(StyleType type)
        {
            return StyleUis.ContainsKey(type);
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

        private void OnFinalRankCalculated(object? sender, StyleType finalRank)
        {
            currentStyle = finalRank;
            finalRankTransition.Start();
        }

        public void Dispose()
        {
            fonts.Dispose();
        }
    }
}
