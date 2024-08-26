using System;
using DragoonMayCry.Score.Style;
using ImGuiNET;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Dalamud.Plugin.Ipc.Exceptions;

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

        private static double LastPlayedSfxTime;
        private static double LastPlayedDeadWeight;
        private static float DeadWeightCooldown = 16f;

        public static void PlaySfx(StyleType key, bool force = false)
        {
            if (!SfxPaths.ContainsKey(key))
            {
                Service.Log.Debug($"No sfx for {key}");
            }
            
            if (!force && !CanPlaySfx(key))
            {
                return;
            }

            LastPlayedSfxTime = ImGui.GetTime();
            if (key == StyleType.DEAD_WEIGHT)
            {
                LastPlayedDeadWeight = ImGui.GetTime();
            }
            AudioEngine.PlaySfx(key, SfxPaths[key], GetSfxVolume());
        }

        private static bool CanPlaySfx(StyleType type)
        {
            float sfxCooldown = Plugin.Configuration!.AnnouncerCooldown;
            double time = ImGui.GetTime();
            if (type == StyleType.DEAD_WEIGHT)
            {
                var delay = Math.Max(DeadWeightCooldown, sfxCooldown);
                return LastPlayedDeadWeight + delay < time;
            }
            return LastPlayedSfxTime + sfxCooldown < time;
        }

        private static string GetPathToAudio(string name)
        {
            return Path.Combine(
                Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!,
                $"Assets\\Audio\\{name}.wav");
        }

        private static float GetSfxVolume()
        {
            if (Plugin.Configuration!.ApplyGameVolume && (Service.GameConfig.System.GetBool("IsSndSe") ||
                            Service.GameConfig.System.GetBool("IsSndMaster")))
            {
                return 0;
            }

            var gameVolume = Plugin.Configuration!.ApplyGameVolume
                                 ? Service.GameConfig.System
                                          .GetUInt("SoundSe") / 100f *
                                   (Service.GameConfig.System.GetUInt(
                                        "SoundMaster") / 100f)
                                 : 1;
            return gameVolume * (Plugin.Configuration!.SfxVolume / 100f);
        }
    }
}
