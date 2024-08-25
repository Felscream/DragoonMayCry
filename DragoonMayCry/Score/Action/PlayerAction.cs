using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DragoonMayCry.Score.Action
{
    public enum PlayerActionType
    {
        Action = 0,
        CastStart = 1,
        CastCancel = 2,
        OffGCD = 3,
        AutoAttack = 4,
        LimitBreak = 5,
        Other = 6
    }
    public class PlayerAction
    {
        public uint Id { get; private set; }
        public PlayerActionType Type { get; private set; }
        public uint ComboId { get; private set; }
        public bool PreservesCombo { get; private set; }
        public float EndTime { get; private set; }
        public bool Canceled { get; set; }

        public PlayerAction(uint id, PlayerActionType type, uint comboId, bool preservesCombo, float endTime)
        {
            Id = id;
            Type = type;
            ComboId = comboId;
            PreservesCombo = preservesCombo;
            EndTime = endTime;
        }
    }
}
