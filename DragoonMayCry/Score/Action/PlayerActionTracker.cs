using Dalamud.Game.Gui.FlyText;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using DragoonMayCry.Data;
using DragoonMayCry.Score.Action.JobModule;
using DragoonMayCry.State;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using ImGuiNET;
using KamiLib.Caching;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Lumina.Excel;
using ActionManager = FFXIVClientStructs.FFXIV.Client.Game.ActionManager;
using LuminaAction = Lumina.Excel.Sheets.Action;

namespace DragoonMayCry.Score.Action
{
    public unsafe class PlayerActionTracker : IDisposable
    {
        public struct LimitBreakEvent(bool isTankLb, bool isCasting)
        {
            public readonly bool IsTankLb = isTankLb;
            public readonly bool IsCasting = isCasting;
        }

        public class LimitBreak(float gracePeriod, bool isTankLb, uint id)
        {
            public float GracePeriod { get; set; } = gracePeriod;
            public bool IsTankLb { get; set; } = isTankLb;
            public uint ActionId { get; set; } = id;
            public uint TargetHit { get; set; } = 0;
        }

        private readonly HashSet<FlyTextKind> validTextKind = new()
        {
            FlyTextKind.Damage,
            FlyTextKind.DamageCrit,
            FlyTextKind.DamageDh,
            FlyTextKind.DamageCritDh,
        };

        public EventHandler? GcdDropped;
        public EventHandler<float>? DamageActionUsed;
        public EventHandler<float>? GcdClip;
        public EventHandler<LimitBreakEvent>? UsingLimitBreak;
        public EventHandler? LimitBreakEffect;
        public EventHandler? LimitBreakCanceled;
        public EventHandler? ActionFlyTextCreated;
        public EventHandler<float>? TotalCombatWastedGcd;
        public EventHandler<DutyCompletionStats>? DutyCompletedWastedGcd;


        private delegate void ActionUsedDelegate(
            uint sourceId, nint sourceCharacter, nint pos,
            nint effectHeader, nint effectArray, nint effectTrail);

        private readonly Hook<ActionUsedDelegate>? onActionUsedHook;

        private delegate void ActorControlDelegate(
            uint entityId, uint id, uint unk1, uint type, uint unk2, uint unk3,
            uint unk4, uint unk5, ulong targetId, byte unk6);

        private readonly Hook<ActorControlDelegate>? onActorControlHook;

        private delegate void CastDelegate(
            uint sourceId, nint sourceCharacter);

        private readonly Hook<CastDelegate>? onCastHook;

        public delegate void AddToScreenLogWithLogMessageId(
            BattleChara* target, BattleChara* dealer, int a3, char a4, int castId, int a6, int a7, int a8);

        readonly Hook<AddToScreenLogWithLogMessageId>? addToScreenLogWithLogMessageId;

        private readonly ActionManagerLight* actionManagerL;
        private readonly PlayerState playerState;
        private readonly IDutyState dutyState;
        private readonly ExcelSheet<LuminaAction> luminaActionSheet;

        private const float DefaultGcdDropThreshold = 0.2f;
        private ushort lastDetectedClip = 0;
        private float wastedGcd = 0;
        private float combatWastedGcd = 0;

        private bool isGcdDropped;

        private readonly Stopwatch limitBreakStopwatch;
        private readonly Stopwatch spellCastStopwatch;
        private LimitBreak? limitBreakCast;
        private const int MaxActionHistorySize = 6;
        private readonly Queue<UsedAction> actionHistory;

        private readonly HashSet<FlyTextKind> validHitTypes = new()
            { FlyTextKind.Damage, FlyTextKind.DamageDh, FlyTextKind.DamageCrit, FlyTextKind.DamageCritDh };

        private uint spellCastId = uint.MaxValue;
        private IJobActionModifier? jobActionModule;
        private JobModuleFactory? jobModuleFactory;
        private JobId currentJob = JobId.OTHER;

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
            luminaActionSheet = Service.DataManager.GetExcelSheet<LuminaAction>();
            playerState = PlayerState.GetInstance();
            currentJob = playerState.GetCurrentJob();
            dutyState = Service.DutyState;
            dutyState.DutyCompleted += OnDutyCompleted;

            limitBreakStopwatch = new Stopwatch();
            spellCastStopwatch = new Stopwatch();
            actionManagerL =
                (ActionManagerLight*)ActionManager.Instance();
            Service.FlyText.FlyTextCreated += OnFlyText;
            try
            {
                onActionUsedHook =
                    Service.Hook.HookFromSignature<ActionUsedDelegate>(
                        "40 ?? 56 57 41 ?? 41 ?? 41 ?? 48 ?? ?? ?? ?? ?? ?? ?? 48", OnActionUsed);


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

            var kind = GetHitType(hitType);
            if (!validHitTypes.Contains(kind) ||
                dealer->Character.GetGameObjectId() == target->Character.GetGameObjectId())
            {
                if (jobActionModule != null && kind != FlyTextKind.MpRegen)
                {
                    var bonusPoints = jobActionModule.OnActionAppliedOnTarget((uint)actionId);
                    if (bonusPoints > 0)
                    {
                        DamageActionUsed?.Invoke(this, bonusPoints);
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
                DamageActionUsed?.Invoke(this, bonusPoints.Value);
            }
        }

        private static FlyTextKind GetHitType(int hitType)
        {
            return hitType switch
            {
                448 => FlyTextKind.DamageDh,
                451 => FlyTextKind.DamageCritDh,
                510 => FlyTextKind.Damage,
                511 => FlyTextKind.DamageCrit,
                519 => FlyTextKind.Healing,
                521 => FlyTextKind.MpRegen,
                526 => FlyTextKind.Buff,
                _ => FlyTextKind.AutoAttackOrDot
            };
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

            // the +3 is just to give enough time to register the gcd clipping just after
            var gracePeriod = isTankLb ? tankLimitBreakDelays[actionId] : castTime + 3f;

            limitBreakCast = new LimitBreak(gracePeriod, isTankLb, actionId);
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
                return animationLock != 0.1f;
            }

            return animationLock > Plugin.Configuration.JobConfiguration[currentJob].GcdDropThreshold.Value;
        }

        private void DetectClipping()
        {
            var animationLock = actionManagerL->animationLock;
            if (lastDetectedClip == actionManagerL->currentSequence
                || actionManagerL->isGCDRecastActive
                || animationLock <= 0)
            {
                return;
            }

            if (limitBreakCast == null && animationLock != 0.1f)
            {
                combatWastedGcd += animationLock;
            }

            if (IsGcdClipped(animationLock))
            {
                if (limitBreakCast == null)
                {
                    if (Plugin.IsEmdModeEnabled() && !playerState.IsIncapacitated() && playerState.CanTargetEnemy())
                    {
                        GcdDropped?.Invoke(this, EventArgs.Empty);
                    }
                    else
                    {
                        GcdClip?.Invoke(this, animationLock);
                    }
                }
                else if (!limitBreakCast.IsTankLb)
                {
                    limitBreakCast.GracePeriod += animationLock - 2.9f;
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
            var isIncapacitated = playerState.IsIncapacitated();
            var canTargetEnemy = playerState.CanTargetEnemy();
            if (!actionManagerL->isGCDRecastActive
                && actionManagerL->animationLock == 0
                && !actionManagerL->isCasting
                && limitBreakCast == null
                && !isIncapacitated
                && (canTargetEnemy || playerState.IsDead))
            {
                combatWastedGcd += ImGui.GetIO().DeltaTime;
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
            this.jobModuleFactory = factory;
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
            if (!Plugin.CanRunDmc() || !Plugin.IsMultiHitLoaded() || color == 4278190218)
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
                        DamageActionUsed?.Invoke(this, modifiedPoints);
                        return;
                    }
                }

                DamageActionUsed?.Invoke(this, damage);
            }
        }

        private void OnDutyCompleted(object? sender, ushort instance)
        {
            if (Plugin.IsEnabledForCurrentJob())
            {
                DutyCompletedWastedGcd?.Invoke(this, new DutyCompletionStats(combatWastedGcd, instance));
            }
        }

        public struct DutyCompletionStats
        {
            public float WastedGcd { get; private set; }
            public ushort InstanceId { get; private set; }

            public DutyCompletionStats(float wastedGcd, ushort instanceId)
            {
                WastedGcd = wastedGcd;
                InstanceId = instanceId;
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
