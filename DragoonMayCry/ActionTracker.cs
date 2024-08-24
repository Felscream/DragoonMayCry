using Dalamud.Hooking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Objects.SubKinds;
using DragoonMayCry.State;
using static FFXIVClientStructs.FFXIV.Client.System.String.Utf8String.Delegates;

namespace DragoonMayCry
{
    public enum ActionType
    {
        Action = 0,
        CastStart = 1,
        CastCancel = 2,
        OffGCD = 3,
        AutoAttack = 4
    }

    public class ActionTracker
    {
        private delegate void OnActionUsedDelegate(uint sourceId, IntPtr sourceCharacter, IntPtr pos, IntPtr effectHeader, IntPtr effectArray, IntPtr effectTrail);
        private Hook<OnActionUsedDelegate>? _onActionUsedHook;

        private delegate void OnActorControlDelegate(uint entityId, uint id, uint unk1, uint type, uint unk2, uint unk3, uint unk4, uint unk5, UInt64 targetId, byte unk6);
        private Hook<OnActorControlDelegate>? _onActorControlHook;

        private delegate void OnCastDelegate(uint sourceId, IntPtr sourceCharacter);
        private Hook<OnCastDelegate>? _onCastHook;

        private readonly PlayerState playerState;
        public ActionTracker(PlayerState playerState)
        {
            this.playerState = playerState;

            try
            {
                _onActionUsedHook = Service.Hook.HookFromSignature<OnActionUsedDelegate>(
                    "40 ?? 56 57 41 ?? 41 ?? 41 ?? 48 ?? ?? ?? ?? ?? ?? ?? 48",
                    OnActionUsed
                );
                _onActionUsedHook?.Enable();

                _onActorControlHook = Service.Hook.HookFromSignature<OnActorControlDelegate>(
                    "E8 ?? ?? ?? ?? 0F B7 0B 83 E9 64",
                    OnActorControl
                );
                _onActorControlHook?.Enable();

                _onCastHook = Service.Hook.HookFromSignature<OnCastDelegate>(
                    "40 56 41 56 48 81 EC ?? ?? ?? ?? 48 8B F2",
                    OnCast
                );
                _onCastHook?.Enable();
            }
            catch (Exception e)
            {
                Service.Log.Error("Error initiating hooks: " + e.Message);
            }

            Service.Framework.Update += Update;
        }

        private void OnActionUsed(uint sourceId, IntPtr sourceCharacter, IntPtr pos, IntPtr effectHeader,
                                  IntPtr effectArray, IntPtr effectTrail)
        {
            _onActionUsedHook?.Original(sourceId, sourceCharacter, pos, effectHeader, effectArray, effectTrail);

            IPlayerCharacter? player = playerState.Player;
            if (player == null || sourceId != player.GameObjectId) { return; }

            int actionId = Marshal.ReadInt32(effectHeader, 0x8);
            TimelineItemType? type = TypeForActionID((uint)actionId);
            if (!type.HasValue) { return; }

            Plugin.Logger.Debug($"Action {actionId} {type.ToString()}");

            AddItem((uint)actionId, type.Value);
        }

        private ActionType? TypeForActionID(uint actionId)
        {
            LuminaAction? action = _sheet?.GetRow(actionId);
            if (action == null) { return null; }

            // off gcd or sprint
            if (action.ActionCategory.Row is 4 || actionId == 3)
            {
                return ActionType.OffGCD;
            }

            if (action.ActionCategory.Row is 1)
            {
                return ActionType.AutoAttack;
            }

            return ActionType.Action;
        }

        private void OnActorControl(uint entityId, uint type, uint buffID, uint direct, uint actionId, uint sourceId, uint arg4, uint arg5, ulong targetId, byte a10)
        {
            _onActorControlHook?.Original(entityId, type, buffID, direct, actionId, sourceId, arg4, arg5, targetId, a10);

            if (type != 15) { return; }

            IPlayerCharacter? player = Plugin.ClientState.LocalPlayer;
            if (player == null || entityId != player.GameObjectId) { return; }

            AddItem(actionId, TimelineItemType.CastCancel);
        }

        private void OnCast(uint sourceId, IntPtr ptr)
        {
            _onCastHook?.Original(sourceId, ptr);

            IPlayerCharacter? player = Plugin.ClientState.LocalPlayer;
            if (player == null || sourceId != player.GameObjectId) { return; }

            int value = Marshal.ReadInt16(ptr);
            uint actionId = value < 0 ? (uint)(value + 65536) : (uint)value;

            AddItem(actionId, TimelineItemType.CastStart);
        }
    }
}
