using System.Linq;


using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Objects.Enums;

namespace DragoonMayCry {
    public class PlayerState {

        private ClientState ClientState { get; set; }
        private Condition Condition { get; set; }
        private PlayerCharacter Player => this.ClientState?.LocalPlayer;
        private Data<ushort> Territory { get; } = new();
        private Data<bool> IsDead { get; } = new();
        public bool Died() => !IsDead.Last && IsDead.Current;
        private PartyList PartyList { get; set; }
       
        private bool CheckCondition(ConditionFlag[] conditionFlags) => (this.Condition != null) && conditionFlags.Any(x => this.Condition[x]);
        private class Data<T> where T : struct {
            public T Current { get; private set; }

            public T Last { get; private set; }

            public void Update(T data) => this.Current = data;

            public void SaveData() => this.Last = this.Current;
        }

        public void ServicesUpdate(ClientState clientState, PartyList partyList, Condition condition) {
            this.ClientState = clientState;
            this.PartyList = partyList;
            this.Condition = condition;
        }

        public void StateUpdate() {
            this.Territory.Update(this.ClientState.TerritoryType);
            this.IsDead.Update(this.Player.CurrentHp == 0);
        }

        public void SaveData() {
            this.IsDead.SaveData();
        }

        public bool IsInCombat() { 
            var inCombat = this.CheckCondition(new[] { ConditionFlag.InCombat });
            if (!inCombat) {
                foreach (var actor in PartyList) {
                    if (actor.GameObject is not Character character || (character.StatusFlags & StatusFlags.InCombat) == 0)
                        continue;
                    return true;
                }
            }
            return inCombat;
        }

        public bool IsValidState() {
            var onZoneChange = (this.Territory.Last != this.Territory.Current);
            return !onZoneChange && !Died();
        }
    }
}
