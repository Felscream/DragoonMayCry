using DragoonMayCry.Audio.BGM;
using DragoonMayCry.Score.Model;
using ImGuiNET;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;

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
                { SoundId.DeadWeight1, GetPathToAnnouncerAudio("dead_weight1.ogg") },
                { SoundId.DeadWeight2, GetPathToAnnouncerAudio("dead_weight2.ogg") },
                { SoundId.Dirty, GetPathToAnnouncerAudio("DmC/dirty.ogg") },
                { SoundId.Cruel, GetPathToAnnouncerAudio("DmC/cruel.ogg") },
                { SoundId.Brutal, GetPathToAnnouncerAudio("DmC/brutal.ogg") },
                { SoundId.Anarchic, GetPathToAnnouncerAudio("DmC/anarchic.ogg") },
                { SoundId.Savage, GetPathToAnnouncerAudio("DmC/savage.ogg") },
                { SoundId.Sadistic, GetPathToAnnouncerAudio("DmC/sadistic.ogg") },
                { SoundId.Sensational, GetPathToAnnouncerAudio("DmC/sensational.ogg") }
            };

        private readonly Dictionary<SoundId, int> soundIdsNextAvailability;
        // to alternate between dead weight sfx
        private readonly Queue<SoundId> deadWeightQueue;
        private readonly AudioEngine audioEngine;
        private readonly float sfxCooldown = 1f;
        private double lastPlayTime = 0f;
        private static AudioService? instance;
        private AudioService()
        {
            soundIdsNextAvailability = new();
            deadWeightQueue = new();
            deadWeightQueue.Enqueue(SoundId.DeadWeight1);
            deadWeightQueue.Enqueue(SoundId.DeadWeight2);
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

            var effectiveKey = key;
            if(key == SoundId.DeadWeight2)
            {
                effectiveKey = SoundId.DeadWeight1;
            }

            
            if (!force && !CanPlaySfx(effectiveKey))
            {
                if (!soundIdsNextAvailability.ContainsKey(effectiveKey))
                {
                    soundIdsNextAvailability.Add(effectiveKey, 0);
                }
                else
                {
                    soundIdsNextAvailability[effectiveKey]--;
                }
                return;
            }

            if(key == SoundId.DeadWeight1 || key == SoundId.DeadWeight2)
            {
                var toPlay = deadWeightQueue.Dequeue();
                deadWeightQueue.Enqueue(toPlay);
                audioEngine.PlaySfx(toPlay);
            } else
            {
                audioEngine.PlaySfx(key);
            }
            
            lastPlayTime = ImGui.GetTime();
            if (!soundIdsNextAvailability.ContainsKey(effectiveKey) || force || soundIdsNextAvailability[effectiveKey] <= 0)
            {
                soundIdsNextAvailability[effectiveKey] = Plugin.Configuration!.PlaySfxEveryOccurrences.Value - 1;
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
            if(!ready)
            {
                return;
            }

            audioEngine.RegisterSfx(DmcAnnouncer);
        }

        public void OnBgmVolumeChange(object? sender, int volume)
        {
            audioEngine.UpdateBgmVolume(GetBgmVolume());
        }

        public bool RegisterBgmParts(Dictionary<BgmId, string> paths)
        {
            foreach (KeyValuePair<BgmId, string> entry in paths)
            {
                if (!File.Exists(entry.Value))
                {
                    Service.Log.Warning($"File {entry.Value} does not exist");
                    return false;
                }
                audioEngine.RegisterBgmPart(entry.Key, entry.Value);
            }
            return true;
        }

        public void ClearBgmCache()
        {
            audioEngine.ClearBgmCache();
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
            if (Plugin.Configuration!.ApplyGameVolumeSfx && (Service.GameConfig.System.GetBool("IsSndSe") ||
                            Service.GameConfig.System.GetBool("IsSndMaster")))
            {
                return 0;
            }

            var gameVolume = Plugin.Configuration!.ApplyGameVolumeSfx
                                 ? Service.GameConfig.System
                                          .GetUInt("SoundSe") / 100f *
                                   (Service.GameConfig.System.GetUInt(
                                        "SoundMaster") / 100f)
                                 : 1;
            return Math.Clamp(gameVolume * (Plugin.Configuration!.SfxVolume.Value / 100f), 0, 1);
        }

        private float GetBgmVolume()
        {
            if (Plugin.Configuration!.ApplyGameVolumeBgm.Value && (Service.GameConfig.System.GetBool("IsSndMaster")))
            {
                return 0;
            }

            var gameVolume = Plugin.Configuration!.ApplyGameVolumeBgm.Value
                                 ? (Service.GameConfig.System.GetUInt("SoundMaster") / 100f)
                                 : 1;
            return Math.Clamp(gameVolume * (Plugin.Configuration!.BgmVolume.Value / 100f), 0, 1);
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
        public ISampleProvider? PlayBgm(BgmId id, double fadingDuration = 0)
        {
            return audioEngine.PlayBgm(id, fadingDuration);
        }

        public void RemoveBgmPart(ISampleProvider sample)
        {
            audioEngine.RemoveInput(sample);
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
            if (id == SoundId.DeadWeight1 || id == SoundId.DeadWeight2)
            {
                var toPlay = deadWeightQueue.Dequeue();
                deadWeightQueue.Enqueue(toPlay);
                audioEngine.PlaySfx(toPlay);
            } else
            {
                audioEngine.PlaySfx(id);
            }
        }
#endif
    }
}
