using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using Dalamud.Game.Gui.FlyText;

namespace DragoonMayCry.Style {
    internal class Structures {

        public enum ActionType : byte {
            Nothing = 0,
            Miss = 1,
            FullResist = 2,
            Damage = 3,
            Heal = 4,
            BlockedDamage = 5,
            ParriedDamage = 6,
            Invulnerable = 7,
            NoEffectText = 8,
            Unknown_0 = 9,
            MpLoss = 10,
            MpGain = 11,
            TpLoss = 12,
            TpGain = 13,
            GpGain = 14,
            ApplyStatusEffectTarget = 15,
            ApplyStatusEffectSource = 16,
            StatusNoEffect = 20,
            StartActionCombo = 27,
            ComboSucceed = 28,
            Knockback = 33,
            Mount = 40,
            VFX = 59,
        };

        public enum DamageType {
            Unknown = 0,
            Slashing = 1,
            Piercing = 2,
            Blunt = 3,
            Magic = 5,
            Darkness = 6,
            Physical = 7,
            LimitBreak = 8,
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct EffectHeader {
            [FieldOffset(8)] public uint ActionId;
            [FieldOffset(28)] public ushort AnimationId;
            [FieldOffset(33)] public byte TargetCount;
        }

        public struct EffectEntry {
            public ActionType type;
            public byte param0;
            public byte param1;
            public byte param2;
            public byte mult;
            public byte flags;
            public ushort value;

            public override string ToString() {
                return
                    $"Type: {type}, p0: {param0}, p1: {param1}, p2: {param2}, mult: {mult}, flags: {flags} | {Convert.ToString(flags, 2)}, value: {value}";
            }
        }

        public struct ActionEffectInfo {

            public uint actionId;
            public ActionType type;
            public uint sourceId;
            public uint targetCount;

            public override bool Equals(object o) {
                return
                    o is ActionEffectInfo other
                    && other.actionId == actionId
                    && other.sourceId == sourceId
                    && other.targetCount == targetCount;
            }

            public override int GetHashCode() {
                return HashCode.Combine(actionId, sourceId, targetCount);
            }

            public override string ToString() {
                return
                    $"actionId: {actionId} sourceId: {sourceId} (0x{sourceId:X}) targetCount: {targetCount}";
            }
        }
    }
}
