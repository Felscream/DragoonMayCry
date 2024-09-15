using Dalamud.Interface.ImGuiNotification;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DragoonMayCry
{
    public static class AssetsManager
    {
        public enum AssetsStatus
        {
            Uninitialized,
            Updating,
            Done,
            FailedFileIntegrity,
            FailedDownloading,
            FailedInsufficientDiskSpace
        }

        public static EventHandler<bool>? AssetsReady;
        public static bool IsReady { get; private set; }
        public static AssetsStatus Status { get; private set; } = AssetsStatus.Uninitialized;

        private const string TargetAssetVersion = "0.12.0.0";
        private const string TargetSha1 = "fe381bb7cfdcb5012d55e4acfd0944e762ce7295";
        private const long RequiredDiskSpaceCompressed = 42_303_488;
        private const long RequiredDiskSpaceExtracted = 42_938_368;

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

        public static void FetchAudioFiles()
        {
            if(Status == AssetsStatus.Updating ) {
                return;
            }

            var configDir = Plugin.PluginInterface.GetPluginConfigDirectory(); ;
            var localAssetDir = GetAssetsDirectory();
            var areFilesValid = false;
            if (Directory.Exists(localAssetDir))
            {
                areFilesValid = AreLocalFilesValid() && TargetAssetVersion == CurrentDownloadedAssetVersion();
            }
            
            if (areFilesValid)
            {
                Status = AssetsStatus.Done;
                SendAssetsReadyEvent();
                return;
            }

            LogAndNotify("Downloading assets", NotificationType.Info);
            Status = AssetsStatus.Updating;
            // Clear folder if it exists
            if (Directory.Exists(localAssetDir))
            {
                Directory.Delete(localAssetDir, true);
            }

            Uri assetsUri = new Uri($"https://github.com/Felscream/DragoonMayCry/releases/download/v{TargetAssetVersion}/assets.zip");
            var downloadLocation = $"{configDir}/assets-{TargetAssetVersion}.zip";
            var requiredSpace = RequiredDiskSpaceExtracted + RequiredDiskSpaceCompressed;
            var freeDiskSpace = new DriveInfo(configDir).AvailableFreeSpace;

            if(freeDiskSpace < requiredSpace)
            {
                LogAndNotify("Not enough free disk space to extract assets", NotificationType.Error);
                Status = AssetsStatus.FailedInsufficientDiskSpace;
                return;
            }

            HttpClient httpClient = new();
            HttpResponseMessage response = httpClient.GetAsync(assetsUri).Result;

            if (!response.IsSuccessStatusCode)
            {
                LogAndNotify($"Unable to download assets: {response.StatusCode} - {response.Content}", NotificationType.Error);
                Status = AssetsStatus.FailedDownloading;
                return;
            }

            using (FileStream fs = new(downloadLocation, FileMode.CreateNew))
            {
                response.Content.CopyToAsync(fs).Wait();
            }

            LogAndNotify("Extracting assets", NotificationType.Info);

            ZipFile.ExtractToDirectory(downloadLocation, localAssetDir);
            File.Delete(downloadLocation);

            LogAndNotify("Assets extraction complete", NotificationType.Success);
            // Validate the downloaded assets
            if (!AreLocalFilesValid())
            {
                LogAndNotify("File integrity check failed", NotificationType.Error);
                Status = AssetsStatus.FailedFileIntegrity;
                return;
            }
            Status = AssetsStatus.Done;
            SendAssetsReadyEvent();
        }
       
        private static void SendAssetsReadyEvent()
        {
            IsReady = true;
            AssetsReady?.Invoke(null, true);
        }

        private static bool AreLocalFilesValid()
        {
            var assetDirectory = GetAssetsDirectory();
            var localAssetsSha1 = GetAssetsSha1(assetDirectory);
#if DEBUG
            if(localAssetsSha1 != TargetSha1)
            {
                Service.Log.Warning($"Update assets sha1 before commiting, current sha1 {localAssetsSha1} vs target {TargetSha1}");
            }
#endif
            return localAssetsSha1 == TargetSha1;
        }

        private static String GetAssetsSha1(string folder)
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

        public static string GetAssetsDirectory()
        {
            var configDir = Plugin.PluginInterface.GetPluginConfigDirectory();
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

        private static string? CurrentDownloadedAssetVersion()
        {
            var assetsDir = GetAssetsDirectory();
            var manifestFile = $"{assetsDir}/manifest.json";

            if (!File.Exists(manifestFile)) return null;

            var jsonData = File.ReadAllText(manifestFile);
            AssetsManifest? manifest = JsonConvert.DeserializeObject<AssetsManifest>(jsonData);

            return manifest?.Version;
        }
    }

    

    class AssetsManifest
    {
        [JsonProperty("version")]
        public string? Version { get; set; }
    }
}
