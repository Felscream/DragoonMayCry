using System;

namespace DragoonMayCry.Score.Action.JobModule.Model
{
    internal class AppliedStatusIdentifier(uint actionId, uint statusId, uint iconId) : IEquatable<AppliedStatusIdentifier?>
    {
        public uint ActionId { get; private set; } = actionId;
        public uint StatusId { get; private set; } = statusId;
        public uint IconId { get; private set; } = iconId;

        public override bool Equals(object? obj)
        {
            return Equals(obj as AppliedStatusIdentifier);
        }

        public bool Equals(AppliedStatusIdentifier? other)
        {
            return other is not null && ActionId == other.ActionId;
        }

        public override int GetHashCode()
        {
            return (int)ActionId;
        }
    }
}
