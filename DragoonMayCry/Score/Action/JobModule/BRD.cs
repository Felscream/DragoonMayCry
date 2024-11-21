using Dalamud.Game.ClientState.Objects.Types;
using System.Collections.Generic;

namespace DragoonMayCry.Score.Action.JobModule
{
    internal unsafe class BRD : DotJob
    {
        protected override Dictionary<uint, uint> ActionToStatusIds
        {
            get { return bardDotIds; }
        }

        private readonly Dictionary<uint, uint> bardDotIds = new()
        {
            { 100, 124 },   // Venomous Bite
            { 113, 129 },   // Windbite
            { 7406, 1200 }, // Caustic Bite
            { 7407, 1201 }, // Stormbite
        };

        private readonly HashSet<uint> dotIds;
        private readonly List<uint> bardBuffs = [125, 141, 2722, 2964]; // Raging Strikes, Battle Voice, Radiant Finale
        private readonly uint ironJawsId = 3560;

        private readonly ScoreManager scoreManager;

        public BRD(ScoreManager scoreManager) : base()
        {
            this.scoreManager = scoreManager;
            dotIds = [.. bardDotIds.Values];
        }

        public override float OnAction(uint actionId)
        {
            if (actionId == ironJawsId)
            {
                var buffCount = GetBuffCount();
                if (IsValidIronJawsUsage(buffCount))
                {
                    var currentRankThreshold = scoreManager.CurrentScoreRank.StyleScoring.Threshold;
                    return (0.2f + buffCount * 0.07f) * currentRankThreshold;
                }
            }

            return 0;
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

        private bool IsValidIronJawsUsage(int buffCount)
        {
            if (targetManager.Target?.ObjectKind != Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc)
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

            var fadingDotsCount = 0;
            var burstValidDotRefreshCount = 0;
            for (var i = 0; i < statuses.Length; i++)
            {
                var status = statuses[i];
                if (status != null && status.SourceId == playerId && dotIds.Contains(status.StatusId))
                {
                    if (status.RemainingTime is > 0 and <= 6)
                    {
                        fadingDotsCount++;
                        burstValidDotRefreshCount++;
                    }
                    else if (status.RemainingTime < 31)
                    {
                        burstValidDotRefreshCount++;
                    }
                }

                if (fadingDotsCount >= 2)
                {
                    return true;
                }

                if (burstValidDotRefreshCount >= 2 && buffCount >= 1)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
