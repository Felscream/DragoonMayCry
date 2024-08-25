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
        public float TimeInCombat() => (float)stopwatch.ElapsedMilliseconds / 1000;
        
        private readonly Stopwatch stopwatch;
        private readonly IFramework framework = Service.Framework;
        private static CombatStopwatch? Instance;

        private CombatStopwatch()
        {
            stopwatch = new Stopwatch();
            PlayerState.GetInstance().RegisterCombatStateChangeHandler(OnCombat);
        }

        public static CombatStopwatch GetInstance()
        {
            if (Instance == null)
            {
                Instance = new();
            }

            return Instance;
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
