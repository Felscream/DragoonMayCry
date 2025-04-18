using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using DragoonMayCry.State;
using System.Collections.Generic;

namespace DragoonMayCry.Score.Action.JobModule
{
    internal abstract class DotJob : IJobActionModifier
    {
        protected readonly PlayerState playerState;
        protected readonly ITargetManager targetManager;

        protected DotJob()
        {
            targetManager = Service.TargetManager;
            playerState = PlayerState.GetInstance();
        }
        protected abstract Dictionary<uint, uint> ActionToStatusIds { get; }


        public virtual float OnAction(uint actionId)
        {
            return 0;
        }

        public virtual float OnActionAppliedOnTarget(uint actionId)
        {
            return 0;
        }

        protected virtual bool IsValidDotRefresh(uint actionId)
        {
            if (targetManager.Target?.ObjectKind != ObjectKind.BattleNpc
                || !ActionToStatusIds.ContainsKey(actionId))
            {
                return false;
            }

            var target = (IBattleChara)targetManager.Target;
            var playerId = playerState.Player?.GameObjectId;
            var statuses = target.StatusList;

            if (statuses == null)
            {
                return false;
            }

            var searchedStatus = ActionToStatusIds[actionId];
            for (var i = 0; i < statuses.Length; i++)
            {
                var status = statuses[i];
                if (status != null && status.SourceId == playerId && status.StatusId == searchedStatus)
                {
                    return status.RemainingTime > 0 && status.RemainingTime <= 3.2;
                }
            }
            return false;
        }
    }
}
