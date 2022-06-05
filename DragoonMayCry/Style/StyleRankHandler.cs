using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Dalamud.Logging;
using ImGuiScene;
using ImGuiNET;
using static DragoonMayCry.Style.Structures;

using Character = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;

namespace DragoonMayCry.Style {

    internal unsafe class StyleRankHandler {
        public delegate void OnStyleChangeEventHandler(StyleRank currentStyle, StyleRank previousStyle);
        public event OnStyleChangeEventHandler OnStyleChange;
        public delegate void OnProgressChangeEventHandler(StyleRank currentStyle, float progress);
        public event OnProgressChangeEventHandler OnProgressChange;

        private StyleRank currentStyle;
        private Queue<uint> actionHistory;
        private int actionHistorySize = 3;

        private readonly StyleRank noStyle;
        private float nextStyleThreshold = 100f;
        private float currentProgress = 0f;
        private double toleratedInactivityDuration = 10d;
        private double lastUpdateTime = 0d;

        public StyleRankHandler(TextureWrap d, TextureWrap c, TextureWrap b, TextureWrap a, TextureWrap s, TextureWrap ss, TextureWrap sss) {
            noStyle = new StyleRank(null, StyleType.NO_STYLE, null, 16f, double.MaxValue, 1f);
            StyleRank dStyle = new(d, StyleType.D, noStyle, 12f, double.MaxValue, 1f);
            StyleRank cStyle = new(c, StyleType.C, dStyle, 10f, double.MaxValue, 0.8f);
            StyleRank bStyle = new(b, StyleType.B, cStyle, 10f, 5d, 0.7f);
            StyleRank aStyle = new(a, StyleType.A, bStyle, 8f, 4d, 0.7f);
            StyleRank sStyle = new(s, StyleType.S, aStyle, 7f, 3d, 0.6f);
            StyleRank ssStyle = new(ss, StyleType.SS, sStyle, 7f, 2d, 0.5f);
            StyleRank sssStyle = new(sss, StyleType.SSS, ssStyle, 5f, 2d, 0.3f);

            noStyle.NextStyle = dStyle;
            dStyle.NextStyle = cStyle;
            cStyle.NextStyle = bStyle;
            bStyle.NextStyle = aStyle;
            aStyle.NextStyle = sStyle;
            sStyle.NextStyle = ssStyle;
            ssStyle.NextStyle = sssStyle;

            currentStyle = noStyle;

            actionHistory = new Queue<uint>();
        }

        public void Update(uint sourceId, uint currentCharacterId, Character* sourceCharacter, IntPtr pos, EffectHeader* effectHeader, EffectEntry* effectArray, ulong* effectTail, bool inCombat) {
            if (sourceId != currentCharacterId || effectHeader->ActionId < 8 || effectArray[0].type == Structures.ActionType.Nothing || !inCombat) {
                return;
            }
            try {
                PluginLog.Debug($"EFFECT : --- source actor: {sourceCharacter->GameObject.ObjectID}, action id {effectHeader->ActionId}, anim id {effectHeader->AnimationId} numTargets: {effectHeader->TargetCount} ---");

                var newEffect = new Structures.ActionEffectInfo {
                    actionId = effectHeader->ActionId,
                    type = effectArray[0].type,
                    sourceId = sourceId,
                    targetCount = effectHeader->TargetCount
                };
                float points = currentStyle.PointsPerAction;
                if (actionHistory.Contains(newEffect.actionId) && actionHistory.Count >= actionHistorySize) {
                    points *= currentStyle.SpamMalusMultiplier;
                }
                actionHistory.Enqueue(newEffect.actionId);
                if (actionHistory.Count >= actionHistorySize) {
                    actionHistory.Dequeue();
                }

                currentProgress += points;
                if (currentProgress > nextStyleThreshold && currentStyle.NextStyle != null) {
                    ToRank(currentStyle.NextStyle);
                }
                PluginLog.Debug($"Updating current progress -> {currentProgress}");
                OnProgressChange(currentStyle, currentProgress);
            } catch (Exception e) {
                PluginLog.Error(e, "An error has occurred in Dragoon May Cry.");
            }
        }

        private void ToRank(StyleRank nextRank) {
            currentProgress = currentProgress % nextStyleThreshold;
            var previousStyle = currentStyle;
            currentStyle = nextRank;
            PluginLog.Debug($"Going to style rank {currentStyle.StyleType} from {previousStyle.StyleType} with a progression of {currentProgress}");
            OnStyleChange(currentStyle, previousStyle);
        }

        public void Reset() {
            currentStyle = noStyle;
            currentProgress = 0f;
            OnProgressChange(currentStyle, currentProgress);
        }

        public void Died() {
            currentProgress = 0f;
            ToRank(noStyle.NextStyle!);
            OnProgressChange(currentStyle, currentProgress);
        }
    }
}
