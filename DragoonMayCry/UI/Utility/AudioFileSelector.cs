#region

using Dalamud.Interface;
using Dalamud.Interface.ImGuiFileDialog;
using ImGuiNET;
using System;
using System.Collections.Generic;
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

        public void SelectAudioFile(string? filePath, Action<bool, List<string>> callback)
        {
            var startPath = string.IsNullOrEmpty(filePath)
                                ? Environment.ExpandEnvironmentVariables("%USERPROFILE%")
                                : Path.GetDirectoryName(filePath);
            fileDialog.OpenFileDialog("Select a file", ".ogg", callback, 1, startPath);
        }

        public void OnClose()
        {
            fileDialog.Reset();
        }
    }
}
