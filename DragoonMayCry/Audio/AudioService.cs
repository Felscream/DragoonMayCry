using Dalamud.Game.Config;
using DragoonMayCry.Audio.BGM;
using DragoonMayCry.Score.Model;
using ImGuiNET;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

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

        private static readonly HashSet<string> SfxGameSettings = new HashSet<string>
        {
            "IsSndSe", "IsSndMaster", "SoundSe", "SoundMaster"
        };
        private static readonly HashSet<string> BgmGameSettings = new HashSet<string>
        {
            "IsSndMaster", "SoundMaster"
        };

        
        // to alternate between dead weight sfx
        private readonly AudioEngine audioEngine;
        private static AudioService? instance;
        private AudioService()
        {
            audioEngine = new AudioEngine();
            audioEngine.UpdateSfxVolume(GetSfxVolume());
            audioEngine.UpdateBgmVolume(GetBgmVolume());
            Service.GameConfig.SystemChanged += OnSystemChange;
        }

        public void PlaySfx(SoundId key)
        {
            if (!Plugin.Configuration!.PlaySoundEffects)
            {
                return;
            }
           
            audioEngine.PlaySfx(key);
        }

        public void PlaySfx(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                Service.Log.Error($"Could not find audio file: [{path}]");
                return;
            }
            audioEngine.PlaySfx(path);
        }

        public void OnSfxVolumeChange(object? sender, int volume)
        {
            audioEngine.UpdateSfxVolume(GetSfxVolume());
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

        public void RegisterAnnouncerSfx(Dictionary<SoundId, string> sfx)
        {
            audioEngine.ClearBgmCache();
            audioEngine.RegisterAnnouncerSfx(sfx);
        }

        public void ClearBgmCache()
        {
            audioEngine.ClearBgmCache();
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

        private void OnSystemChange(object? sender, ConfigChangeEvent e)
        {
            var configOption = e.Option.ToString();
            if (SfxGameSettings.Contains(configOption)){
                audioEngine.UpdateSfxVolume(GetSfxVolume());
            }
            
            if (BgmGameSettings.Contains(configOption))
            {
                audioEngine.UpdateBgmVolume(GetBgmVolume());
            }
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
    }
}
