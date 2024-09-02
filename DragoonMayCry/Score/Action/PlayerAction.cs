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
}
