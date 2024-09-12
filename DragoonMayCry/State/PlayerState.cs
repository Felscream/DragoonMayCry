using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Statuses;
using Dalamud.Plugin.Services;
using DragoonMayCry.Data;
using DragoonMayCry.State.Tracker;
using DragoonMayCry.Util;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using ActionManager = FFXIVClientStructs.FFXIV.Client.Game.ActionManager;
using CSFramework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework;
using DalamudGameObject = Dalamud.Game.ClientState.Objects.Types.IGameObject;
using ObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;

namespace DragoonMayCry.State
{
    public unsafe class PlayerState : IDisposable
    {
        public bool IsInCombat => CheckCondition([ConditionFlag.InCombat]);
        public bool IsInsideInstance => CheckCondition([ConditionFlag.BoundByDuty, ConditionFlag.BoundByDuty56, ConditionFlag.BoundByDuty95]);
        public bool IsDead => Player != null && Player.IsDead;
        public bool IsLoggedIn => Player != null;
        public IPlayerCharacter? Player => Service.ClientState.LocalPlayer;
        private ICondition Condition => Service.Condition;
        private static RaptureAtkModule* RaptureAtkModule => CSFramework.Instance()->GetUIModule()->GetRaptureAtkModule();

        private bool CheckCondition(ConditionFlag[] conditionFlags) => conditionFlags.Any(x => Condition[x]);

        private readonly ConditionFlag[] unableToAct = new ConditionFlag[] { ConditionFlag.Transformed, ConditionFlag.Swimming,
            ConditionFlag.Diving, ConditionFlag.WatchingCutscene, 
            ConditionFlag.OccupiedInCutSceneEvent, ConditionFlag.WatchingCutscene78 };

        

        private readonly InCombatStateTracker inCombatStateTracker;
        private readonly OnDeathStateTracker onDeathStateTracker;
        private readonly OnEnteringInstanceStateTracker onEnteringInstanceStateTracker;
        private readonly LoginStateTracker loginStateTracker;
        private readonly JobChangeTracker jobChangeTracker;
        private readonly DebuffTracker debuffTracker;
        private static PlayerState? Instance;
        private PlayerState()
        {
            inCombatStateTracker = new();
            onDeathStateTracker = new();
            onEnteringInstanceStateTracker = new();
            loginStateTracker = new();
            jobChangeTracker = new();
            debuffTracker = new();
            Service.Framework.Update += Update;
        }

        public static PlayerState GetInstance()
        {
            if (Instance == null)
            {
                Instance = new PlayerState();
            }

            return Instance;
        }

        public void Update(IFramework framework)
        {
            if (!CanUpdateStates())
            {
                return;
            }
            onDeathStateTracker.Update(this);
            inCombatStateTracker.Update(this);
            onEnteringInstanceStateTracker.Update(this);
            loginStateTracker.Update(this);
            jobChangeTracker.Update(this);
            debuffTracker.Update(this);
            
        }

        private bool CanUpdateStates()
        {
            if(!IsInsideInstance 
                && Plugin.Configuration != null && !Plugin.Configuration.ActiveOutsideInstance 
                || IsInPvp())
            {
                return false;
            }
            return IsCombatJob();
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

        public void RegisterJobChangeHandler(EventHandler<JobIds> onJobChange)
        {
            jobChangeTracker.OnChange += onJobChange;
        }

        public void RegisterDamageDownHandler(EventHandler<bool> onDamageDown)
        {
            debuffTracker.OnChange += onDamageDown;
        }

        public JobIds GetCurrentJob()
        {
            if (Player == null)
            {
                return JobIds.OTHER;
            }

            return JobHelper.IdToJob(Player.ClassJob.Id);
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
            if(Player == null)
            {
                return true;
            }

            if (CheckCondition(unableToAct))
            {
                return true;
            }

            StatusList statuses = Player.StatusList;
            for(int i = 0; i < statuses.Length; i++)
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
                && o != Player
                && CanAttack(o)
             ).ToList();

            var enemyList = GetEnemyListObjectIds();
            for(int i = 0; i < targets.Count; i++)
            {
                if (enemyList.Contains(targets[i].EntityId))
                {
                    return true;
                }
            }
            return false;
        }

        private bool CanAttack(DalamudGameObject o)
        {
            var address = (GameObject*)o.Address;
            if (address == null)
            {
                return false;
            }
            return address->GetIsTargetable()
                && ActionManager.CanUseActionOnTarget(142, address);
        }

        private ISet<uint> GetEnemyListObjectIds()
        {
            var addonByName = Service.GameGui.GetAddonByName("_EnemyList", 1);
            if (addonByName == IntPtr.Zero)
                return new HashSet<uint>();

            var addon = (AddonEnemyList*)addonByName;
            var numArray = RaptureAtkModule->AtkModule.AtkArrayDataHolder.NumberArrays[21];
            var enemyIdSet = new HashSet<uint>();
            for (var i = 0; i < addon->EnemyCount; i++)
            {
                var id = (uint)numArray->IntArray[8 + (i * 6)];
                enemyIdSet.Add(id);
            }
            return enemyIdSet;
        }

        public void Dispose()
        {
            Service.Framework.Update -= Update;
        }
    }
}
