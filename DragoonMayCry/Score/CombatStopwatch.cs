using Dalamud.Plugin.Services;
using DragoonMayCry.State;
using System.Diagnostics;

namespace DragoonMayCry.Score
{
    public class CombatStopwatch
    {
        public double TimeInCombat() => stopwatch.Elapsed.TotalSeconds;
        
        private readonly Stopwatch stopwatch;
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
            stopwatch.Restart();
        }

        private void Stop()
        {
            stopwatch.Stop();
        }
        private void OnCombat(object? sender, bool enteredCombat)
        {
            if (enteredCombat)
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
