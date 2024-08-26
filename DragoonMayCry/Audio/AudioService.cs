using DragoonMayCry.Score.Style;
using ImGuiNET;
using System.Collections.Generic;
using System.IO;

namespace DragoonMayCry.Audio
{
    public static class AudioService
    {
        private static Dictionary<StyleType, string> SfxPaths =
            new Dictionary<StyleType, string>
            {
                { StyleType.DEAD_WEIGHT, GetPathToAudio("dead_weight") },
                { StyleType.D, GetPathToAudio("dirty") },
                { StyleType.C, GetPathToAudio("cruel") },
                { StyleType.B, GetPathToAudio("brutal") },
                { StyleType.A, GetPathToAudio("anarchic") },
                { StyleType.S, GetPathToAudio("savage") },
                { StyleType.SS, GetPathToAudio("sadistic") },
                { StyleType.SSS, GetPathToAudio("sensational") }
            };

        private static float LastPlayedSfxTime;

        public static void PlaySfx(StyleType key)
        {
            if (!SfxPaths.ContainsKey(key))
            {
                Service.Log.Debug($"No sfx for {key}");
            }

            double time = ImGui.GetTime();
            Service.Log.Debug($"{time}");
            float sfxCooldown = Plugin.Configuration!.AnnouncerCooldown;
            if (LastPlayedSfxTime > 0 && LastPlayedSfxTime + sfxCooldown > time)
            {
                return;
            }

            LastPlayedSfxTime = (float)time;
            AudioEngine.PlaySfx(key, SfxPaths[key]);
        }

        private static string GetPathToAudio(string name)
        {
            return Path.Combine(
                Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!,
                $"Assets\\Audio\\{name}.wav");
        }
    }
}
