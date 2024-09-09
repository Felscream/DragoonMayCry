using Dalamud.Interface.ImGuiNotification;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DragoonMayCry
{
    public static class AssetManager
    {
        enum State
        {
            Ready,
            Downloading,
            Done
        }

        private const string TargetAssetVersion = "0.7.5";
        private const string TargetSha1 = "164765419ff8c7ffa2f65fbe279050f460d14a04";

        private const string downloadUrl = "https://github.com/Felscream/DragoonMayCry/blob/download-assets/assets/Assets.zip";


        public static void FetchAudioFiles()
        {
            string localAssetDir = GetAssetsDirectory();
            Service.Log.Debug($"{localAssetDir}");
            var needToUpdateFiles = true;
            if (Directory.Exists(localAssetDir))
            {
                needToUpdateFiles = CheckLocalAssets();
            }


        }

        public static void VerifyAndUpdateAssets()
        {

            try
            {
                Task.Run(() => { FetchAudioFiles(); });
            } catch(Exception ex)
            {
                Service.Log.Error(ex, "An excepion occured while fetching audio files");
            }
        }

        private static bool CheckLocalAssets()
        {
            var assetDirectory = GetAssetsDirectory();
            var localAssetsSha1 = GetAssetSha1(assetDirectory);
#if DEBUG
            if(localAssetsSha1 != TargetSha1)
            {
                LogAndNotify($"Update assets sha1 before commiting, current sha1 {localAssetsSha1}", NotificationType.Warning);
            }
#endif
            return localAssetsSha1 == TargetSha1;
        }

        private static String GetAssetSha1(string folder)
        {
            using(SHA1 sha1 = SHA1.Create())
            {
                var files = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories)
                    .OrderBy(f => f)
                    .ToList();

                using (MemoryStream ms = new MemoryStream())
                {
                    foreach (var file in files)
                    {
                        byte[] content = File.ReadAllBytes(file);
                        byte[] hash = sha1.ComputeHash(content);

                        ms.Write(hash, 0, hash.Length);
                    }

                    byte[] folderHash = sha1.ComputeHash(ms.ToArray());
                    return BitConverter.ToString(folderHash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        private static string? GetCurrentManifestVersion()
        {
            return "";
        }

        public static string GetAssetsDirectory()
        {
            string configDir = Plugin.PluginInterface.GetPluginConfigDirectory();
            return $"{configDir}/assets";
        }

        private static void LogAndNotify(string message, NotificationType type)
        {
            switch (type)
            {
                case NotificationType.Success:
                case NotificationType.Info:
                    Service.Log.Info(message);
                    break;
                case NotificationType.Warning:
                    Service.Log.Warning(message);
                    break;
                case NotificationType.Error:
                    Service.Log.Error(message);
                    break;
                default:
                case NotificationType.None:
                    Service.Log.Debug(message);
                    break;
            }
            Notification notification = new()
            {
                Content = message,
                Type = type
            };

            Service.NotificationManager.AddNotification(notification);
        }
    }
}
