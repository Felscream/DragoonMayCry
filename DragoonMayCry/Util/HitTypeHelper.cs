#region

using Dalamud.Game.Gui.FlyText;

#endregion

namespace DragoonMayCry.Util
{
    public static class HitTypeHelper
    {
        public static FlyTextKind GetHitType(int hitType)
        {
            return hitType switch
            {
                448 => FlyTextKind.DamageDh,
                451 => FlyTextKind.DamageCritDh,
                510 => FlyTextKind.Damage,
                511 => FlyTextKind.DamageCrit,
                519 => FlyTextKind.Healing,
                521 => FlyTextKind.MpRegen,
                526 => FlyTextKind.Buff,
                _ => FlyTextKind.AutoAttackOrDot,
            };
        }
    }
}
