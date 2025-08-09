#region

using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using DragoonMayCry.Data;
using DragoonMayCry.State.Tracker;
using DragoonMayCry.Util;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using ActionManager = FFXIVClientStructs.FFXIV.Client.Game.ActionManager;
using CSFramework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework;
using DalamudGameObject = Dalamud.Game.ClientState.Objects.Types.IGameObject;
using ObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;

#endregion

namespace DragoonMayCry.State
{
    public unsafe class PlayerState : IDisposable
    {
        private static PlayerState? Instance;
        private readonly IClientState clientState;
        private readonly DebuffTracker debuffTracker;

        private readonly InCombatStateTracker inCombatStateTracker;

        private readonly ConditionFlag[] inCutscene =
        [
            ConditionFlag.WatchingCutscene, ConditionFlag.WatchingCutscene78, ConditionFlag.OccupiedInCutSceneEvent,
            ConditionFlag.Occupied38,
        ];
        private readonly JobChangeTracker jobChangeTracker;
        private readonly LoginStateTracker loginStateTracker;
        private readonly OnDeathStateTracker onDeathStateTracker;
        private readonly OnEnteringInstanceStateTracker onEnteringInstanceStateTracker;
        private readonly PvpStateTracker pvpStateTracker;

        private readonly ConditionFlag[] unableToAct =
        [
            ConditionFlag.Transformed, ConditionFlag.Swimming,
            ConditionFlag.Diving, ConditionFlag.WatchingCutscene,
            ConditionFlag.OccupiedInCutSceneEvent, ConditionFlag.WatchingCutscene78,
        ];

        private PlayerState()
        {
            inCombatStateTracker = new InCombatStateTracker();
            onDeathStateTracker = new OnDeathStateTracker();
            onEnteringInstanceStateTracker = new OnEnteringInstanceStateTracker();
            loginStateTracker = new LoginStateTracker();
            jobChangeTracker = new JobChangeTracker();
            debuffTracker = new DebuffTracker();
            pvpStateTracker = new PvpStateTracker();
            clientState = Service.ClientState;
            Service.Framework.Update += Update;
        }
        public bool IsInCombat => CheckCondition([ConditionFlag.InCombat]);

        public bool IsInsideInstance =>
            CheckCondition([ConditionFlag.BoundByDuty, ConditionFlag.BoundByDuty56, ConditionFlag.BoundByDuty95]);
        public bool IsInCutscene => IsLoggedIn && CheckCondition(inCutscene);
        public bool IsDead => Player is
        {
            IsDead: true,
        };
        public bool IsLoggedIn => Player != null;
        public IPlayerCharacter? Player => Service.ClientState.LocalPlayer;
        private ICondition Condition => Service.Condition;

        private static RaptureAtkModule* RaptureAtkModule =>
            CSFramework.Instance()->GetUIModule()->GetRaptureAtkModule();

        public void Dispose()
        {
            Service.Framework.Update -= Update;
        }

        private bool CheckCondition(ConditionFlag[] conditionFlags)
        {
            return conditionFlags.Any(x => Condition[x]);
        }

        public static PlayerState GetInstance()
        {
            if (Instance == null)
            {
                Instance = new PlayerState();
            }

            return Instance;
        }

        private void Update(IFramework framework)
        {
            onEnteringInstanceStateTracker.Update(this);
            loginStateTracker.Update(this);
            jobChangeTracker.Update(this);
            pvpStateTracker.Update(this);
            if (!CanUpdateStates())
            {
                return;
            }

            onDeathStateTracker.Update(this);
            inCombatStateTracker.Update(this);
            debuffTracker.Update(this);
        }

        private bool CanUpdateStates()
        {
            if (!IsInsideInstance
                && (Plugin.Configuration == null || !Plugin.Configuration.ActiveOutsideInstance)
                || IsInPvp())
            {
                return false;
            }

            return IsCombatJob() && Plugin.IsEnabledForCurrentJob();
        }

        public bool IsInPvp()
        {
            return Service.ClientState.IsPvP;
        }

        public void RegisterCombatStateChangeHandler(EventHandler<bool> inCombatHandler)
        {
            inCombatStateTracker.OnChange += inCombatHandler;
        }

        public void RegisterDeathStateChangeHandler(EventHandler<bool> onDeathHandler)
        {
            onDeathStateTracker.OnChange += onDeathHandler;
        }

        public void RegisterInstanceChangeHandler(EventHandler<bool> onEnteringInstanceHandler)
        {
            onEnteringInstanceStateTracker.OnChange += onEnteringInstanceHandler;
        }

        public void RegisterLoginStateChangeHandler(EventHandler<bool> onLoginStateChange)
        {
            loginStateTracker.OnChange += onLoginStateChange;
        }

        public void RegisterJobChangeHandler(EventHandler<JobId> onJobChange)
        {
            jobChangeTracker.OnChange += onJobChange;
        }

        public void RegisterDamageDownHandler(EventHandler<bool> onDamageDown)
        {
            debuffTracker.OnChange += onDamageDown;
        }

        public void RegisterPvpStateChangeHandler(EventHandler<bool> onPvpStateChange)
        {
            pvpStateTracker.OnChange += onPvpStateChange;
        }

        public JobId GetCurrentJob()
        {
            return jobChangeTracker.CurrentJob;
        }

        public bool IsTank()
        {
            return JobHelper.IsTank(GetCurrentJob());
        }

        public bool IsCombatJob()
        {
            return JobHelper.IsCombatJob(GetCurrentJob());
        }

        public bool IsIncapacitated()
        {
            if (Player == null)
            {
                return true;
            }

            if (CheckCondition(unableToAct))
            {
                return true;
            }

            var statuses = Player.StatusList;
            for (var i = 0; i < statuses.Length; i++)
            {
                var status = statuses[i];
                if (status == null)
                {
                    continue;
                }

                if (DebuffIds.IsIncapacitatingDebuff(status.StatusId))
                {
                    return true;
                }
            }

            return false;
        }

        public bool CanTargetEnemy()
        {
            var player = Player != null ? (GameObject*)Player.Address : null;
            if (player == null)
                return false;

            var targets = Service.ObjectTable.Where(o =>
                                                        ObjectKind.BattleNpc.Equals(o.ObjectKind)
                                                        && !o.Equals(Player)
                                                        && CanAttack(o)
                                                        && IsKillable(o)
            ).ToArray();
            return targets.Length > 0;
        }

        private static bool IsKillable(DalamudGameObject o)
        {
            var character = (Character*)o.Address;
            if (character == null)
            {
                return false;
            }

            return character->GetIsTargetable() && character->Health > 1;
        }

        private static bool CanAttack(DalamudGameObject o)
        {
            var go = (GameObject*)o.Address;
            if (go == null)
            {
                return false;
            }

            return go->GetIsTargetable()
                   && ActionManager.CanUseActionOnTarget(142, go);
        }

        public uint GetCurrentTerritoryId()
        {
            return clientState.TerritoryType;
        }

        public uint GetCurrentContentId()
        {
            return GameMain.Instance()->CurrentContentFinderConditionId;
        }
    }
}
