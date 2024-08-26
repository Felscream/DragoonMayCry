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
            float sfxCooldown = Plugin.Configuration!.AnnouncerCooldown;
            if (LastPlayedSfxTime > 0 && LastPlayedSfxTime + sfxCooldown > time)
            {
                return;
            }

            LastPlayedSfxTime = (float)time;
            AudioEngine.PlaySfx(key, SfxPaths[key], GetSfxVolume());
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
