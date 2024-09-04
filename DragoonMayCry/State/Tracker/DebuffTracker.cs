using Dalamud.Game.ClientState.Statuses;
using System.Collections.Generic;

namespace DragoonMayCry.State.Tracker
{
    internal class DebuffTracker : StateTracker<bool>
    {
        private readonly HashSet<uint> damageDownIds = new HashSet<uint>
        {
            215, 628, 696, 1016, 1090, 2092, 2404, 2522, 2911, 3166, 3304, 3964
        };

        private readonly HashSet<uint> sustainedDamageIds = new HashSet<uint>
        {
            2935, // found on Valigarmanda, M1S, P9S, P10S
            3692, // P10S poison
            4149, // M3S
        };

        private readonly HashSet<uint> vulnerabilityUpIds = new HashSet<uint>
        {
            1789, // found on Valigarmanda, Zoraal Ja
        };

        private readonly Dictionary<ushort, ISet<uint>> debuffBlackListPerInstance = new Dictionary<ushort, ISet<uint>>{
            { 937, new HashSet<uint> {2935} }, // Sustained damage on P9S is applied after a TB
            { 939, new HashSet<uint> {2935} } // Failed tower soak on P10S, may not be the player's fault
        };


        private bool hasDamageDown;
        
        public override void Update(PlayerState playerState)
        {
            var player = playerState.Player;
            if (player == null || playerState.IsDead)
            {
                return;
            }
            
            StatusList statuses = player.StatusList;
            for(int i = 0; i < statuses.Length; i++)
            {
                var status = statuses[i];
                if (StatusIndicatesFailedMechanic(status))
                {
                    if (!hasDamageDown)
                    {
                        Service.Log.Debug($"Damage down applied");
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
            if(status == null)
            {
                return false;
            }

            uint id = status.GameData.RowId;
            ushort territory = Service.ClientState.TerritoryType;
            if (debuffBlackListPerInstance.ContainsKey(territory) && debuffBlackListPerInstance[territory].Contains(id))
            {
                return false;
            }

            
            return damageDownIds.Contains(id) || sustainedDamageIds.Contains(id) || vulnerabilityUpIds.Contains(id);
        }
    }
}
