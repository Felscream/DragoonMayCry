#region

using Dalamud.Bindings.ImGui;
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

        private readonly ActionManagerLight* actionManagerL;

        private readonly Hook<AddToScreenLogWithLogMessageId>? addToScreenLogWithLogMessageId;
        private readonly DmcPlayerState dmcPlayerState;
        private readonly IDutyState dutyState;

        private readonly Stopwatch limitBreakStopwatch;
        private readonly ExcelSheet<LuminaAction> luminaActionSheet;

        private readonly Hook<ActionUsedDelegate>? onActionUsedHook;

        private readonly Hook<CastCancelDelegate>? onCastCancelHook;

        private readonly Hook<CastDelegate>? onCastHook;
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
            dmcPlayerState = DmcPlayerState.GetInstance();
            currentJob = dmcPlayerState.GetCurrentJob();
            dutyState = Service.DutyState;
            dutyState.DutyCompleted += OnDutyCompleted;

            limitBreakStopwatch = new Stopwatch();
            spellCastStopwatch = new Stopwatch();
            actionManagerL = (ActionManagerLight*)ActionManager.Instance();
            Service.FlyText.FlyTextCreated += OnFlyText;
            try
            {
                onActionUsedHook =
                    Service.Hook.HookFromSignature<ActionUsedDelegate>(
                        "40 55 56 57 41 54 41 55 41 56 41 57 48 8D AC 24",
                        OnActionUsed);


                onCastCancelHook =
                    Service.Hook.HookFromSignature<CastCancelDelegate>(
                        "48 8B C4 48 83 EC 48 48 89 58 08", OnCastCancel);


                onCastHook =
                    Service.Hook.HookFromSignature<CastDelegate>("40 53 57 48 81 EC ?? ?? ?? ?? 48 8B FA 8B D1",
                                                                 OnCast);
                addToScreenLogWithLogMessageId =
                    Service.Hook.HookFromSignature<AddToScreenLogWithLogMessageId>(
                        "E8 ?? ?? ?? ?? 0F 28 B4 24 ?? ?? ?? ?? 48 8B 45 CF", OnLogMessage);
            }
            catch (Exception e)
            {
                Service.Log.Error("Error initiating hooks: " + e.Message);
            }

            onActionUsedHook?.Enable();
            onCastCancelHook?.Enable();
            onCastHook?.Enable();
            addToScreenLogWithLogMessageId?.Enable();

            Service.Framework.Update += Update;
            dmcPlayerState.RegisterCombatStateChangeHandler(OnCombat);
            dmcPlayerState.RegisterDeathStateChangeHandler(OnDeath);
            dmcPlayerState.RegisterDamageDownHandler(OnFailedMechanic);
            dmcPlayerState.RegisterJobChangeHandler(OnJobChanged);
        }

        public void Dispose()
        {
            Service.Framework.Update -= Update;
            dutyState.DutyCompleted -= OnDutyCompleted;

            onActionUsedHook?.Disable();
            onActionUsedHook?.Dispose();

            onCastCancelHook?.Disable();
            onCastCancelHook?.Dispose();

            onCastHook?.Disable();
            onCastHook?.Dispose();

            addToScreenLogWithLogMessageId?.Disable();
            addToScreenLogWithLogMessageId?.Dispose();
        }

        private void OnLogMessage(
            BattleChara* target, BattleChara* dealer, int hitType, char a4, int actionId, int damage, int a7, int a8)
        {
            addToScreenLogWithLogMessageId?.Original(target, dealer, hitType, a4, actionId, damage, a7, a8);

            if (!Plugin.CanRunDmc() || dealer == null || target == null || dmcPlayerState.Player == null)
            {
                return;
            }

            if (dealer->EntityId != dmcPlayerState.Player.EntityId &&
                dealer->CompanionOwnerId != dmcPlayerState.Player.EntityId)
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

            var player = dmcPlayerState.Player;
            if (player == null || sourceId != player.GameObjectId)
            {
                return;
            }
            if (!dmcPlayerState.CanTargetEnemy())
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

        private void OnCastCancel(nint actionManager)
        {
            onCastCancelHook?.Original(actionManager);
            if (!Plugin.CanRunDmc())
            {
                return;
            }

            var player = dmcPlayerState.Player;
            if (player == null)
            {
                return;
            }

            if (limitBreakCast != null)
            {
                if (dmcPlayerState.CanTargetEnemy())
                {
                    combatWastedGcd += (float)limitBreakStopwatch.Elapsed.TotalSeconds;
                }

                CancelLimitBreak();
            }
            else if (spellCastStopwatch.IsRunning)
            {
                combatWastedGcd += (float)spellCastStopwatch.Elapsed.TotalSeconds;
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

            var player = dmcPlayerState.Player;
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
            else if (dmcPlayerState.CanTargetEnemy())
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
            if (dmcPlayerState.IsInCombat)
            {
                UsingLimitBreak?.Invoke(this, new LimitBreakEvent(false, false));
            }
        }

        private void CancelLimitBreak()
        {
            limitBreakStopwatch.Reset();
            limitBreakCast = null;
            if (dmcPlayerState.IsInCombat)
            {
                LimitBreakCanceled?.Invoke(this, EventArgs.Empty);
                UsingLimitBreak?.Invoke(this, new LimitBreakEvent(false, false));
            }
        }

        private void StartLimitBreakUse(uint actionId)
        {
            if (!dmcPlayerState.IsInCombat || limitBreakCast != null)
            {
                return;
            }

            var isTankLb = dmcPlayerState.IsTank();
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
            var animationLock = actionManagerL->animationLock;
            if (lastDetectedClip == actionManagerL->currentSequence
                || actionManagerL->isGCDRecastActive
                || actionManagerL->isCasting
                || animationLock <= 0.1f
                || !dmcPlayerState.CanTargetEnemy()
                || limitBreakCast != null)
            {
                return;
            }

            combatWastedGcd += animationLock;

            if (IsGcdClipped(animationLock))
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

            lastDetectedClip = actionManagerL->currentSequence;
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
            var isIncapacitated = dmcPlayerState.IsIncapacitated();
            var canTargetEnemy = dmcPlayerState.CanTargetEnemy();
            if (!actionManagerL->isGCDRecastActive
                && actionManagerL->animationLock <= 0
                && !actionManagerL->isCasting
                && !actionManagerL->isQueued
                && limitBreakCast == null
                && !isIncapacitated
                && (canTargetEnemy || dmcPlayerState.IsDead))
            {
                combatWastedGcd += ImGui.GetIO().DeltaTime;
            }

            // do not track dropped GCDs if the LB is being cast
            // or the player died between 2 GCDs
            if (dmcPlayerState.IsDead)
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

        private delegate void CastCancelDelegate(nint actionManager);

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
