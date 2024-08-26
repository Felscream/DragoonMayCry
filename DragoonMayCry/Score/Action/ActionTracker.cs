using Dalamud.Game.Gui.FlyText;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using DragoonMayCry.State;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using ImGuiNET;
using Lumina.Excel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ActionManager = FFXIVClientStructs.FFXIV.Client.Game.ActionManager;
using LuminaAction = Lumina.Excel.GeneratedSheets.Action;

namespace DragoonMayCry.Score.Action
{


    public unsafe class ActionTracker : IDisposable
    {
        private HashSet<FlyTextKind> validTextKind = new HashSet<FlyTextKind>() {
            FlyTextKind.Damage,
            FlyTextKind.DamageCrit,
            FlyTextKind.DamageDh,
            FlyTextKind.DamageCritDh
        };

        private delegate void AddFlyTextDelegate(
            IntPtr addonFlyText,
            uint actorIndex,
            uint messageMax,
            IntPtr numbers,
            uint offsetNum,
            uint offsetNumMax,
            IntPtr strings,
            uint offsetStr,
            uint offsetStrMax,
            int unknown);
        private readonly Hook<AddFlyTextDelegate>? addFlyTextHook;

        public EventHandler? OnGcdDropped;
        public EventHandler<float>? OnFlyTextCreation;
        public EventHandler<float>? OnGcdClip;

        private delegate void OnActionUsedDelegate(
            uint sourceId, nint sourceCharacter, nint pos,
            nint effectHeader, nint effectArray, nint effectTrail);

        private Hook<OnActionUsedDelegate>? onActionUsedHook;

        private delegate void OnActorControlDelegate(
            uint entityId, uint id, uint unk1, uint type, uint unk2, uint unk3,
            uint unk4, uint unk5, ulong targetId, byte unk6);

        private Hook<OnActorControlDelegate>? onActorControlHook;

        private delegate void OnCastDelegate(
            uint sourceId, nint sourceCharacter);

        private Hook<OnCastDelegate>? onCastHook;

        private readonly PlayerState playerState;
        private ExcelSheet<LuminaAction>? sheet;

        private CombatStopwatch combatStopwatch;
        private Stopwatch gracePeriodStopwatch;

        private ushort lastDetectedClip = 0;
        private float currentWastedGcd = 0;
        private float encounterTotalClip = 0;
        private float encounterTotalWaste = 0;

        private bool isInactive;
        private PlayerAction? currentAction;
        private PlayerAction? previousAction;
        public ActionTracker()
        {
            playerState = PlayerState.GetInstance();
            combatStopwatch = CombatStopwatch.GetInstance();

            sheet = Service.DataManager.GetExcelSheet<LuminaAction>();
            try
            {
                onActionUsedHook = Service.Hook.HookFromSignature<OnActionUsedDelegate>("40 ?? 56 57 41 ?? 41 ?? 41 ?? 48 ?? ?? ?? ?? ?? ?? ?? 48", OnActionUsed);
                

                onActorControlHook = Service.Hook.HookFromSignature<OnActorControlDelegate>("E8 ?? ?? ?? ?? 0F B7 0B 83 E9 64", OnActorControl);
                

                onCastHook = Service.Hook.HookFromSignature<OnCastDelegate>("40 56 41 56 48 81 EC ?? ?? ?? ?? 48 8B F2", OnCast);
                

                var addFlyTextAddress = Service.Scanner.ScanText("E8 ?? ?? ?? ?? FF C7 41 D1 C7");
                addFlyTextHook = Service.Hook.HookFromAddress<AddFlyTextDelegate>(addFlyTextAddress, AddFlyTextDetour);
            }
            catch (Exception e)
            {
                Service.Log.Error("Error initiating hooks: " + e.Message);
            }

            onActionUsedHook?.Enable();
            onActorControlHook?.Enable();
            onCastHook?.Enable();
            addFlyTextHook?.Enable();

            Service.Framework.Update += Update;
            playerState.RegisterCombatStateChangeHandler(OnCombat);
        }

        public void Dispose()
        {
            Service.Framework.Update -= Update;
            addFlyTextHook?.Disable();
            addFlyTextHook?.Dispose();
                
            onActionUsedHook?.Disable();
            onActionUsedHook?.Dispose();

            onActorControlHook?.Disable();
            onActorControlHook?.Dispose();

            onCastHook?.Disable();
            onCastHook?.Dispose();
        }

        private void OnActionUsed(
            uint sourceId, nint sourceCharacter, nint pos,
            nint effectHeader,
            nint effectArray, nint effectTrail)
        {
            onActionUsedHook?.Original(sourceId, sourceCharacter, pos,
                                        effectHeader, effectArray, effectTrail);

            var player = playerState.Player;
            if (player == null || sourceId != player.GameObjectId)
            {
                return;
            }

            var actionId = Marshal.ReadInt32(effectHeader, 0x8);
            
            var type = TypeForActionID((uint)actionId);
            if (type == PlayerActionType.Other)
            {
                return;
            }
            Service.Log.Debug($"{type}");
            if (type == PlayerActionType.LimitBreak)
            {
                Service.Log.Debug("Used Limitbreak;");
            }
            
            RegisterNewAction((uint)actionId);
        }

        private PlayerActionType TypeForActionID(uint actionId)
        {
            var action = sheet?.GetRow(actionId);
            if (action == null)
            {
                return PlayerActionType.Other;
            }

            Service.Log.Debug($"{action.Name} {action.RowId}");

            return action.ActionCategory.Row switch
            {
                2 => PlayerActionType.Spell,
                4 => PlayerActionType.OffGCD,
                6 => PlayerActionType.Other,
                7 => PlayerActionType.Other,
                9 => PlayerActionType.LimitBreak,
                15 => PlayerActionType.LimitBreak,
                _ => PlayerActionType.Weaponskill,
            };
        }

        private void OnActorControl(
            uint entityId, uint type, uint buffID, uint direct, uint actionId,
            uint sourceId, uint arg4, uint arg5, ulong targetId, byte a10)
        {
            onActorControlHook?.Original(entityId, type, buffID, direct,
                                          actionId, sourceId, arg4, arg5,
                                          targetId, a10);

            if (type != 15)
            {
                return;
            }

            var player = playerState.Player;
            if (player == null || entityId != player.GameObjectId)
            {
                return;
            }
            Service.Log.Warning("cast cancel");
            // send a cast cancel event
        }

        private void OnCast(uint sourceId, nint ptr)
        {
            onCastHook?.Original(sourceId, ptr);

            var player = playerState.Player;
            if (player == null || sourceId != player.GameObjectId)
            {
                return;
            }

            int value = Marshal.ReadInt16(ptr);
            var actionId = value < 0 ? (uint)(value + 65536) : (uint)value;
            var type = TypeForActionID((uint)actionId);
            if (type == PlayerActionType.LimitBreak)
            {
                Service.Log.Debug("Cast Limitbreak;");
            }
            //RegisterNewAction(actionId);
        }

        private void RegisterNewAction(uint actionId)
        {
            if (sheet == null)
            {
                return;
            }
            var luminaAction = sheet.GetRow(actionId);
            if (luminaAction == null || !luminaAction.IsPlayerAction)
            {
                return;
            }

            PlayerActionType type = TypeForActionID(actionId);
            if (type != PlayerActionType.Weaponskill && type != PlayerActionType.Spell && type != PlayerActionType.LimitBreak)
            {
                return;
            }

            var duration = type == PlayerActionType.Weaponskill
                               ? GetGCDTime(actionId)
                               : GetCastTime(actionId);

            var playerAction = new PlayerAction(
                actionId, type, luminaAction.ActionCombo?.Value?.RowId,
                luminaAction.PreservesCombo, combatStopwatch.TimeInCombat(), duration);
            Service.Log.Warning($"Registering new action");
            Service.Log.Warning($"{luminaAction.Name} type {type} has combo {luminaAction.ActionCombo?.Value != null && luminaAction.ActionCombo?.Value.RowId != 0}");
            Service.Log.Warning($"start {combatStopwatch.TimeInCombat()} duration { duration}");
            
            
        }

        private unsafe float GetGCDTime(uint actionId)
        {
            var actionManager = ActionManager.Instance();
            var adjustedId = actionManager->GetAdjustedActionId(actionId);
            return actionManager->GetRecastTime(ActionType.Action, adjustedId);
        }
        private unsafe float GetCastTime(uint actionId)
        {
            var actionManager = ActionManager.Instance();
            var adjustedId = actionManager->GetAdjustedActionId(actionId);
            return ActionManager.GetAdjustedCastTime(ActionType.Action, adjustedId) / 1000f;
        }

        private unsafe void Update(IFramework framework)
        {
            if (!playerState.IsInCombat)
            {
                return;
            }

            DetectClipping();
            DetectWastedGCD();

        }

        private void OnCombat(object? sender, bool InCombat)
        {
            if (InCombat)
            {
                encounterTotalClip = 0;
                encounterTotalWaste = 0;
                currentWastedGcd = 0;
            }
        }

        private unsafe void DetectClipping()
        {
            var animationLock = Plugin.ActionManager->animationLock;
            if (lastDetectedClip == Plugin.ActionManager->currentSequence || Plugin.ActionManager->isGCDRecastActive || animationLock <= 0)
            {
                return;
            }

            if (animationLock != 0.1f)
            {
                encounterTotalClip += animationLock;
                Service.Log.Debug($"GCD Clip: {animationLock} ms");
                OnGcdClip?.Invoke(this, animationLock);
            }

            lastDetectedClip = Plugin.ActionManager->currentSequence;
        }

        private unsafe void DetectWastedGCD()
        {
            if (!Plugin.ActionManager->isGCDRecastActive && !Plugin.ActionManager->isQueued)
            {
                if (Plugin.ActionManager->animationLock > 0) return;
                currentWastedGcd += ImGui.GetIO().DeltaTime;
                if (!isInactive && currentWastedGcd > Plugin.Configuration!.GcdDropThreshold)
                {
                    isInactive = true;
                    Service.Log.Debug($"GCD dropped");
                    OnGcdDropped?.Invoke(this, EventArgs.Empty);
                }
            }
            else if (currentWastedGcd > 0)
            {
                encounterTotalWaste += currentWastedGcd;
                Service.Log.Debug($"Wasted GCD: {currentWastedGcd} ms");
                currentWastedGcd = 0;
                isInactive = false;
            }
        }

        private unsafe void AddFlyTextDetour(
            IntPtr addonFlyText,
            uint actorIndex,
            uint messageMax,
            IntPtr numbers,
            uint offsetNum,
            uint offsetNumMax,
            IntPtr strings,
            uint offsetStr,
            uint offsetStrMax,
            int unknown)
        {
                // don't lock this since locks may not be enough
            addFlyTextHook?.Original(
                addonFlyText,
                actorIndex,
                messageMax,
                numbers,
                offsetNum,
                offsetNumMax,
                strings,
                offsetStr,
                offsetStrMax,
                unknown);
            try
            {
                // Known valid flytext region within the atk arrays
                // actual index
                var strIndex = 27;
                var numIndex = 30;
                var atkArrayDataHolder = ((UIModule*)Service.GameGui.GetUIModule())->GetRaptureAtkModule()->AtkModule.AtkArrayDataHolder;

                try
                {
                    var strArray = atkArrayDataHolder._StringArrays[strIndex];
                    var flyText1Ptr = strArray->StringArray[offsetStr];
                    if (flyText1Ptr == null || (nint)flyText1Ptr == IntPtr.Zero)
                    {
                        return;
                    }
                    var numArray = atkArrayDataHolder._NumberArrays[numIndex];
                    var kind = numArray->IntArray[offsetNum + 1];
                    var val1 = numArray->IntArray[offsetNum + 2];
                    var val2 = numArray->IntArray[offsetNum + 3];
                    int damageTypeIcon = numArray->IntArray[offsetNum + 4];
                    int color = numArray->IntArray[offsetNum + 6];
                    int icon = numArray->IntArray[offsetNum + 7];
                    var text1 = Marshal.PtrToStringUTF8((nint)flyText1Ptr);
                    var flyText2Ptr = strArray->StringArray[offsetStr + 1];
                    var text2 = Marshal.PtrToStringUTF8((nint)flyText2Ptr);


                    if (text1 == null || text2 == null)
                    {
                        return;
                    }
                    if (text1.EndsWith("\\u00A7") && text1.Length >= 1)
                    {
                        return;
                    }


                    String? shownActionName = null;
                    if (text1 != string.Empty)
                    {
                        shownActionName = text1;
                    }
                    FlyTextKind flyKind = (FlyTextKind)kind;
                    if (shownActionName == null || val1 <= 0 || !validTextKind.Contains(flyKind))
                    {
                        Service.Log.Debug($"Ignoring action of kind {flyKind}");
                        return;
                    }
                    OnFlyTextCreation?.Invoke(this, val1);
                }
                catch (Exception e)
                {
                    Service.Log.Error(e, "Skipping");
                }
            }
            catch (Exception e)
            {
                Service.Log.Error(e, "An error has occurred in DragoonMayCry");
            }
        }
    }
}
