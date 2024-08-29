using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using DragoonMayCry.State.Tracker;
using System;
using System.Linq;
using DragoonMayCry.Data;
using DragoonMayCry.Util;

namespace DragoonMayCry.State
{
    public class PlayerState : IDisposable
    {
        public bool IsInCombat => CheckCondition([ConditionFlag.InCombat]);
        public bool IsInsideInstance => CheckCondition([ConditionFlag.BoundByDuty]);
        public bool IsDead => Player != null && Player.IsDead;
        public bool IsLoggedIn => Player != null;
        public IPlayerCharacter? Player => Service.ClientState.LocalPlayer;
        private ICondition Condition => Service.Condition;
        
        private bool CheckCondition(ConditionFlag[] conditionFlags) => conditionFlags.Any(x => Condition[x]);

        private readonly InCombatStateTracker inCombatStateTracker;
        private readonly OnDeathStateTracker onDeathStateTracker;
        private readonly OnEnteringInstanceStateTracker onEnteringInstanceStateTracker;
        private readonly LoginStateTracker loginStateTracker;
        private readonly JobChangeTracker jobChangeTracker;
        private static PlayerState? Instance;
        private PlayerState()
        {
            inCombatStateTracker = new();
            onDeathStateTracker = new();
            onEnteringInstanceStateTracker = new();
            loginStateTracker = new();
            jobChangeTracker = new();
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
            if (!IsCombatJob())
            {
                return;
            }
            inCombatStateTracker.Update(this);
            onDeathStateTracker.Update(this);
            onEnteringInstanceStateTracker.Update(this);
            loginStateTracker.Update(this);
            jobChangeTracker.Update(this);
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

        public void Dispose()
        {
            Service.Framework.Update -= Update;
        }
    }
}
