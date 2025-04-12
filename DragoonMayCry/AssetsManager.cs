using Dalamud.Interface.ImGuiNotification;
using Newtonsoft.Json;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
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

        private const string TargetAssetVersion = "1.3.0.0";

        private const long RequiredDiskSpaceCompressed = 56_683_188;
        private const long RequiredDiskSpaceExtracted = 57_383_642;

        public static void VerifyAndUpdateAssets()
        {
            try
            {
                Task.Run(FetchAudioFiles);
            }
            catch (Exception ex)
            {
                Service.Log.Error(ex, "An exception occured while fetching audio files");
            }
        }

        public static void FetchAudioFiles()
        {
            if (Status == AssetsStatus.Updating)
            {
                return;
            }

            var configDir = Plugin.PluginInterface.GetPluginConfigDirectory(); ;
            var localAssetDir = GetAssetsDirectory();
            if (Directory.Exists(localAssetDir) && AreLocalFilesValid())
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

            var assetsUri = new Uri($"https://github.com/Felscream/DragoonMayCry/releases/download/v{TargetAssetVersion}/assets.zip");
            var downloadLocation = $"{configDir}/assets-{TargetAssetVersion}.zip";
            const long requiredSpace = RequiredDiskSpaceExtracted + RequiredDiskSpaceCompressed;
            var freeDiskSpace = new DriveInfo(configDir).AvailableFreeSpace;

            if (freeDiskSpace < requiredSpace)
            {
                LogAndNotify("Not enough free disk space to extract assets", NotificationType.Error);
                Status = AssetsStatus.FailedInsufficientDiskSpace;
                return;
            }

            HttpClient httpClient = new();
            var response = httpClient.GetAsync(assetsUri).Result;

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
            return TargetAssetVersion == CurrentDownloadedAssetVersion();
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
                Type = type,
            };

            Service.NotificationManager.AddNotification(notification);
        }

        private static string? CurrentDownloadedAssetVersion()
        {
            var assetsDir = GetAssetsDirectory();
            var manifestFile = $"{assetsDir}/manifest.json";

            if (!File.Exists(manifestFile)) return null;

            var jsonData = File.ReadAllText(manifestFile);
            var manifest = JsonConvert.DeserializeObject<AssetsManifest>(jsonData);

            return manifest?.Version;
        }
    }

    internal class AssetsManifest
    {
        [JsonProperty("version")]
        public string? Version { get; set; }
    }
}
