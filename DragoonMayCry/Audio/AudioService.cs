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
    public class AudioService
    {
        
        private static readonly Dictionary<SoundId, string> SfxPaths =
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

        private readonly Dictionary<SoundId, int> soundIdsNextAvailability;
        private readonly AudioEngine audioEngine;

        public AudioService()
        {
            soundIdsNextAvailability = new();
            audioEngine = new AudioEngine();
        }

        public void PlaySfx(SoundId key, bool force = false)
        {
            if (!SfxPaths.ContainsKey(key))
            {
                return;
            }
            
            if (!force && !CanPlaySfx(key))
            {
                soundIdsNextAvailability[key] -= 1;
                return;
            }

            Service.Log.Debug($"Playing SFX {key}");
            audioEngine.PlaySfx(key, SfxPaths[key], GetSfxVolume());

            if (!soundIdsNextAvailability.ContainsKey(key) || force || soundIdsNextAvailability[key] == 0)
            {
                soundIdsNextAvailability[key] = Plugin.Configuration!.PlaySfxEveryOccurrences - 1;
            }
        }

        public void PlaySfx(StyleType type, bool force = false)
        {
            PlaySfx(StyleTypeToSoundId(type), force);
        }

        public void ResetSfxPlayCounter()
        {
            foreach (var entry in soundIdsNextAvailability)
            {
                soundIdsNextAvailability[entry.Key] = 0;
            }
        }

        private bool CanPlaySfx(SoundId type)
        {
            return !soundIdsNextAvailability.ContainsKey(type) ||
                   soundIdsNextAvailability[type] <= 0;
        }

        private static string GetPathToAudio(string name)
        {
            return Path.Combine(
                Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!,
                $"Assets\\Audio\\{name}.wav");
        }

        private float GetSfxVolume()
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

        private SoundId StyleTypeToSoundId(StyleType type)
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
