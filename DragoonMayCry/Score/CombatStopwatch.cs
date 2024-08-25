using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using DragoonMayCry.State;
using FFXIVClientStructs;
using ImGuiNET;

namespace DragoonMayCry.Score
{
    public class CombatStopwatch
    {
        public double TimeInCombat() => Math.Floor((double)stopwatch.ElapsedMilliseconds / 1000);
        
        private Stopwatch stopwatch;
        private readonly IFramework framework = Service.Framework;
        private static CombatStopwatch _instance;

        private CombatStopwatch()
        {
            stopwatch = new Stopwatch();
            PlayerState.Instance().RegisterCombatStateChangeHandler(OnCombat);
        }

        public static CombatStopwatch Instance()
        {
            if (_instance == null)
            {
                _instance = new();
            }

            return _instance;
        }
        private void Start()
        {
            stopwatch.Reset();
            stopwatch.Start();
        }

        private void Stop()
        {
            stopwatch.Stop();
        }
        private void OnCombat(object? sender, bool inCombat)
        {
            if (inCombat)
            {
                Start();
            }
            else
            {
                Stop();
            }
        }
    }
}
