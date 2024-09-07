using System;
using ImGuiNET;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Dalamud.Plugin.Ipc.Exceptions;
using DragoonMayCry.Score.Model;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using DragoonMayCry.Audio.FSM;

namespace DragoonMayCry.Audio
{
    public class AudioService : IDisposable
    {
        public static AudioService Instance { get {
                if(instance == null)
                {
                    instance = new AudioService();
                }
                return instance;
            } 
        }

        private static readonly Dictionary<SoundId, string> DmcAnnouncer =
            new Dictionary<SoundId, string>
            {
                { SoundId.DeadWeight, GetPathToAnnouncerAudio("dead_weight.wav") },
                { SoundId.Dirty, GetPathToAnnouncerAudio("DmC/dirty.wav") },
                { SoundId.Cruel, GetPathToAnnouncerAudio("DmC/cruel.wav") },
                { SoundId.Brutal, GetPathToAnnouncerAudio("DmC/brutal.wav") },
                { SoundId.Anarchic, GetPathToAnnouncerAudio("DmC/anarchic.wav") },
                { SoundId.Savage, GetPathToAnnouncerAudio("DmC/savage.wav") },
                { SoundId.Sadistic, GetPathToAnnouncerAudio("DmC/sadistic.wav") },
                { SoundId.Sensational, GetPathToAnnouncerAudio("DmC/sensational.wav") }
            };

        private readonly Dictionary<SoundId, int> soundIdsNextAvailability;
        private readonly AudioEngine audioEngine;
        private readonly float sfxCooldown = 1f;
        private double lastPlayTime = 0f;
        private static AudioService? instance;
        private AudioService()
        {
            soundIdsNextAvailability = new();
            audioEngine = new AudioEngine();
            audioEngine.UpdateSfxVolume(GetSfxVolume());
            AssetsManager.AssetsReady += OnAssetsReady;
        }

        public void PlaySfx(SoundId key, bool force = false)
        {
            if (!Plugin.Configuration!.PlaySoundEffects || !DmcAnnouncer.ContainsKey(key))
            {
                return;
            }
            
            if (!force && !CanPlaySfx(key))
            {
                if (!soundIdsNextAvailability.ContainsKey(key))
                {
                    soundIdsNextAvailability.Add(key, 0);
                }
                else
                {
                    soundIdsNextAvailability[key]--;
                }
                return;
            }

            audioEngine.PlaySfx(key);
            lastPlayTime = ImGui.GetTime();
            if (!soundIdsNextAvailability.ContainsKey(key) || force || soundIdsNextAvailability[key] <= 0)
            {
                soundIdsNextAvailability[key] = Plugin.Configuration!.PlaySfxEveryOccurrences.Value - 1;
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

        public void OnSfxVolumeChange(object? sender, int volume)
        {
            audioEngine.UpdateSfxVolume(GetSfxVolume());
        }

        private void OnAssetsReady(object? sender, bool ready)
        {
            Service.Log.Debug("Assets loaded");
            if(!ready)
            {
                return;
            }

            audioEngine.RegisterSfx(DmcAnnouncer);
        }

        public void OnBgmVolumeChange(object? sender, int volume)
        {
            Service.Log.Debug("BGM Volume change");
            audioEngine.UpdateBgmVolume(GetBgmVolume());
        }

        public void RegisterBgmPart(BgmId id, string path)
        {
            Service.Log.Debug($"Registering bgm {id}");
            audioEngine.RegisterBgmPart(id, path);
        }

        private bool CanPlaySfx(SoundId type)
        {
            double lastPlayTimeDiff = ImGui.GetTime() - lastPlayTime;
            return lastPlayTimeDiff > sfxCooldown
                && (!soundIdsNextAvailability.ContainsKey(type) ||
                   soundIdsNextAvailability[type] <= 0);
        }

        private static string GetPathToAnnouncerAudio(string name)
        {
            return Path.Combine(AssetsManager.GetAssetsDirectory(), $"Audio\\Announcer\\{name}");
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
            return gameVolume * (Plugin.Configuration!.SfxVolume.Value / 100f);
        }

        private float GetBgmVolume()
        {
            if (Plugin.Configuration!.ApplyGameVolume && Service.GameConfig.System.GetBool("IsSndMaster"))
            {
                return 0;
            }

            var gameVolume = Plugin.Configuration!.ApplyGameVolume
                                 ? Service.GameConfig.System
                                          .GetUInt("SoundBgm") / 100f *
                                   (Service.GameConfig.System.GetUInt(
                                        "SoundMaster") / 100f)
                                 : 1;
            return gameVolume * (Plugin.Configuration!.BgmVolume.Value / 100f);
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
        public void PlayBgm(BgmId id)
        {
            audioEngine.PlayBgm(id);
        }

        public void StopBgm()
        {
            audioEngine.RemoveAllBgm();
        }
        public void Dispose()
        {
            audioEngine.Dispose();
        }

#if DEBUG   
        public void PlaySfx(SoundId id)
        {
            audioEngine.PlaySfx(id);
        }
#endif
    }
}
