#region

using Dalamud.Interface;
using Dalamud.Interface.ImGuiFileDialog;
using ImGuiNET;
using System;
using System.IO;

#endregion

namespace DragoonMayCry.UI.Utility
{
    public class AudioFileSelector : IDisposable
    {
        private readonly FileDialogManager fileDialog = new()
        {
            AddedWindowFlags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking,
        };

        public AudioFileSelector()
        {
            fileDialog.CustomSideBarItems.Add((Environment.ExpandEnvironmentVariables("User Folder"),
                                                  Environment.ExpandEnvironmentVariables("%USERPROFILE%"),
                                                  FontAwesomeIcon.User, 0));
        }

        public void Dispose()
        {
            fileDialog.Reset();
        }

        public string SelectAudioFile(string? filePath)
        {
            var newPath = "";
            var startPath = string.IsNullOrEmpty(filePath)
                                ? Environment.ExpandEnvironmentVariables("%USERPROFILE%")
                                : Path.GetDirectoryName(filePath);
            fileDialog.OpenFileDialog("Select a file", ".ogg", (success, paths) =>
                                      {
                                          if (success && paths.Count > 0)
                                          {
                                              newPath = paths[0];
                                          }
                                      }, 1,
                                      startPath);
            return newPath;
        }


        public void OnClose()
        {
            fileDialog.Reset();
        }
    }
}
