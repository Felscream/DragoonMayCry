#region

using FFXIVClientStructs.FFXIV.Client.Game;
using System.Runtime.InteropServices;

#endregion

namespace DragoonMayCry.State
{
    [StructLayout(LayoutKind.Explicit)]
    public struct ActionManagerLight
    {
        [FieldOffset(0x8)] public float animationLock;
        [FieldOffset(0x28)] public bool isCasting;
        [FieldOffset(0x30)] public float elapsedCastTime;
        [FieldOffset(0x34)] public float castTime;
        [FieldOffset(0x60)] public ComboDetail Combo;
        [FieldOffset(0x68)] public bool isQueued;
        [FieldOffset(0x120)] public ushort currentSequence;
        [FieldOffset(0x5F8)] public bool isGCDRecastActive;
        [FieldOffset(0x5FC)] public uint currentGCDAction;
        [FieldOffset(0x600)] public float elapsedGCDRecastTime;
        [FieldOffset(0x604)] public float gcdRecastTime;

    }
}
