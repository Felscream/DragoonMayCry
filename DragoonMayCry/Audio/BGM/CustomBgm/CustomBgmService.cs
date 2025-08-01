#region

using DragoonMayCry.Audio.BGM.CustomBgm.Model;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

#endregion

namespace DragoonMayCry.Audio.BGM.CustomBgm
{
    public class CustomBgmService
    {
        private static CustomBgmService? instance;

        private readonly string customBgmDirectory;
        private readonly List<CustomBgmProject> projects = new();

        private CustomBgmService()
        {
            var configDir = Plugin.PluginInterface.GetPluginConfigDirectory();
            customBgmDirectory = Path.Combine(configDir, "CustomBGM");
            Directory.CreateDirectory(customBgmDirectory);
            LoadProjects();
        }
        public static CustomBgmService Instance => instance ??= new CustomBgmService();

        public IReadOnlyList<CustomBgmProject> Projects => projects.AsReadOnly();

        public void LoadProjects()
        {
            projects.Clear();
            if (!Directory.Exists(customBgmDirectory)) return;

            var projectFiles = Directory.GetFiles(customBgmDirectory, "*.json");
            foreach (var file in projectFiles)
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var project = JsonConvert.DeserializeObject<CustomBgmProject>(json);
                    if (project != null)
                    {
                        projects.Add(project);
                    }
                }
                catch (Exception ex)
                {
                    Service.Log.Error($"Failed to load Custom BGM project from {file}: {ex.Message}");
                }
            }
        }

        public void UpdateProjectName(CustomBgmProject project, string newName)
        {
            var oldFilePath = Path.Combine(customBgmDirectory, $"{project.Name}.json");
            var newFilePath = Path.Combine(customBgmDirectory, $"{newName}.json");
            try
            {
                File.Move(oldFilePath, newFilePath);
                project.Name = newName;
            }
            catch (IOException e)
            {
                Log.Error("An error occured while renaming {}", oldFilePath);
            }
        }

        public void SaveProject(CustomBgmProject project, string newName)
        {
            if (newName != project.Name)
            {
                UpdateProjectName(project, newName);
            }
            var filePath = Path.Combine(customBgmDirectory, $"{project.Name}.json");
            var json = JsonConvert.SerializeObject(project, Formatting.Indented);
            File.WriteAllText(filePath, json);

            // Update the project in our list
            var existingIndex = projects.FindIndex(p => p.Id == project.Id);
            if (existingIndex >= 0)
            {
                projects[existingIndex] = project;
            }
            else
            {
                projects.Add(project);
            }
        }

        public void DeleteProject(CustomBgmProject project)
        {
            var filePath = Path.Combine(customBgmDirectory, $"{project.Name}.json");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            projects.RemoveAll(p => p.Id == project.Id);
        }

        public CustomBgmProject CreateNewProject(string name)
        {
            var project = new CustomBgmProject
            {
                Name = name,
            };
            return project;
        }

        public List<string> ValidateProject(CustomBgmProject project)
        {
            return CustomBgmValidator.GetErrors(project);
        }

        public bool IsProjectValid(CustomBgmProject project)
        {
            return !ValidateProject(project).Any();
        }

        public CustomBgmProject? GetProjectById(long id)
        {
            return projects.FirstOrDefault(p => p.Id == id);
        }

        public CustomBgmProject? GetProjectByName(string name)
        {
            return projects.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public bool IsNameUnique(string name, long? excludeId = null)
        {
            return !projects.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase) &&
                                      (excludeId == null || p.Id != excludeId));
        }
    }
}
