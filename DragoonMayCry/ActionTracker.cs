using Dalamud.Hooking;
using System;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Objects.SubKinds;
using DragoonMayCry.State;
using Lumina.Excel;
using LuminaAction = Lumina.Excel.GeneratedSheets.Action;
using ImGuiNET;
using System.Collections.Generic;
using Dalamud.Utility;
using System.Linq;
using static Lumina.Data.Parsing.Layer.LayerCommon;
using FFXIVClientStructs.FFXIV.Client.Game;
using Dalamud.Plugin.Services;

// Thanks to Tischel https://github.com/Tischel/ActionTimeline
namespace DragoonMayCry
{
    public enum PlayerActionType
    {
        Action = 0,
        CastStart = 1,
        CastCancel = 2,
        OffGCD = 3,
        AutoAttack = 4
    }

    public class ActionItem
    {
        public uint ActionID { get; }
        public uint IconID { get; }
        public PlayerActionType Type { get; }
        public double Time { get; }

        public float GCDDuration { get; }
        public float CastTime { get; }

        public GCDClipData? GCDClipData = null;

        public ActionItem(
            uint actionID, uint iconID, PlayerActionType type, double time)
        {
            ActionID = actionID;
            IconID = iconID;
            Type = type;
            Time = time;
            GCDDuration = 0;
            CastTime = 0;
        }

        public ActionItem(
            uint actionID, uint iconID, PlayerActionType type, double time,
            float gcdDuration, float castTime) : this(
            actionID, iconID, type, time)
        {
            GCDDuration = gcdDuration;
            CastTime = castTime;
        }
    }

    public struct GCDClipData
    {
        public bool IsClipped { get; }
        public double StartTime { get; }
        public double? EndTime { get; }
        public bool IsFakeEndTime { get; }

        public bool CanBeConsideredClipped()
        {
            double endTime = EndTime.HasValue ? EndTime.Value : ImGui.GetTime();
            return IsClipped && Math.Abs(endTime - StartTime) > 0.1f;
        }

        public GCDClipData(
            bool isClipped, double startTime, double? endTime,
            bool isFakeEndTime)
        {
            IsClipped = isClipped;
            StartTime = startTime;
            EndTime = endTime;
            IsFakeEndTime = isFakeEndTime;
        }
    }

    public class ActionTracker : IDisposable
    {
        private Dictionary<uint, uint> specialCasesMap = new()
        {
            // MNK
            [16475] = 53, // anatman

            // SAM
            [16484] = 7477, // kaeshi higanbana
            [16485] = 7477, // kaeshi goken
            [16486] = 7477, // keashi setsugekka
            [25782] = 7477, // kaeshi namikiri

            // RDM
            [25858] = 7504 // resolution
        };

        private Dictionary<uint, float> hardcodedCasesMap = new()
        {
            // NIN
            [2259] = 0.5f,  // ten
            [2261] = 0.5f,  // chi
            [2263] = 0.5f,  // jin
            [18805] = 0.5f, // ten
            [18806] = 0.5f, // chi
            [18807] = 0.5f, // jin

            [2265] = 1.5f,  // fuma shuriken
            [18873] = 1.5f, // fuma shuriken
            [18874] = 1.5f, // fuma shuriken
            [18875] = 1.5f, // fuma shuriken
            [2266] = 1.5f,  // katon
            [18876] = 1.5f, // katon
            [2267] = 1.5f,  // raiton
            [18877] = 1.5f, // raiton
            [2268] = 1.5f,  // hyoton
            [18878] = 1.5f, // hyoton
            [2269] = 1.5f,  // huton
            [18879] = 1.5f, // huton
            [2270] = 1.5f,  // doton
            [10892] = 1.5f, // doton
            [18880] = 1.5f, // doton
            [2271] = 1.5f,  // suiton
            [18881] = 1.5f, // suiton
            [2272] = 1.5f,  // rabbit medium

            [16491] = 1.5f, // goka mekkyaku
            [16492] = 1.5f, // hyosho ranryu
        };

        private delegate void OnActionUsedDelegate(
            uint sourceId, IntPtr sourceCharacter, IntPtr pos,
            IntPtr effectHeader, IntPtr effectArray, IntPtr effectTrail);

        private Hook<OnActionUsedDelegate>? _onActionUsedHook;

        private delegate void OnActorControlDelegate(
            uint entityId, uint id, uint unk1, uint type, uint unk2, uint unk3,
            uint unk4, uint unk5, UInt64 targetId, byte unk6);

        private Hook<OnActorControlDelegate>? _onActorControlHook;

        private delegate void OnCastDelegate(
            uint sourceId, IntPtr sourceCharacter);

        private Hook<OnCastDelegate>? _onCastHook;

        private readonly PlayerState playerState;
        private ExcelSheet<LuminaAction>? sheet;

        private List<ActionItem> _items = new (30);
        private bool hadSwiftcast = false;

        public ActionTracker(PlayerState playerState)
        {
            this.playerState = playerState;
            sheet = Service.DataManager.GetExcelSheet<LuminaAction>();
            try
            {
                _onActionUsedHook = Service.Hook.HookFromSignature<OnActionUsedDelegate>("40 ?? 56 57 41 ?? 41 ?? 41 ?? 48 ?? ?? ?? ?? ?? ?? ?? 48", OnActionUsed);
                _onActionUsedHook?.Enable();

                _onActorControlHook = Service.Hook.HookFromSignature<OnActorControlDelegate>("E8 ?? ?? ?? ?? 0F B7 0B 83 E9 64", OnActorControl);
                _onActorControlHook?.Enable();

                _onCastHook = Service.Hook.HookFromSignature<OnCastDelegate>( "40 56 41 56 48 81 EC ?? ?? ?? ?? 48 8B F2", OnCast);
                _onCastHook?.Enable();
            }
            catch (Exception e)
            {
                Service.Log.Error("Error initiating hooks: " + e.Message);
            }

            Service.Framework.Update += Update;
        }

        public void Dispose()
        {
            Service.Framework.Update -= Update;

            _items.Clear();

            _onActionUsedHook?.Disable();
            _onActionUsedHook?.Dispose();

            _onActorControlHook?.Disable();
            _onActorControlHook?.Dispose();

            _onCastHook?.Disable();
            _onCastHook?.Dispose();
        }

        private void OnActionUsed(
            uint sourceId, IntPtr sourceCharacter, IntPtr pos,
            IntPtr effectHeader,
            IntPtr effectArray, IntPtr effectTrail)
        {
            _onActionUsedHook?.Original(sourceId, sourceCharacter, pos,
                                        effectHeader, effectArray, effectTrail);

            IPlayerCharacter? player = playerState.Player;
            if (player == null || sourceId != player.GameObjectId)
            {
                return;
            }

            int actionId = Marshal.ReadInt32(effectHeader, 0x8);
            var type = TypeForActionID((uint)actionId);
            if (!type.HasValue)
            {
                return;
            }

            AddItem((uint)actionId, type.Value);
        }

        private PlayerActionType? TypeForActionID(uint actionId)
        {
            LuminaAction? action = sheet?.GetRow(actionId);
            if (action == null)
            {
                return null;
            }

            // off gcd or sprint
            if (action.ActionCategory.Row is 4 || actionId == 3)
            {
                return PlayerActionType.OffGCD;
            }

            if (action.ActionCategory.Row is 1)
            {
                return PlayerActionType.AutoAttack;
            }

            return PlayerActionType.Action;
        }

        private void OnActorControl(
            uint entityId, uint type, uint buffID, uint direct, uint actionId,
            uint sourceId, uint arg4, uint arg5, ulong targetId, byte a10)
        {
            _onActorControlHook?.Original(entityId, type, buffID, direct,
                                          actionId, sourceId, arg4, arg5,
                                          targetId, a10);

            if (type != 15)
            {
                return;
            }

            IPlayerCharacter? player = playerState.Player;
            if (player == null || entityId != player.GameObjectId)
            {
                return;
            }
            AddItem(actionId, PlayerActionType.CastCancel);
        }

        private void OnCast(uint sourceId, IntPtr ptr)
        {
            _onCastHook?.Original(sourceId, ptr);

            IPlayerCharacter? player = playerState.Player;
            if (player == null || sourceId != player.GameObjectId)
            {
                return;
            }

            int value = Marshal.ReadInt16(ptr);
            uint actionId = value < 0 ? (uint)(value + 65536) : (uint)value;
            AddItem(actionId, PlayerActionType.CastStart);
        }

        private void AddItem(uint actionId, PlayerActionType type)
        {
            LuminaAction? action = sheet?.GetRow(actionId);
            if (action == null)
            {
                return;
            }

            // only cache the last kMaxItemCount items
            if (_items.Count >= 30)
            {
                _items.RemoveAt(0);
            }

            double now = ImGui.GetTime();
            float gcdDuration = 0;
            float castTime = 0;

            // handle sprint and auto attack icons
            int iconId = actionId == 3
                             ? 104
                             : (actionId == 1 ? 101 : action.Icon);

            // handle weird cases
            uint id = actionId;
            if (specialCasesMap.TryGetValue(actionId, out uint replacedId))
            {
                type = PlayerActionType.Action;
                id = replacedId;
            }

            // calculate gcd and cast time
            if (type == PlayerActionType.CastStart)
            {
                gcdDuration = GetGCDTime(id);
                castTime = GetCastTime(id);
            }
            else if (type == PlayerActionType.Action)
            {
                ActionItem? lastItem = _items.LastOrDefault();
                if (lastItem != null && lastItem.Type == PlayerActionType.CastStart)
                {
                    gcdDuration = lastItem.GCDDuration;
                    castTime = lastItem.CastTime;
                }
                else
                {
                    gcdDuration = GetGCDTime(id);
                    castTime = hadSwiftcast ? 0 : GetCastTime(id);
                }
            }

            // handle more weird cases
            if (hardcodedCasesMap.TryGetValue(actionId, out float gcd))
            {
                type = PlayerActionType.Action;
                gcdDuration = gcd;
            }

            ActionItem item = new ActionItem(
                actionId, (uint)iconId, type, now, gcdDuration, castTime);
            _items.Add(item);
        }

        private void CheckSwiftcast()
        {
            IPlayerCharacter? player = playerState.Player;
            if (player != null)
            {
                hadSwiftcast = player.StatusList.Any(s => s.StatusId == 167);
            }
        }

        private (double?, bool) FindGCDClipEndTime(ActionItem item, int index)
        {
            if (index >= _items.Count - 1)
            {
                return (null, false);
            }

            ActionItem? prevItem = null;

            for (int i = index + 1; i < _items.Count; i++)
            {
                ActionItem nextItem = _items[i];
                if (nextItem.Type == PlayerActionType.Action)
                {
                    double time =
                        prevItem != null && prevItem.Type ==
                        PlayerActionType.CastStart
                            ? prevItem.Time
                            : nextItem.Time;
                    return (time, false);
                }
                
                if (nextItem.Type == PlayerActionType.CastStart &&
                         i == _items.Count - 1)
                {
                    return (nextItem.Time, true);
                }

                prevItem = nextItem;
            }

            return (null, false);
        }

        private unsafe float GetGCDTime(uint actionId)
        {
            ActionManager* actionManager = ActionManager.Instance();
            uint adjustedId = actionManager->GetAdjustedActionId(actionId);
            return actionManager->GetRecastTime(ActionType.Action, adjustedId);
        }
        private unsafe float GetCastTime(uint actionId)
        {
            ActionManager* actionManager = ActionManager.Instance();
            uint adjustedId = actionManager->GetAdjustedActionId(actionId);
            return (float)ActionManager.GetAdjustedCastTime(ActionType.Action, adjustedId) / 1000f;
        }

        private unsafe void Update(IFramework framework)
        {
            if (!playerState.IsInCombat)
            {
                return;
            }
            double now = ImGui.GetTime();

            CheckSwiftcast();

            // gcd clipping logic
            for (int i = 0; i < _items.Count; i++)
            {
                ActionItem item = _items[i];
                if (item.Type != PlayerActionType.Action) { continue; }
                if (item.GCDDuration == 0) { continue; } // does this ever happen???
                if (item.GCDClipData.HasValue && item.GCDClipData.Value.EndTime.HasValue && !item.GCDClipData.Value.IsFakeEndTime) { continue; }

                double gcdClipStart = item.Time + Math.Max(0, item.GCDDuration - item.CastTime);

                // cast threshold
                if (item.CastTime > 0)
                {
                    gcdClipStart += 0.6;
                }

                // check if clipped
                if (now >= gcdClipStart)
                {
                    var (gcdClipEnd, isFakeEnd) = FindGCDClipEndTime(item, i);

                    // make sure threshold doesn't break the math
                    if (gcdClipEnd.HasValue && gcdClipStart > gcdClipEnd.Value)
                    {
                        gcdClipStart = gcdClipEnd.Value;
                    }

                    // check max time
                    if (!gcdClipEnd.HasValue && now - gcdClipStart > 60)
                    {
                        gcdClipEnd = now;
                        isFakeEnd = false;
                    }

                    var lastItem = _items.LastOrDefault();

                    // not clipped?
                    if (!isFakeEnd && gcdClipEnd.HasValue && Math.Abs(gcdClipEnd.Value - gcdClipStart) < 0.1)
                    {
                        item.GCDClipData = new GCDClipData(false, 0, 0, false);
                    }
                    // clipped :(
                    else if(lastItem != null && lastItem.Type != PlayerActionType.CastStart)
                    {
                        item.GCDClipData = new GCDClipData(true, gcdClipStart, gcdClipEnd, isFakeEnd);
                    }
                }
            }
        }
    }
}
