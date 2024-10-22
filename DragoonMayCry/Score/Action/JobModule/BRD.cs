using System.Collections.Generic;
using System.Linq;

namespace DragoonMayCry.Score.Action.JobModule
{
    internal unsafe class BRD : DotJob
    {
        protected override Dictionary<uint, uint> StatusIconIds { get { return statusIconIds; } }

        private readonly Dictionary<uint, uint> statusIconIds = new()
        {
            { 100, 10352 }, // Venomous Bite
            { 113, 10360 }, // Windbite
            { 7406,  12616}, // Caustic Bite
            { 7407,  12617}, // Stormbite
        };

        private readonly List<uint> bardBuffs = [125, 141, 2722]; // Raging Strikes, Battle Voice, Radiant Finale
        private readonly uint ironJawsId = 3560;

        private readonly ScoreManager scoreManager;
        public BRD(ScoreManager scoreManager) : base()
        {
            this.scoreManager = scoreManager;
        }

        public override float OnAction(uint actionId)
        {
            if (actionId != ironJawsId)
            {
                return 0;
            }

            var buffCount = GetBuffCount();
            var currentRankThreshold = scoreManager.CurrentScoreRank.StyleScoring.Threshold;

            if (!IsValidIronJawsUsage())
            {
                return buffCount * 0.1f * currentRankThreshold;
            }


            return (0.3f + buffCount * 0.1f) * currentRankThreshold;
        }

        private int GetBuffCount()
        {
            var player = playerState.Player;
            if (player == null || playerState.IsDead)
            {
                return 0;
            }

            var statuses = player.StatusList;

            var buffCount = 0;
            for (var i = 0; i < statuses.Length; i++)
            {

                var status = statuses[i];
                if (status != null && bardBuffs.Contains(status.StatusId))
                {
                    buffCount++;
                }
                if (buffCount == bardBuffs.Count)
                {
                    return buffCount;
                }
            }
            return buffCount;
        }

        private bool IsValidIronJawsUsage()
        {
            if (targetManager.Target?.ObjectKind != Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc)
            {
                return false;
            }

            var appliedStatusesCount = GetPlayerAppliedStatusesCount();
            if (appliedStatusesCount < 2)
            {
                return false;
            }

            var targetDebuffAddon = FindTargetDebuffAddon();
            if (targetDebuffAddon == null)
            {
                return false;
            }

            var targetStatusIconIds = statusIconIds.Select(entry => entry.Value).ToHashSet();
            var validDotRefreshes = 0;
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

                if (resource == null || !targetStatusIconIds.Contains(resource->IconId))
                {
                    continue;
                }

                var resTextNode = cmp->UldManager.NodeList[2];
                if (resTextNode == null)
                {
                    continue;
                }

                var textNode = resTextNode->GetAsAtkTextNode();
                var color = textNode->TextColor;
                if (color.RGBA != PlayerAplliedStatusColor)
                {
                    continue;
                }

                var remainingDotTime = textNode->NodeText.ToString();
                if (uint.TryParse(remainingDotTime, out var value))
                {
                    if (value > 0 && value < 7)
                    {
                        validDotRefreshes++;
                    }
                }
                if (validDotRefreshes == 2)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
