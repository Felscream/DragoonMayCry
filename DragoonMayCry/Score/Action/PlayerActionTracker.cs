using Dalamud.Game.Gui.FlyText;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using DragoonMayCry.State;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using ImGuiNET;
using KamiLib.Caching;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
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
            public uint ActionId { get; set; }

            public LimitBreak(float gracePeriod, bool isTankLb, uint id)
            {
                GracePeriod = gracePeriod;
                IsTankLb = isTankLb;
                ActionId = id;
            }
        }



        private readonly HashSet<FlyTextKind> validTextKind = new() {
            FlyTextKind.Damage,
            FlyTextKind.DamageCrit,
            FlyTextKind.DamageDh,
            FlyTextKind.DamageCritDh,
        };

        public EventHandler? OnGcdDropped;
        public EventHandler<float>? DamageActionUsed;
        public EventHandler<float>? OnGcdClip;
        public EventHandler<LimitBreakEvent>? UsingLimitBreak;
        public EventHandler? OnLimitBreakEffect;
        public EventHandler? OnLimitBreakCanceled;
        public EventHandler? ActionFlyTextCreated;


        private delegate void OnActionUsedDelegate(
            uint sourceId, nint sourceCharacter, nint pos,
            nint effectHeader, nint effectArray, nint effectTrail);

        private readonly Hook<OnActionUsedDelegate>? onActionUsedHook;

        private delegate void OnActorControlDelegate(
            uint entityId, uint id, uint unk1, uint type, uint unk2, uint unk3,
            uint unk4, uint unk5, ulong targetId, byte unk6);

        private readonly Hook<OnActorControlDelegate>? onActorControlHook;

        private delegate void OnCastDelegate(
            uint sourceId, nint sourceCharacter);

        private readonly Hook<OnCastDelegate>? onCastHook;

        public delegate void AddToScreenLogWithLogMessageId(BattleChara* target, BattleChara* dealer, int a3, char a4, int castID, int a6, int a7, int a8);
        readonly Hook<AddToScreenLogWithLogMessageId>? addToScreenLogWithLogMessageId = null;

        private readonly State.ActionManagerLight* actionManager;
        private readonly PlayerState playerState;
        private readonly LuminaCache<LuminaAction> luminaActionCache;

        private const float GcdDropThreshold = 0.2f;
        private ushort lastDetectedClip = 0;
        private float currentWastedGcd = 0;

        private bool isGcdDropped;

        private readonly Stopwatch limitBreakStopwatch;
        private LimitBreak? limitBreakCast;
        private const int MaxActionHistorySize = 6;
        private readonly Queue<UsedAction> actionHistory;
        private readonly HashSet<FlyTextKind> validHitTypes = new() { FlyTextKind.Damage, FlyTextKind.DamageDh, FlyTextKind.DamageCrit, FlyTextKind.DamageCritDh };

        // added 0.1f to all duration
        private readonly Dictionary<uint, float> tankLimitBreakDelays =
            new()
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
            actionHistory = new();
            luminaActionCache = LuminaCache<LuminaAction>.Instance;
            playerState = PlayerState.GetInstance();
            limitBreakStopwatch = new Stopwatch();
            actionManager =
                (State.ActionManagerLight*)FFXIVClientStructs.FFXIV.Client.Game.ActionManager.Instance();
            Service.FlyText.FlyTextCreated += OnFlyText;
            try
            {
                onActionUsedHook = Service.Hook.HookFromSignature<OnActionUsedDelegate>("40 ?? 56 57 41 ?? 41 ?? 41 ?? 48 ?? ?? ?? ?? ?? ?? ?? 48", OnActionUsed);


                onActorControlHook = Service.Hook.HookFromSignature<OnActorControlDelegate>("E8 ?? ?? ?? ?? 0F B7 0B 83 E9 64", OnActorControl);


                onCastHook = Service.Hook.HookFromSignature<OnCastDelegate>("40 56 41 56 48 81 EC ?? ?? ?? ?? 48 8B F2", OnCast);
                addToScreenLogWithLogMessageId = Service.Hook.HookFromSignature<AddToScreenLogWithLogMessageId>("E8 ?? ?? ?? ?? 8B 8C 24 ?? ?? ?? ?? 85 C9", OnLogMessage);
            }
            catch (Exception e)
            {
                Service.Log.Error("Error initiating hooks: " + e.Message);
            }

            onActionUsedHook?.Enable();
            onActorControlHook?.Enable();
            onCastHook?.Enable();
            addToScreenLogWithLogMessageId?.Enable();

            Service.Framework.Update += Update;
            playerState.RegisterCombatStateChangeHandler(OnCombat);
            playerState.RegisterDeathStateChangeHandler(OnDeath);
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

            addToScreenLogWithLogMessageId?.Disable();
            addToScreenLogWithLogMessageId?.Dispose();
        }

        private void OnLogMessage(BattleChara* target, BattleChara* dealer, int hitType, char a4, int castID, int damage, int a7, int a8)
        {
            addToScreenLogWithLogMessageId?.Original(target, dealer, hitType, a4, castID, damage, a7, a8);

            if (!Plugin.CanRunDmc() || dealer == null || target == null || playerState.Player == null)
            {
                return;
            }

            if (dealer->GetGameObjectId() != playerState.Player.GameObjectId)
            {
                return;
            }

            if (limitBreakCast != null && limitBreakCast.ActionId == (uint)castID)
            {
                OnLimitBreakEffect?.Invoke(this, EventArgs.Empty);
                return;
            }

            var kind = GetHitType(hitType);
            if (!validHitTypes.Contains(kind) || dealer->Character.GetGameObjectId().ObjectId == target->Character.GetGameObjectId().ObjectId)
            {
                return;
            }

            RegisterAndFireUsedAction(kind, damage, (uint)castID);
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
            if (player == null || sourceId != player.GameObjectId)
            {
                return;
            }

            var actionId = Marshal.ReadInt32(effectHeader, 0x8);

            var type = TypeForActionId((uint)actionId);

            if (type == PlayerActionType.LimitBreak)
            {
                StartLimitBreakUse((uint)actionId);
            }
        }

        private FlyTextKind GetHitType(int hitType)
        {
            return hitType switch
            {
                448 => FlyTextKind.DamageDh,
                451 => FlyTextKind.DamageCritDh,
                510 => FlyTextKind.Damage,
                511 => FlyTextKind.DamageCrit,
                519 => FlyTextKind.Healing,
                526 => FlyTextKind.Buff,
                _ => FlyTextKind.AutoAttackOrDot
            };
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
                7 => PlayerActionType.AutoAttack,
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
            var type = TypeForActionId(actionId);

            if (type == PlayerActionType.LimitBreak)
            {
                StartLimitBreakUse(actionId);
            }
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
            currentWastedGcd = 0;
            actionHistory.Clear();
            if (!enteredCombat)
            {
                if (limitBreakCast != null || limitBreakStopwatch.IsRunning)
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
                UsingLimitBreak?.Invoke(this, new LimitBreakEvent(false, false));
            }
        }

        private void CancelLimitBreak()
        {
            limitBreakStopwatch.Reset();
            limitBreakCast = null;
            if (playerState.IsInCombat)
            {
                OnLimitBreakCanceled?.Invoke(this, EventArgs.Empty);
                UsingLimitBreak?.Invoke(this, new LimitBreakEvent(false, false));
            }
        }

        private void StartLimitBreakUse(uint actionId)
        {
            if (!playerState.IsInCombat || limitBreakCast != null)
            {
                return;
            }

            var isTankLb = playerState.IsTank();
            if (isTankLb && !tankLimitBreakDelays.ContainsKey(actionId))
            {
                return;
            }

            var castTime = GetCastTime(actionId);

            // the +3 is just to give enough time to register the gcd clipping just after
            var gracePeriod = isTankLb ? tankLimitBreakDelays[actionId] : castTime + 3f;

            var action = luminaActionCache?.GetRow(actionId);
            limitBreakCast = new LimitBreak(gracePeriod, isTankLb, actionId);
            limitBreakStopwatch.Restart();

            UsingLimitBreak?.Invoke(this, new LimitBreakEvent(isTankLb, true));
        }

        private static bool IsGcdClipped(float animationLock)
        {
            if (Plugin.IsEmdModeEnabled())
            {
                return animationLock != 0.1f;
            }
            return animationLock > 0.2f;
        }

        private unsafe void DetectClipping()
        {
            var animationLock = actionManager->animationLock;
            if (lastDetectedClip == actionManager->currentSequence
                || actionManager->isGCDRecastActive
                || animationLock <= 0)
            {
                return;
            }

            if (IsGcdClipped(animationLock))
            {
                Service.Log.Debug($"GCD Clip: {animationLock} s");
                if (limitBreakCast == null)
                {
                    if (Plugin.IsEmdModeEnabled() && !playerState.IsIncapacitated() && playerState.CanTargetEnemy())
                    {
                        OnGcdDropped?.Invoke(this, EventArgs.Empty);
                    }
                    else
                    {
                        OnGcdClip?.Invoke(this, animationLock);
                    }
                }
                else if (!limitBreakCast.IsTankLb)
                {
                    limitBreakCast.GracePeriod += animationLock - 2.9f;
                }
            }

            lastDetectedClip = actionManager->currentSequence;
        }

        private float GetGcdDropThreshold()
        {
            return Plugin.IsEmdModeEnabled() ? 0 : GcdDropThreshold;
        }

        private unsafe void DetectWastedGCD()
        {
            // do not track dropped GCDs if the LB is being cast
            // or the player died between 2 GCDs
            if (playerState.IsDead)
            {
                return;
            }
            if (!actionManager->isGCDRecastActive && !actionManager->isQueued && !actionManager->isCasting)
            {
                if (actionManager->animationLock > 0) return;
                currentWastedGcd += ImGui.GetIO().DeltaTime;
                if (!isGcdDropped && currentWastedGcd > GetGcdDropThreshold())
                {
                    isGcdDropped = true;
                    if (!playerState.IsIncapacitated() && playerState.CanTargetEnemy() && limitBreakCast == null)
                    {
                        OnGcdDropped?.Invoke(this, EventArgs.Empty);
                    }

                }
            }
            else if (currentWastedGcd > 0)
            {
                Service.Log.Debug($"Wasted GCD: {currentWastedGcd} ms");
                currentWastedGcd = 0;
                isGcdDropped = false;
            }
        }

        private void OnDeath(object? sender, bool isDead)
        {
            if (limitBreakCast != null)
            {
                limitBreakStopwatch.Reset();
                limitBreakCast = null;
                OnLimitBreakCanceled?.Invoke(this, EventArgs.Empty);
                UsingLimitBreak?.Invoke(this, new LimitBreakEvent(false, false));
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

            if (!Plugin.CanRunDmc() || !Plugin.IsMultiHitLoaded() || color == 4278190218)
            {
                return;
            }

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

            if (!validTextKind.Contains(kind))
            {
                return;
            }
            ActionFlyTextCreated?.Invoke(this, EventArgs.Empty);
        }

        private void RegisterAndFireUsedAction(FlyTextKind kind, int damage, uint actionId)
        {
            var usedAction = new UsedAction(actionId, damage, kind);
            if (actionHistory.Contains(usedAction))
            {
                return;
            }

            if (actionHistory.Count >= MaxActionHistorySize)
            {
                actionHistory.Dequeue();
            }
            actionHistory.Enqueue(usedAction);

            if (limitBreakCast != null && actionId == limitBreakCast.ActionId)
            {
                OnLimitBreakEffect?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                DamageActionUsed?.Invoke(this, damage);
            }
        }

        private class UsedAction
        {
            public uint ActionId { get; private set; }
            public int Damage { get; private set; }
            public FlyTextKind Kind { get; private set; }

            public UsedAction(uint actionId, int damage, FlyTextKind kind)
            {
                ActionId = actionId;
                Damage = damage;
                Kind = kind;
            }

            public override bool Equals(object? obj)
            {
                return obj is UsedAction text &&
                       ActionId == text.ActionId &&
                       Damage == text.Damage &&
                       Kind == text.Kind;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(ActionId, Damage, Kind);
            }
        }
    }
}
