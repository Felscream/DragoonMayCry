using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DragoonMayCry.Score.Action
{
    public enum PlayerActionType
    {
        Weaponskill = 0,
        Spell = 1,
        CastStart = 2,
        CastCancel = 3,
        OffGCD = 4,
        AutoAttack = 5,
        LimitBreak = 6,
        Other = 7
    }
    public class PlayerAction
    {
        public uint Id { get; private set; }
        public PlayerActionType Type { get; private set; }
        public uint? ComboId { get; private set; }
        public bool PreservesCombo { get; private set; }
        public float StartTime { get; private set; }
        public float Duration { get; private set; }

        public PlayerAction(uint id, PlayerActionType type, uint? comboId, bool preservesCombo, float startTime, float duration)
        {
            Id = id;
            Type = type;
            ComboId = comboId;
            PreservesCombo = preservesCombo;
            StartTime = startTime;
            Duration = duration;
        }
    }
}
