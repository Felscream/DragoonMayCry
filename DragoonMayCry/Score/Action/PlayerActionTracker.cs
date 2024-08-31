using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Dalamud.Game.Gui.FlyText;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using DragoonMayCry.State;
using DragoonMayCry.Util;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using ImGuiNET;
using DragoonMayCry.Cache;
using Lumina.Excel;
using ActionManager = FFXIVClientStructs.FFXIV.Client.Game.ActionManager;
using LuminaAction = Lumina.Excel.GeneratedSheets.Action;

namespace DragoonMayCry.Score.Action
{


    public unsafe class PlayerActionTracker : IDisposable
    {
        public struct LimitBreakEvent
        {
            public bool IsTankLb;
            public bool IsCasting;

            public LimitBreakEvent(bool isTankLb, bool isCasting)
            {
                IsTankLb = isTankLb;
                IsCasting = isCasting;
            }
        }

        public class LimitBreak
        {
            public float GracePeriod { get; set; }
            public bool IsTankLb { get; set; }
            public String Name { get; set; }

            public LimitBreak(float gracePeriod, bool isTankLb, string name)
            {
                GracePeriod = gracePeriod;
                IsTankLb = isTankLb;
                Name = name;
            }
        }

        private HashSet<FlyTextKind> validTextKind = new HashSet<FlyTextKind>() {
            FlyTextKind.Damage,
            FlyTextKind.DamageCrit,
            FlyTextKind.DamageDh,
            FlyTextKind.DamageCritDh
        };

        public EventHandler? OnGcdDropped;
        public EventHandler? OnCastCanceled;
        public EventHandler<float>? OnFlyTextCreation;
        public EventHandler<float>? OnGcdClip;
        public EventHandler<LimitBreakEvent>? OnLimitBreak;
        public EventHandler? OnLimitBreakEffect;
        public EventHandler? OnLimitBreakCanceled;


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

        private readonly State.ActionManager* actionManager;
        private readonly PlayerState playerState;
        private LuminaCache<LuminaAction> luminaActionCache;

        private CombatStopwatch combatStopwatch;

        private const float GcdDropThreshold = 0.1f;
        private ushort lastDetectedClip = 0;
        private float currentWastedGcd = 0;

        private bool isGcdDropped;
        private PlayerAction? currentAction;
        private PlayerAction? previousAction;


        private Stopwatch limitBreakStopwatch;
        private LimitBreak? limitBreakCast;

        // added 0.1f to all duration
        private Dictionary<uint, float> tankLimitBreakDelays =
            new Dictionary<uint, float>
            {
                { 197, 2.1f },   // LB1
                { 198, 4.1f },   // LB2
                { 199, 4.1f },   // PLD Last Bastion
                { 4240, 4.1f },  // WAR Land Waker
                { 4241, 4.1f },  // DRK Dark Force
                { 17105, 4.1f }, // GNB Gunmetal Soul
            };
        public PlayerActionTracker()
        {
            luminaActionCache = LuminaCache<LuminaAction>.Instance;
            playerState = PlayerState.GetInstance();
            combatStopwatch = CombatStopwatch.GetInstance();
            limitBreakStopwatch = new Stopwatch();
            actionManager =
                (State.ActionManager*)FFXIVClientStructs.FFXIV.Client.Game.ActionManager.Instance();
            Service.FlyText.FlyTextCreated += OnFlyText;
            try
            {
                onActionUsedHook = Service.Hook.HookFromSignature<OnActionUsedDelegate>("40 ?? 56 57 41 ?? 41 ?? 41 ?? 48 ?? ?? ?? ?? ?? ?? ?? 48", OnActionUsed);
                

                onActorControlHook = Service.Hook.HookFromSignature<OnActorControlDelegate>("E8 ?? ?? ?? ?? 0F B7 0B 83 E9 64", OnActorControl);
                

                onCastHook = Service.Hook.HookFromSignature<OnCastDelegate>("40 56 41 56 48 81 EC ?? ?? ?? ?? 48 8B F2", OnCast);
            }
            catch (Exception e)
            {
                Service.Log.Error("Error initiating hooks: " + e.Message);
            }

            onActionUsedHook?.Enable();
            onActorControlHook?.Enable();
            onCastHook?.Enable();

            Service.Framework.Update += Update;
            playerState.RegisterCombatStateChangeHandler(OnCombat);
            playerState.RegisterDeathStateChangeHandler(OnDeath!);
        }

        public void Dispose()
        {
            Service.Framework.Update -= Update;
                
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

            if (!Plugin.CanRunDmc())
            {
                return;
            }

            var player = playerState.Player;
            if (sourceId != player!.GameObjectId)
            {
                return;
            }

            var actionId = Marshal.ReadInt32(effectHeader, 0x8);
            var type = TypeForActionId((uint)actionId);
            if (type == PlayerActionType.Other || actionId == 7) // 7 -> auto attack
            {
                return;
            }
            
            if (type == PlayerActionType.LimitBreak)
            {
                StartLimitBreakUse((uint)actionId);
            }

            /*var combo = actionManager->Combo; //value is 0 when a combo is dropped
            var action = luminaActionCache.GetRow(combo.Action);
            
            
            RegisterNewAction((uint)actionId);*/
        }

        private PlayerActionType TypeForActionId(uint actionId)
        {
            var action = luminaActionCache.GetRow(actionId);
            if (action == null)
            {
                return PlayerActionType.Other;
            }

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
            if (!Plugin.CanRunDmc())
            {
                return;
            }
            if (type != 15)
            {
                return;
            }

            var player = playerState.Player;
            if (player == null || entityId != player.GameObjectId)
            {
                return;
            }

            if (limitBreakCast != null)
            {
                CancelLimitBreak();
            }
            // send a cast cancel event
        }

        private void OnCast(uint sourceId, nint ptr)
        {
            onCastHook?.Original(sourceId, ptr);

            if (!Plugin.CanRunDmc())
            {
                return;
            }

            var player = playerState.Player;
            if (player == null || sourceId != player.GameObjectId)
            {
                return;
            }

            int value = Marshal.ReadInt16(ptr);
            var actionId = value < 0 ? (uint)(value + 65536) : (uint)value;
            var type = TypeForActionId((uint)actionId);

            if (type == PlayerActionType.LimitBreak)
            {
                StartLimitBreakUse(actionId);
            }
        }

        private void RegisterNewAction(uint actionId)
        {
            var luminaAction = luminaActionCache.GetRow(actionId);
            if (luminaAction == null || !luminaAction.IsPlayerAction)
            {
                return;
            }

            PlayerActionType type = TypeForActionId(actionId);
            if (type != PlayerActionType.Weaponskill && type != PlayerActionType.Spell && type != PlayerActionType.LimitBreak)
            {
                return;
            }

            var duration = type == PlayerActionType.Weaponskill
                               ? GetGcdTime(actionId)
                               : GetCastTime(actionId);

            var playerAction = new PlayerAction(
                actionId, type, luminaAction.ActionCombo?.Value?.RowId,
                luminaAction.PreservesCombo, combatStopwatch.TimeInCombat(), duration);
            Service.Log.Warning($"Registering new action");
            Service.Log.Warning($"{luminaAction.Name} type {type} has combo {luminaAction.ActionCombo?.Value != null && luminaAction.ActionCombo?.Value.RowId != 0}");
        }

        private unsafe float GetGcdTime(uint actionId)
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
            if (!Plugin.CanRunDmc())
            {
                return;
            }

            DetectClipping();
            HandleLimitBreakUse();
            DetectWastedGCD();

        }

        private void HandleLimitBreakUse()
        {
            if (!limitBreakStopwatch.IsRunning || limitBreakCast == null)
            {
                return;
            }

            if (limitBreakStopwatch.ElapsedMilliseconds / 1000f > limitBreakCast.GracePeriod)
            {
                ResetLimitBreakUse();
            }
        }

        private void OnCombat(object? sender, bool enteredCombat)
        {
            if (enteredCombat)
            {
                currentWastedGcd = 0;
            }
            else
            {
                if (limitBreakCast != null)
                {
                    ResetLimitBreakUse();
                }
            }
        }

        private void ResetLimitBreakUse()
        {
            limitBreakStopwatch.Reset();
            limitBreakCast = null;
            if (playerState.IsInCombat)
            {
                OnLimitBreak?.Invoke(this, new LimitBreakEvent(false, false));
            }
        }

        private void CancelLimitBreak()
        {
            limitBreakStopwatch.Reset();
            limitBreakCast = null;
            if (playerState.IsInCombat)
            {
                OnLimitBreakCanceled?.Invoke(this, EventArgs.Empty);
            }
        }

        private void StartLimitBreakUse(uint actionId)
        {
            if (!playerState.IsInCombat || limitBreakCast != null)
            {
                return;
            }

            var isTankLb = playerState.IsTank();
            var castTime = GetCastTime(actionId);

            // the +3 is just to give enough time to register the gcd clipping just after
            var gracePeriod = isTankLb ? tankLimitBreakDelays[actionId] : castTime + 3f;

            var action = luminaActionCache?.GetRow(actionId);
            limitBreakCast = new LimitBreak(gracePeriod, isTankLb, action?.Name!);
            limitBreakStopwatch.Restart();
            
            OnLimitBreak?.Invoke(this, new LimitBreakEvent(isTankLb, true));
        }

        private unsafe void DetectClipping()
        {
            var animationLock = actionManager->animationLock;
            if (lastDetectedClip == actionManager->currentSequence || actionManager->isGCDRecastActive || animationLock <= 0)
            {
                return;
            }

            if (animationLock >= 0.1f)
            {
                Service.Log.Debug($"GCD Clip: {animationLock} s");
                if (limitBreakCast == null)
                {
                    Service.Log.Debug($"Sending clipping event");
                    OnGcdClip?.Invoke(this, animationLock);
                }
                else if(!limitBreakCast.IsTankLb)
                {
                    limitBreakCast.GracePeriod += animationLock - 2.9f;
                }
            }

            lastDetectedClip = actionManager->currentSequence;
        }

        private unsafe void DetectWastedGCD()
        {
            // do not track dropped GCDs if the LB is being cast
            // or the player died between 2 GCDs
            if (limitBreakCast != null)
            {
                return;
            }
            if (!actionManager->isGCDRecastActive && !actionManager->isQueued)
            {
                if (actionManager->animationLock > 0) return;
                currentWastedGcd += ImGui.GetIO().DeltaTime;
                if (!isGcdDropped && currentWastedGcd > GcdDropThreshold)
                {
                    isGcdDropped = true;
                    OnGcdDropped?.Invoke(this, EventArgs.Empty);
                }
            }
            else if (currentWastedGcd > 0)
            {
                Service.Log.Debug($"Wasted GCD: {currentWastedGcd} ms");
                currentWastedGcd = 0;
                isGcdDropped = false;
            }
        }

        private void OnDeath(object sender, bool isDead)
        {
            if (limitBreakCast != null)
            {
                limitBreakStopwatch.Reset();
                limitBreakCast = null;
                OnLimitBreakCanceled?.Invoke(this, EventArgs.Empty);
            }
        }

        private unsafe void OnFlyText(
            ref FlyTextKind kind,
            ref int val1,
            ref int val2,
            ref SeString text1,
            ref SeString text2,
            ref uint color,
            ref uint icon,
            ref uint damageTypeIcon,
            ref float yOffset,
            ref bool handled)
        {
            
            if (!Plugin.CanRunDmc())
            {
                return;
            }
            {
                var damage = val1;
                var actionName = text1.ToString();


                if (actionName == null || text2 == null)
                {
                    return;
                }
                if (actionName.EndsWith("\\u00A7") && actionName.Length >= 1)
                {
                    return;
                }

                // TODO Some DoTs deal no damage on application,
                // will have to figure out what to do about that
                if (actionName.StartsWith('+'))
                {
                    actionName = actionName.Substring(2);
                }
                
                if (!validTextKind.Contains(kind) && 
                    (limitBreakCast == null || limitBreakCast.Name != actionName))
                {
                        return;
                    
                }

                if (limitBreakCast != null && actionName == limitBreakCast.Name)
                {
                    OnLimitBreakEffect?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    OnFlyTextCreation?.Invoke(this, damage);
                }
            }
        }
    }
}
