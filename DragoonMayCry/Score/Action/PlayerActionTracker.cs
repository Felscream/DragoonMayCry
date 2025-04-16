#region

using Dalamud.Game.Gui.FlyText;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using DragoonMayCry.Data;
using DragoonMayCry.Score.Action.JobModule;
using DragoonMayCry.State;
using DragoonMayCry.Util;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using ImGuiNET;
using Lumina.Excel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ActionManager = FFXIVClientStructs.FFXIV.Client.Game.ActionManager;
using LuminaAction = Lumina.Excel.Sheets.Action;

#endregion

namespace DragoonMayCry.Score.Action
{
    public unsafe class PlayerActionTracker : IDisposable
    {
        public delegate void AddToScreenLogWithLogMessageId(
            BattleChara* target, BattleChara* dealer, int a3, char a4, int castId, int a6, int a7, int a8);

        private const float DefaultGcdDropThreshold = 0.2f;
        private const int MaxActionHistorySize = 6;
        private readonly Queue<UsedAction> actionHistory;

        private readonly ActionManager* actionManagerL;

        private readonly Hook<AddToScreenLogWithLogMessageId>? addToScreenLogWithLogMessageId;
        private readonly IDutyState dutyState;

        private readonly Stopwatch limitBreakStopwatch;
        private readonly ExcelSheet<LuminaAction> luminaActionSheet;

        private readonly Hook<ActionUsedDelegate>? onActionUsedHook;

        private readonly Hook<ActorControlDelegate>? onActorControlHook;

        private readonly Hook<CastDelegate>? onCastHook;
        private readonly PlayerState playerState;
        private readonly Stopwatch spellCastStopwatch;

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

        private readonly HashSet<FlyTextKind> validHitTypes = new()
            { FlyTextKind.Damage, FlyTextKind.DamageDh, FlyTextKind.DamageCrit, FlyTextKind.DamageCritDh };

        private readonly HashSet<FlyTextKind> validTextKind =
        [
            FlyTextKind.Damage,
            FlyTextKind.DamageCrit,
            FlyTextKind.DamageDh,
            FlyTextKind.DamageCritDh,

        ];
        public EventHandler? ActionFlyTextCreated;
        private float combatWastedGcd;
        private JobId currentJob = JobId.OTHER;
        public EventHandler<DamagePayload>? DamageActionUsed;
        public EventHandler<DutyCompletionStats>? DutyCompletedWastedGcd;
        public EventHandler<float>? GcdClip;

        public EventHandler? GcdDropped;

        private bool isGcdDropped;
        private IJobActionModifier? jobActionModule;
        private JobModuleFactory? jobModuleFactory;
        private ushort lastDetectedClip;
        public EventHandler? LimitBreakCanceled;
        private LimitBreak? limitBreakCast;
        public EventHandler? LimitBreakEffect;

        private uint spellCastId = uint.MaxValue;
        public EventHandler<float>? TotalCombatWastedGcd;
        public EventHandler<LimitBreakEvent>? UsingLimitBreak;
        private float wastedGcd;

        public PlayerActionTracker()
        {
            actionHistory = new Queue<UsedAction>();
            luminaActionSheet = Service.DataManager.GetExcelSheet<LuminaAction>();
            playerState = PlayerState.GetInstance();
            currentJob = playerState.GetCurrentJob();
            dutyState = Service.DutyState;
            dutyState.DutyCompleted += OnDutyCompleted;

            limitBreakStopwatch = new Stopwatch();
            spellCastStopwatch = new Stopwatch();
            actionManagerL = ActionManager.Instance();
            Service.FlyText.FlyTextCreated += OnFlyText;
            try
            {
                onActionUsedHook =
                    Service.Hook.HookFromSignature<ActionUsedDelegate>(
                        "40 55 53 56 41 54 41 55 41 56 41 57 48 8D AC 24 ?? ?? ?? ?? 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 45 70 ",
                        OnActionUsed);


                onActorControlHook =
                    Service.Hook.HookFromSignature<ActorControlDelegate>(
                        "E8 ?? ?? ?? ?? 0F B7 0B 83 E9 64", OnActorControl);


                onCastHook =
                    Service.Hook.HookFromSignature<CastDelegate>("40 56 41 56 48 81 EC ?? ?? ?? ?? 48 8B F2", OnCast);
                addToScreenLogWithLogMessageId =
                    Service.Hook.HookFromSignature<AddToScreenLogWithLogMessageId>(
                        "E8 ?? ?? ?? ?? 8B 8C 24 ?? ?? ?? ?? 85 C9", OnLogMessage);
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
            playerState.RegisterDamageDownHandler(OnFailedMechanic);
            playerState.RegisterJobChangeHandler(OnJobChanged);
        }

        public void Dispose()
        {
            Service.Framework.Update -= Update;
            dutyState.DutyCompleted -= OnDutyCompleted;

            onActionUsedHook?.Disable();
            onActionUsedHook?.Dispose();

            onActorControlHook?.Disable();
            onActorControlHook?.Dispose();

            onCastHook?.Disable();
            onCastHook?.Dispose();

            addToScreenLogWithLogMessageId?.Disable();
            addToScreenLogWithLogMessageId?.Dispose();
        }

        private void OnLogMessage(
            BattleChara* target, BattleChara* dealer, int hitType, char a4, int actionId, int damage, int a7, int a8)
        {
            addToScreenLogWithLogMessageId?.Original(target, dealer, hitType, a4, actionId, damage, a7, a8);

            if (!Plugin.CanRunDmc() || dealer == null || target == null || playerState.Player == null)
            {
                return;
            }

            if (dealer->EntityId != playerState.Player.EntityId &&
                dealer->CompanionOwnerId != playerState.Player.EntityId)
            {
                return;
            }

            if (limitBreakCast != null && limitBreakCast.ActionId == (uint)actionId)
            {
                limitBreakCast.TargetHit++;
                if (limitBreakCast.TargetHit < 2)
                {
                    LimitBreakEffect?.Invoke(this, EventArgs.Empty);
                }

                return;
            }

            if (target->Character.Health < 2)
            {
                return;
            }

            var kind = HitTypeHelper.GetHitType(hitType);
            if (!validHitTypes.Contains(kind) ||
                dealer->Character.GetGameObjectId() == target->Character.GetGameObjectId())
            {
                if (jobActionModule != null && kind != FlyTextKind.MpRegen)
                {
                    var bonusPoints = jobActionModule.OnActionAppliedOnTarget((uint)actionId);
                    if (bonusPoints > 0)
                    {
                        DamageActionUsed?.Invoke(this, new DamagePayload(bonusPoints, kind));
                        ResetLimitBreakUse();
                    }
                }
                return;
            }

            RegisterAndFireUsedAction(kind, damage, (uint)actionId);
        }

        private void OnFailedMechanic(object? sender, bool hasFailedMechanic)
        {
            if (Plugin.CanRunDmc() && hasFailedMechanic)
            {
                combatWastedGcd += 3f;
                Service.Log.Information($"failed Wasted GCD {combatWastedGcd}");
            }
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
            if (!playerState.CanTargetEnemy())
            {
                return;
            }

            var actionId = Marshal.ReadInt32(effectHeader, 0x8);
            if (spellCastId == actionId)
            {
                spellCastStopwatch.Reset();
                spellCastId = uint.MaxValue;
            }

            var type = TypeForActionId((uint)actionId);
            if (type == PlayerActionType.LimitBreak)
            {
                StartLimitBreakUse((uint)actionId);
            }

            var bonusPoints = jobActionModule?.OnAction((uint)actionId);
            if (bonusPoints > 0)
            {

                DamageActionUsed?.Invoke(this, new DamagePayload(bonusPoints.Value, FlyTextKind.None));
                ResetLimitBreakUse();
            }
        }

        private PlayerActionType TypeForActionId(uint actionId)
        {
            if (luminaActionSheet.TryGetRow(actionId, out var actionRow))
            {
                return actionRow.ActionCategory.Value.RowId switch
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

            return PlayerActionType.Other;
        }

        private void OnActorControl(
            uint entityId, uint type, uint buffId, uint direct, uint actionId,
            uint sourceId, uint arg4, uint arg5, ulong targetId, byte a10)
        {
            onActorControlHook?.Original(entityId, type, buffId, direct,
                                         actionId, sourceId, arg4, arg5,
                                         targetId, a10);

            if (!Plugin.CanRunDmc())
            {
                return;
            }

            // 15 seems to be related to cast cancelation
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
                if (playerState.CanTargetEnemy())
                {
                    combatWastedGcd += (float)limitBreakStopwatch.Elapsed.TotalSeconds;
                    Service.Log.Information($"LB Wasted GCD {combatWastedGcd}");
                }

                CancelLimitBreak();
            }
            else if (spellCastStopwatch.IsRunning)
            {
                combatWastedGcd += (float)spellCastStopwatch.Elapsed.TotalSeconds;
                Service.Log.Information($"Cast Wasted GCD {combatWastedGcd}");
            }

            spellCastStopwatch.Reset();
            spellCastId = uint.MaxValue;
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
            else if (playerState.CanTargetEnemy())
            {
                ResetLimitBreakUse();
                spellCastId = actionId;
                spellCastStopwatch.Restart();
            }
        }

        private float GetCastTime(uint actionId)
        {
            var actionManager = ActionManager.Instance();
            var adjustedId = actionManager->GetAdjustedActionId(actionId);
            return ActionManager.GetAdjustedCastTime(ActionType.Action, adjustedId) / 1000f;
        }

        private void Update(IFramework framework)
        {
            if (!Plugin.CanRunDmc())
            {
                return;
            }

            DetectClipping();
            HandleLimitBreakUse();
            DetectWastedGcd();
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
            wastedGcd = 0;
            actionHistory.Clear();

            if (!enteredCombat)
            {
                if (limitBreakCast != null || limitBreakStopwatch.IsRunning)
                {
                    ResetLimitBreakUse();
                }

                if (Plugin.IsEnabledForCurrentJob())
                {
                    if (spellCastStopwatch.IsRunning)
                    {
                        combatWastedGcd += (float)spellCastStopwatch.Elapsed.TotalSeconds;
                    }

                    TotalCombatWastedGcd?.Invoke(this, combatWastedGcd);
                }
            }
            else
            {
                combatWastedGcd = 0;
            }

            spellCastStopwatch.Reset();
            spellCastId = uint.MaxValue;
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
                LimitBreakCanceled?.Invoke(this, EventArgs.Empty);
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

            // the +8 is just to give leeway after the LB effect
            var gracePeriod = isTankLb ? tankLimitBreakDelays[actionId] : castTime + 8f;
            limitBreakCast = new LimitBreak(gracePeriod, actionId);
            limitBreakStopwatch.Restart();

            UsingLimitBreak?.Invoke(this, new LimitBreakEvent(isTankLb, true));
        }

        private bool IsGcdClipped(float animationLock)
        {
            if (!Plugin.IsEnabledForCurrentJob() || !Plugin.Configuration!.JobConfiguration.ContainsKey(currentJob))
            {
                return animationLock > 0.2f;
            }

            if (Plugin.IsEmdModeEnabled())
            {
                return animationLock > 0.1f;
            }

            return animationLock > Plugin.Configuration.JobConfiguration[currentJob].GcdDropThreshold.Value;
        }

        private void DetectClipping()
        {
            var animationLock = actionManagerL->AnimationLock;

            Service.Log.Information($"last handled sequence {actionManagerL->LastHandledActionSequence}");
            Service.Log.Information($"last used sequence {actionManagerL->LastUsedActionSequence}");
            if (lastDetectedClip == actionManagerL->LastHandledActionSequence
                || animationLock <= 0)
            {
                return;
            }

            combatWastedGcd += animationLock;
            Service.Log.Information($"Clipping Wasted GCD {combatWastedGcd}");

            if (IsGcdClipped(animationLock)
                && limitBreakCast == null
                && !playerState.IsIncapacitated()
                && playerState.CanTargetEnemy())
            {

                if (Plugin.IsEmdModeEnabled())
                {
                    GcdDropped?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    GcdClip?.Invoke(this, animationLock);
                }
            }

            lastDetectedClip = actionManagerL->LastUsedActionSequence;
        }

        private void OnJobChanged(object? sender, JobId job)
        {
            currentJob = job;
            jobActionModule = jobModuleFactory?.GetJobActionModule();
        }

        private float GetGcdDropThreshold()
        {
            if (!Plugin.IsEnabledForCurrentJob() || !Plugin.Configuration!.JobConfiguration.ContainsKey(currentJob))
            {
                return DefaultGcdDropThreshold;
            }

            return Plugin.IsEmdModeEnabled()
                       ? 0
                       : Plugin.Configuration.JobConfiguration[currentJob].GcdDropThreshold.Value;
        }

        private void DetectWastedGcd()
        {
            var isIncapacitated = playerState.IsIncapacitated();
            var canTargetEnemy = playerState.CanTargetEnemy();
            if (!actionManagerL->Gcd
                && actionManagerL->animationLock <= 0
                && !actionManagerL->isCasting
                && limitBreakCast == null
                && !isIncapacitated
                && (canTargetEnemy || playerState.IsDead))
            {
                combatWastedGcd += ImGui.GetIO().DeltaTime;
                Service.Log.Information($"Wasted GCD {combatWastedGcd}");
            }

            // do not track dropped GCDs if the LB is being cast
            // or the player died between 2 GCDs
            if (playerState.IsDead)
            {
                return;
            }

            if (!actionManagerL->isGCDRecastActive && !actionManagerL->isQueued && !actionManagerL->isCasting)
            {
                if (actionManagerL->animationLock > 0) return;
                wastedGcd += ImGui.GetIO().DeltaTime;
                if (!isGcdDropped && wastedGcd > GetGcdDropThreshold())
                {
                    isGcdDropped = true;
                    if (!isIncapacitated && canTargetEnemy && limitBreakCast == null)
                    {
                        GcdDropped?.Invoke(this, EventArgs.Empty);
                    }
                }
            }
            else if (wastedGcd > 0)
            {
                wastedGcd = 0;
                isGcdDropped = false;
            }
        }

        private void OnDeath(object? sender, bool isDead)
        {
            if (limitBreakCast != null)
            {
                limitBreakStopwatch.Reset();
                limitBreakCast = null;
                LimitBreakCanceled?.Invoke(this, EventArgs.Empty);
                UsingLimitBreak?.Invoke(this, new LimitBreakEvent(false, false));
            }
        }

        internal void SetJobModuleFactory(JobModuleFactory factory)
        {
            jobModuleFactory = factory;
            jobActionModule = jobModuleFactory.GetJobActionModule();
        }

        private void OnFlyText(
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
            if (!Plugin.CanRunDmc() || color == 4278190218)
            {
                return;
            }

            var actionName = text1.ToString();

            if (string.IsNullOrEmpty(actionName))
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
                limitBreakCast.TargetHit++;
                if (limitBreakCast.TargetHit < 2)
                {
                    LimitBreakEffect?.Invoke(this, EventArgs.Empty);
                }
            }
            else
            {
                if (jobActionModule != null)
                {
                    var modifiedPoints = jobActionModule.OnActionAppliedOnTarget(actionId);
                    if (modifiedPoints > 0)
                    {
                        DamageActionUsed?.Invoke(this, new DamagePayload(modifiedPoints, kind));
                        return;
                    }
                }

                DamageActionUsed?.Invoke(this, new DamagePayload(damage, kind));
                ResetLimitBreakUse();
            }
        }

        private void OnDutyCompleted(object? sender, ushort instance)
        {
            if (Plugin.IsEnabledForCurrentJob())
            {
                DutyCompletedWastedGcd?.Invoke(this, new DutyCompletionStats(combatWastedGcd, instance));
            }
        }

        public class DamagePayload(float damage, FlyTextKind hitKind)
        {
            public float Damage { get; } = damage;
            public FlyTextKind HitKind { get; } = hitKind;
        }

        public struct LimitBreakEvent(bool isTankLb, bool isCasting)
        {
            public readonly bool IsTankLb = isTankLb;
            public readonly bool IsCasting = isCasting;
        }

        public class LimitBreak(float gracePeriod, uint id)
        {
            public float GracePeriod { get; set; } = gracePeriod;
            public uint ActionId { get; set; } = id;
            public uint TargetHit { get; set; }
        }


        private delegate void ActionUsedDelegate(
            uint sourceId, nint sourceCharacter, nint pos,
            nint effectHeader, nint effectArray, nint effectTrail);

        private delegate void ActorControlDelegate(
            uint entityId, uint id, uint unk1, uint type, uint unk2, uint unk3,
            uint unk4, uint unk5, ulong targetId, byte unk6);

        private delegate void CastDelegate(
            uint sourceId, nint sourceCharacter);

        public struct DutyCompletionStats
        {
            public float WastedGcd { get; private set; }
            public ushort InstanceId { get; private set; }

            internal DutyCompletionStats(float wastedGcd, ushort instanceId)
            {
                WastedGcd = wastedGcd;
                InstanceId = instanceId;
            }
        }

        private class UsedAction
        {

            public UsedAction(uint actionId, int damage, FlyTextKind kind)
            {
                ActionId = actionId;
                Damage = damage;
                Kind = kind;
            }
            public uint ActionId { get; private set; }
            public int Damage { get; private set; }
            public FlyTextKind Kind { get; private set; }

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
