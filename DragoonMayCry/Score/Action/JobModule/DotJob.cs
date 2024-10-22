using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using DragoonMayCry.State;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;

namespace DragoonMayCry.Score.Action.JobModule
{
    internal abstract unsafe class DotJob : IJobActionModifier
    {
        protected abstract Dictionary<uint, uint> StatusIconIds { get; }
        protected const uint PlayerAplliedStatusColor = 4_293_197_769;
        protected readonly ITargetManager targetManager;
        protected readonly PlayerState playerState;

        protected DotJob()
        {
            this.targetManager = Service.TargetManager;
            playerState = PlayerState.GetInstance();
        }

        protected int GetPlayerAppliedStatusesCount()
        {
            if (targetManager.Target?.ObjectKind != Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc)
            {
                return 0;
            }

            var chara = (IBattleChara)targetManager.Target;
            var playerId = playerState.Player?.GameObjectId;
            var appliedStatuses = 0;
            var statuses = chara.StatusList;
            for (var i = 0; i < statuses.Length; i++)
            {
                if (statuses[i]?.StatusId != 0 && statuses[i]?.SourceId == playerId)
                {
                    appliedStatuses++;
                }
            }

            return appliedStatuses;
        }

        protected AtkUnitBase* FindTargetDebuffAddon()
        {
            var targetInfoBuffDebuff = Service.GameGui.GetAddonByName("_TargetInfoBuffDebuff", 1);
            if (targetInfoBuffDebuff != IntPtr.Zero)
            {
                var debuffAddon = (AtkUnitBase*)targetInfoBuffDebuff;
                if (debuffAddon != null && debuffAddon->IsVisible && debuffAddon->UldManager.NodeList != null && debuffAddon->UldManager.NodeListCount > 31)
                {
                    return debuffAddon;
                }
            }

            targetInfoBuffDebuff = Service.GameGui.GetAddonByName("_TargetInfo", 1);
            if (targetInfoBuffDebuff != IntPtr.Zero)
            {
                var targetAtk = (AtkUnitBase*)targetInfoBuffDebuff;
                if (targetAtk != null && targetAtk->IsVisible && targetAtk->UldManager.NodeList != null && targetAtk->UldManager.NodeListCount > 52)
                {
                    return targetAtk;
                }
            }
            return null;
        }

        protected virtual bool IsValidDotRefresh(uint actionId)
        {
            if (!StatusIconIds.ContainsKey(actionId))
            {
                return false;
            }

            if (targetManager.Target?.ObjectKind != Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc)
            {
                return false;
            }

            var targetDebuffAddon = FindTargetDebuffAddon();
            if (targetDebuffAddon == null)
            {
                return false;
            }

            var appliedStatusesCount = GetPlayerAppliedStatusesCount();

            var targetStatusIconId = StatusIconIds[actionId];
            var endIndex = targetDebuffAddon->UldManager.NodeListCount > 32 ? 32 : 31;
            for (var i = 0; i < appliedStatusesCount; i++)
            {
                var idx = endIndex - i;
                var node = targetDebuffAddon->UldManager.NodeList[idx];
                var cmp = node->GetComponent();
                if (cmp == null || cmp->UldManager.NodeList == null)
                {
                    continue;
                }

                var imageNode = cmp->UldManager.NodeList[1];
                if (imageNode == null)
                {
                    continue;
                }

                var image = imageNode->GetAsAtkImageNode();
                if (image == null)
                {
                    continue;
                }
                var partsList = image->PartsList;
                if (partsList == null || partsList->PartCount == 0)
                {
                    continue;
                }

                var resource = partsList->Parts[0].UldAsset->AtkTexture.Resource;

                if (resource == null || resource->IconId != targetStatusIconId)
                {
                    continue;
                }

                var resTextNode = cmp->UldManager.NodeList[2];
                if (resTextNode == null)
                {
                    return false;
                }

                var textNode = resTextNode->GetAsAtkTextNode();
                var color = textNode->TextColor;
                if (color.RGBA != PlayerAplliedStatusColor)
                {
                    return false;
                }

                var remainingDotTime = textNode->NodeText.ToString();
                if (uint.TryParse(remainingDotTime, out var value))
                {
                    return value > 0 && value < 4;
                }
            }
            return false;
        }


        public virtual float OnAction(uint actionId)
        {
            return 0;
        }

        public virtual float OnActionAppliedOnTarget(uint actionId)
        {
            return 0;
        }
    }
}
