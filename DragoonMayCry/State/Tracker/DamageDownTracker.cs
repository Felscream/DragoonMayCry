using Dalamud.Game.ClientState.Statuses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DragoonMayCry.State.Tracker
{
    internal class DamageDownTracker : StateTracker<bool>
    {
        private readonly HashSet<uint> damageDownIds = new HashSet<uint>
        {
            62, 215, 628, 696, 1016, 1090, 2092, 2404, 2522, 2911, 3166, 3304, 3964
        };
        private bool hasDamageDown;
        
        public override void Update(PlayerState playerState)
        {
            if(playerState.Player == null || playerState.IsDead)
            {
                return;
            }
            StatusList statuses = playerState.Player.StatusList;
            for(int i = 0; i < statuses.Length; i++)
            {
                var status = statuses[i];
                if (damageDownIds.Contains(status.GameData.RowId))
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
    }
}
