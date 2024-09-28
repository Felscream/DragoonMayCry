using Dalamud.Game.Config;
using DragoonMayCry.Audio.BGM;
using DragoonMayCry.Audio.Engine;
using DragoonMayCry.Audio.StyleAnnouncer;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace DragoonMayCry.Audio
{
    public class AudioService : IDisposable
    {
        public static AudioService Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new AudioService();
                }
                return instance;
            }
        }

        private static readonly HashSet<string> SfxGameSettings = new()
        {
            "IsSndSe", "IsSndMaster", "SoundSe", "SoundMaster"
        };
        private static readonly HashSet<string> BgmGameSettings = new()
        {
            "IsSndMaster", "SoundMaster"
        };


        // to alternate between dead weight sfx
        private readonly AudioEngine audioEngine;
        private readonly Dictionary<DynamicBgmService.Bgm, Dictionary<BgmId, CachedSound>> registeredBgms = new();
        private static AudioService? instance;
        private bool deathEffectApplied;
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

        public bool RegisterBgmParts(DynamicBgmService.Bgm key, Dictionary<BgmId, string> paths)
        {
            if (registeredBgms.ContainsKey(key))
            {
                return true;
            }
            Dictionary<BgmId, CachedSound> bgm;
            try
            {
                bgm = audioEngine.RegisterBgm(paths);
            }
            catch (FileNotFoundException e)
            {
                Service.Log.Error(e, "An error occured while loading BGM");
                return false;
            }

            registeredBgms.Add(key, bgm);
            return true;
        }

        public void RegisterAnnouncerSfx(Dictionary<SoundId, string> sfx)
        {
            audioEngine.ClearSfxCache();
            audioEngine.RegisterAnnouncerSfx(sfx);
        }

        public bool LoadRegisteredBgm(DynamicBgmService.Bgm key)
        {
            if (registeredBgms.TryGetValue(key, out var value))
            {
                audioEngine.LoadBgm(value);
                return true;
            }
            return false;
        }

        private static float GetSfxVolume()
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
            return gameVolume * (Plugin.Configuration!.SfxVolume.Value / 100f);
        }

        private static float GetBgmVolume()
        {
            if (Plugin.Configuration!.ApplyGameVolumeBgm.Value && (Service.GameConfig.System.GetBool("IsSndMaster")))
            {
                return 0;
            }

            var gameVolume = Plugin.Configuration!.ApplyGameVolumeBgm.Value
                                 ? (Service.GameConfig.System.GetUInt("SoundMaster") / 100f)
                                 : 1;
            return gameVolume * (Plugin.Configuration!.BgmVolume.Value / 100f);
        }

        private void OnSystemChange(object? sender, ConfigChangeEvent e)
        {
            var configOption = e.Option.ToString();
            if (SfxGameSettings.Contains(configOption))
            {
                audioEngine.UpdateSfxVolume(GetSfxVolume());
            }

            if (BgmGameSettings.Contains(configOption))
            {
                audioEngine.UpdateBgmVolume(GetBgmVolume());
            }
        }

        public ISampleProvider? PlayBgm(BgmId id, double fadingDuration = 0, double fadeOutDelay = 0, double fadeOutDuration = 0)
        {
            return audioEngine.PlayBgm(id, fadingDuration, fadeOutDelay, fadeOutDuration);
        }

        public void RemoveBgmPart(ISampleProvider sample)
        {
            audioEngine.RemoveInput(sample);
        }

        public void StopBgm()
        {
            audioEngine.RemoveAllBgm();
        }

        public void FadeOutBgm(float fadeOutDuration = 0)
        {
            audioEngine.FadeOutBgm(fadeOutDuration);
        }

        public void Dispose()
        {
            audioEngine.Dispose();
        }

        public void ApplyDeathEffect()
        {
            if (!deathEffectApplied && Plugin.Configuration!.EnableMuffledEffectOnDeath)
            {
                audioEngine.ApplyDeathEffect();
                deathEffectApplied = true;
            }
        }

        public void RemoveDeathEffect()
        {
            if (deathEffectApplied)
            {
                audioEngine.RemoveDeathEffect();
                deathEffectApplied = false;
            }
        }

        [Conditional("DEBUG")]
        public void ApplyDecay(float decay)
        {
            audioEngine.ApplyDecay(decay);
            deathEffectApplied = true;
        }
    }
}
