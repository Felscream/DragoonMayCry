using DragoonMayCry.Score.Style;
using ImGuiNET;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace DragoonMayCry.Audio
{
    public class AudioService
    {
        
        private Dictionary<StyleType, string> SfxPaths =
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

        private float LastPlayedSfxTime;
        private readonly AudioEngine audioEngine;
        private static AudioService? _instance;
        public AudioService()
        {
            audioEngine = new AudioEngine(SfxPaths);

        }

        public static AudioService Instance()
        {
            if (_instance == null)
            {
                _instance = new AudioService();
            }

            return _instance;
        }

        public void PlaySfx(StyleType key)
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
            audioEngine.PlaySfx(key, GetGameSfxVolume());
        }

        private static string GetPathToAudio(string name)
        {
            return Path.Combine(
                Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!,
                $"Assets\\Audio\\{name}.wav");
        }

        public static float GetGameSfxVolume()
        {
            if (Service.GameConfig.System.GetBool("IsSndSe") ||
                Service.GameConfig.System.GetBool("IsSndMaster"))
            {
                return 0;
            }
            return Service.GameConfig.System.GetUInt("SoundSe") / 100f * (Service.GameConfig.System.GetUInt("SoundMaster") / 100f);
        }
    }
}
