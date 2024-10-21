using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using DragoonMayCry.State;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;

namespace DragoonMayCry.Score.Action.JobModule
{
    internal abstract unsafe class DotJob : IJobActionModule
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
            var playerId = playerState.Player?.EntityId;
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

            var debuffAddon = Service.GameGui.GetAddonByName("_TargetInfoBuffDebuff", 1);
            if (debuffAddon == IntPtr.Zero)
            {
                return false;
            }


            var appliedStatusesCount = GetPlayerAppliedStatusesCount();
            var targetDebuffAddon = (AtkUnitBase*)debuffAddon;

            var targetStatusIconId = StatusIconIds[actionId];

            for (var i = 0; i < appliedStatusesCount; i++)
            {
                var idx = targetDebuffAddon->UldManager.NodeListCount - i - 1;
                var node = targetDebuffAddon->UldManager.NodeList[idx];
                var cmp = node->GetComponent();
                if (cmp == null || cmp->UldManager.NodeList == null)
                {
                    return false;
                }

                var imageNode = cmp->UldManager.NodeList[1];
                if (imageNode == null)
                {
                    return false;
                }

                var image = imageNode->GetAsAtkImageNode();
                if (image == null)
                {
                    return false;
                }
                var partsList = image->PartsList;
                if (partsList == null || partsList->PartCount == 0)
                {
                    return false;
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


        public abstract float OnAction(uint actionId);
    }
}
