using Dalamud.Game.ClientState.Statuses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DragoonMayCry.State.Tracker
{
    internal class DebuffTracker : StateTracker<bool>
    {
        private readonly HashSet<uint> damageDownIds = new HashSet<uint>
        {
            62, 215, 628, 696, 1016, 1090, 2092, 2404, 2522, 2911, 3166, 3304, 3964
        };

        private readonly HashSet<uint> sustainedDamageIds = new HashSet<uint>
        {
            2935, // found on Valigarmanda, M1S
            4149, // M3S
        };

        private readonly HashSet<uint> vulnerabilityUpIds = new HashSet<uint>
        {
            1789, // found on Valigarmanda, Zoraal Ja
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

            // Player had damage down last update cycle.
            // If we get here, then damage down expired
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
            return damageDownIds.Contains(id) || sustainedDamageIds.Contains(id) || vulnerabilityUpIds.Contains(id);
        }
    }
}
