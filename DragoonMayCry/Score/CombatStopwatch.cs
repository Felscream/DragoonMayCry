using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using ImGuiNET;

namespace DragoonMayCry.Score
{
    public class CombatStopwatch : IDisposable
    {
        public double TimeInCombat
        {
            get { return _timeInCombat; }
        }
        private double _timeInCombat;
        private double _combatTimeStart;
        private double _combatTimeEnd;
        private readonly IFramework framework = Service.Framework;

        public void Start()
        {
            _combatTimeStart = ImGui.GetTime();
            framework.Update += UpdateTime;
        }

        public void Stop()
        {
            _combatTimeEnd = ImGui.GetTime();
            framework.Update -= UpdateTime;
        }

        private void UpdateTime(IFramework framewok)
        {
            _timeInCombat = ImGui.GetTime() - _combatTimeStart;
        }

        public void Dispose()
        {
            framework.Update -= UpdateTime;
        }
    }
}
