using Dalamud.Game.ClientState.Statuses;
using DragoonMayCry.Data;
using System.Collections.Generic;
namespace DragoonMayCry.State.Tracker
{
    internal class DebuffTracker : StateTracker<bool>
    {
        private readonly Dictionary<ushort, ISet<uint>> debuffInstanceBlacklist = new()
        {
            { 937, new HashSet<uint> {2935} }, // Sustained damage on P9S is applied after a TB
            { 939, new HashSet<uint> {2935} }, // Failed tower soak on P10S, may not be the player's fault
            { 63, new HashSet<uint> {202} }, // Ifrit ex
        };

        private bool hasDamageDown;

        public override void Update(PlayerState playerState)
        {
            if (!playerState.IsInCombat)
            {
                return;
            }
            var player = playerState.Player;
            if (player == null || playerState.IsDead)
            {
                return;
            }

            var statuses = player.StatusList;

            for (var i = 0; i < statuses.Length; i++)
            {

                var status = statuses[i];
                if (StatusIndicatesFailedMechanic(status))
                {
                    if (!hasDamageDown)
                    {
                        hasDamageDown = true;
                        OnChange?.Invoke(this, hasDamageDown);
                    }
                    return;
                }
            }

            // Player had afflications last update cycle.
            // If we get here, then they expired
            if (hasDamageDown)
            {
                hasDamageDown = false;
                OnChange?.Invoke(this, hasDamageDown);
            }
        }

        private bool StatusIndicatesFailedMechanic(Status? status)
        {
            if (status == null || status.StatusId == 0)
            {
                return false;
            }

            var territory = Service.ClientState.TerritoryType;
            if (debuffInstanceBlacklist.ContainsKey(territory) && debuffInstanceBlacklist[territory].Contains(status.StatusId))
            {
                return false;
            }


            return DebuffIds.DamageDownIds.Contains(status.StatusId) || DebuffIds.SustainedDamageIds.Contains(status.StatusId) || DebuffIds.VulnerabilityUpIds.Contains(status.StatusId);
        }
    }
}
