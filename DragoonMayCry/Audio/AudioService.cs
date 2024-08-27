using System;
using DragoonMayCry.Score.Style;
using ImGuiNET;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Dalamud.Plugin.Ipc.Exceptions;

namespace DragoonMayCry.Audio
{
    public static class AudioService
    {
        
        private static Dictionary<SoundId, string> SfxPaths =
            new Dictionary<SoundId, string>
            {
                { SoundId.DeadWeight, GetPathToAudio("dead_weight") },
                { SoundId.Dirty, GetPathToAudio("dirty") },
                { SoundId.Cruel, GetPathToAudio("cruel") },
                { SoundId.Brutal, GetPathToAudio("brutal") },
                { SoundId.Anarchic, GetPathToAudio("anarchic") },
                { SoundId.Savage, GetPathToAudio("savage") },
                { SoundId.Sadistic, GetPathToAudio("sadistic") },
                { SoundId.Sensational, GetPathToAudio("sensational") }
            };

        private static double LastPlayedSfxTime;
        private static double LastPlayedDeadWeight;
        private static float DeadWeightCooldown = 16f;

        public static void PlaySfx(SoundId key, bool force = false)
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
            if (key == SoundId.DeadWeight)
            {
                LastPlayedDeadWeight = ImGui.GetTime();
            }
            AudioEngine.PlaySfx(key, SfxPaths[key], GetSfxVolume());
        }

        public static void PlaySfx(StyleType type, bool force = false)
        {
            PlaySfx(StyleTypeToSoundId(type), force);
        }

        private static bool CanPlaySfx(SoundId type)
        {
            float sfxCooldown = Plugin.Configuration!.AnnouncerCooldown;
            double time = ImGui.GetTime();
            if (type == SoundId.DeadWeight)
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

        private static SoundId StyleTypeToSoundId(StyleType type)
        {
            return type switch
            {
                StyleType.D => SoundId.Dirty,
                StyleType.C => SoundId.Cruel,
                StyleType.B => SoundId.Brutal,
                StyleType.A => SoundId.Anarchic,
                StyleType.S => SoundId.Savage,
                StyleType.SS => SoundId.Sadistic,
                StyleType.SSS => SoundId.Sensational,
                _ => SoundId.Unknown
            };
        }
    }
}
