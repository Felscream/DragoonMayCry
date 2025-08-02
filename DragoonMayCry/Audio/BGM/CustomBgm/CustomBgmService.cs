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
        private readonly Dictionary<long, CustomBgmProject> projects = new();

        private CustomBgmService()
        {
            var configDir = Plugin.PluginInterface.GetPluginConfigDirectory();
            customBgmDirectory = Path.Combine(configDir, "CustomBGM");
            Directory.CreateDirectory(customBgmDirectory);
            LoadProjects();
        }
        public static CustomBgmService Instance => instance ??= new CustomBgmService();

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
                        projects.Add(project.Id, project);
                    }
                }
                catch (Exception ex)
                {
                    Service.Log.Error($"Failed to load Custom BGM project from {file}: {ex.Message}");
                }
            }
        }

        public Dictionary<long, CustomBgmProject> GetProjects()
        {
            var json = JsonConvert.SerializeObject(projects);
            return JsonConvert.DeserializeObject<Dictionary<long, CustomBgmProject>>(json);
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
            SaveProject(project);
        }

        public void SaveProject(CustomBgmProject project)
        {
            var filePath = Path.Combine(customBgmDirectory, $"{project.Name}.json");
            var json = JsonConvert.SerializeObject(project, Formatting.Indented);
            File.WriteAllText(filePath, json);

            projects[project.Id] = project;
        }

        public bool DeleteProject(CustomBgmProject project)
        {
            var filePath = Path.Combine(customBgmDirectory, $"{project.Name}.json");
            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                    projects.Remove(project.Id);
                }
                catch (IOException e)
                {
                    return false;
                }

            }
            return true;
        }

        public CustomBgmProject CreateNewProject(string name)
        {
            var project = new CustomBgmProject(name);
            SaveProject(project);
            return GetProjectById(project.Id)!;
        }

        public List<string> GetProjectErrors(CustomBgmProject project)
        {
            return CustomBgmValidator.GetErrors(project);
        }

        public bool IsProjectValid(CustomBgmProject project)
        {
            return !GetProjectErrors(project).Any();
        }

        public CustomBgmProject? GetProjectById(long id)
        {
            if (projects.TryGetValue(id, out var project))
            {
                var json = JsonConvert.SerializeObject(project);
                return JsonConvert.DeserializeObject<CustomBgmProject>(json);
            }
            return null;
        }

        public CustomBgmProject? GetProjectByName(string name)
        {
            return projects.FirstOrDefault(p => p.Value.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).Value;
        }

        public bool IsNameUnique(string name, long? excludeId = null)
        {
            return !projects.Any(p => p.Value.Name.Equals(name, StringComparison.OrdinalIgnoreCase) &&
                                      (excludeId == null || p.Value.Id != excludeId));
        }
    }
}
