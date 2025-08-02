#region

using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Windowing;
using DragoonMayCry.Audio.BGM.CustomBgm;
using DragoonMayCry.Audio.BGM.CustomBgm.Model;
using ImGuiNET;
using KamiLib.Drawing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

#endregion

namespace DragoonMayCry.UI
{
    public class BgmEditor : Window
    {
        private readonly CustomBgmService customBgmService;
        private readonly Vector4 errorColor = new(219, 45, 26, 255);
        private readonly FileDialogManager fileDialogManager = new();
        private readonly string[] tabNames =
        [
            "Intro", "Combat Start", "Verse Loop", "Chorus Loop", "Transitions", "Combat End",
        ];
        private CustomBgmProject? currentProject;
        private string currentProjectNewName;
        private bool displayProjectNameError;
        private string newProjectName = "";
        private string projectNameError;
        private Dictionary<long, CustomBgmProject> projects;
        private CustomBgmProject? projectToDelete;
        private bool showDeleteConfirmDialog;
        private bool showNewProjectDialog;

        public BgmEditor() : base("DragoonMayCry - BGM Editor ##dmc-bgm-editor")
        {
            Size = new Vector2(1200, 800);
            SizeCondition = ImGuiCond.Appearing;
            customBgmService = CustomBgmService.Instance;
            fileDialogManager.CustomSideBarItems.Add((Environment.ExpandEnvironmentVariables("User Folder"),
                                                         Environment.ExpandEnvironmentVariables("%USERPROFILE%"),
                                                         FontAwesomeIcon.User, 0));
            projects = customBgmService.GetProjects();
        }

        public override void Draw()
        {
            DrawMainInterface();
            DrawNewProjectDialog();
            DrawDeleteConfirmDialog();
            fileDialogManager.Draw();
        }

        private void DrawMainInterface()
        {
            // Project selection and management
            if (ImGui.BeginTable("##ProjectTable", 2))
            {
                ImGui.TableSetupColumn("##LeftColumn", ImGuiTableColumnFlags.WidthFixed, 300);
                ImGui.TableNextColumn();
                DrawProjectManagement();

                ImGui.TableNextColumn();
                DrawProjectErrors();
                ImGui.EndTable();
            }


            ImGui.Separator();

            if (currentProject != null)
            {
                DrawProjectEditor();
            }
            else
            {
                ImGui.Text("Select a project to edit or create a new one.");
            }
        }
        private void DrawProjectErrors()
        {

            if (ImGui.BeginChild("##project_errors", new Vector2(0, 230)))
            {
                ImGui.Text("Note : only `.ogg` files with a sample rate of 48kHz are supported.");
                ImGui.Separator();
                if (currentProject != null)
                {
                    foreach (var error in customBgmService.GetProjectErrors(currentProject))
                    {
                        ImGui.Text(error);
                    }
                }

                ImGui.EndChild();
            }
        }

        private Vector4 GetProjectColor(CustomBgmProject project)
        {
            return customBgmService.IsProjectValid(project) ? Colors.White : Colors.SoftRed;
        }

        private void DrawProjectManagement()
        {
            ImGui.Text("Custom BGM Projects");
            ImGui.SameLine();

            if (ImGui.Button("New Project"))
            {
                newProjectName = "";
                showNewProjectDialog = true;
            }

            ImGui.SameLine();

            if (ImGui.Button("Refresh"))
            {
                customBgmService.LoadProjects();
                projects = customBgmService.GetProjects();
            }

            // Project list
            ImGui.BeginChild("##project_list", new Vector2(300, 200), true);

            foreach (var project in projects.Values)
            {
                var isSelected = currentProject?.Id == project.Id;
                var name = $"##{project.Name}";

                if (ImGui.Selectable(name, isSelected))
                {
                    currentProject = project;
                    currentProjectNewName = currentProject.Name;
                }
                if (ImGui.BeginPopupContextItem($"##context_{project.Id}"))
                {
                    if (ImGui.MenuItem("Delete"))
                    {
                        projectToDelete = project;
                        showDeleteConfirmDialog = true;

                    }
                    ImGui.EndPopup();
                }

                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Text, GetProjectColor(project));
                ImGui.Text(project.Name);
                ImGui.PopStyleColor();
            }

            ImGui.EndChild();
        }

        private void DrawProjectEditor()
        {
            if (currentProject == null) return;

            // Project name editing
            ImGui.InputText("Project Name", ref currentProjectNewName, 100);

            ImGui.SameLine();

            if (ImGui.Button("Save"))
            {
                customBgmService.SaveProject(currentProject, currentProjectNewName);
                currentProject = customBgmService.GetProjectById(currentProject.Id);
                projects[currentProject!.Id] = currentProject;
            }
            ImGui.SameLine();
            if (ImGui.Button("Cancel"))
            {
                if (projects.TryGetValue(currentProject!.Id, out var project))
                {
                    currentProject = customBgmService.GetProjectById(currentProject.Id);
                    projects[currentProject!.Id] = currentProject;
                }
                currentProject = null;
            }

            if (currentProject == null)
            {
                return;
            }

            // Tab bar for different sections
            if (ImGui.BeginTabBar("##project_tabs"))
            {
                for (var i = 0; i < tabNames.Length; i++)
                {
                    if (ImGui.BeginTabItem(tabNames[i]))
                    {
                        DrawTabContent(i);
                        ImGui.EndTabItem();
                    }
                }
                ImGui.EndTabBar();
            }
        }

        private void DrawTabContent(int tabIndex)
        {
            switch (tabIndex)
            {
                case 0:
                    DrawIntroTab();
                    break;
                case 1: // Combat Start
                    DrawCombatStartTab();
                    break;
                case 2: // Verse Loop
                    DrawVerseLoopTab();
                    break;
                case 3: // Chorus Loop
                    DrawChorusLoopTab();
                    break;
                case 4: // Transitions
                    DrawTransitionsTab();
                    break;
                case 5: // Combat End
                    DrawCombatEndTab();
                    break;
            }
        }

        private void DrawIntroTab()
        {
            ImGui.Text("Intro Configuration");
            ImGui.Separator();

            ImGui.Text("The intro is a single audio file that loops until combat begins.");

            if (currentProject?.Intro == null)
            {
                currentProject!.Intro = new Stem();
            }

            DrawStemConfiguration(currentProject.Intro);

            ImGui.Text("Transition time is when the audio should start transitioning to the next state.");
        }

        private void DrawCombatStartTab()
        {
            ImGui.Text("Combat Start Transition");
            ImGui.Separator();

            ImGui.Text("This audio plays when entering combat to transition from the intro to the verse loop.");

            if (currentProject?.CombatStart == null)
            {
                currentProject!.CombatStart = new Stem();
            }

            DrawStemConfiguration(currentProject.CombatStart);
        }

        private void DrawVerseLoopTab()
        {
            ImGui.Text("Verse Loop Configuration");
            ImGui.Separator();

            ImGui.Text(
                "Verse loop is the base combat state (D rank to A Rank). It consists of a list of audio stems.");
            ImGui.Text("One stem is selected randomly to be played for a given group.");
            ImGui.Text(
                "Groups are played sequentially, one after another : Group 1 -> Group 2 -> ... -> Group N -> Group 1 -> ...");

            if (ImGui.Button("Add Group"))
            {
                currentProject!.VerseLoop.AddLast(new Group());
            }

            ImGui.SameLine();

            if (ImGui.Button("Clear All Groups"))
            {
                currentProject!.VerseLoop.Clear();
            }

            // Draw groups
            var groupIndex = 0;
            var groupsToRemove = new List<Group>();

            foreach (var group in currentProject!.VerseLoop)
            {
                ImGui.PushID($"verse_group_{groupIndex}");

                if (ImGui.CollapsingHeader($"Group {groupIndex + 1} ({group.Stems.Count} stems)"))
                {
                    DrawGroupContent(group, "verse");

                    ImGui.NewLine();
                    if (ImGui.Button("Remove Group"))
                    {
                        groupsToRemove.Add(group);
                    }
                }

                ImGui.PopID();
                groupIndex++;
            }

            // Remove marked groups
            foreach (var group in groupsToRemove)
            {
                currentProject.VerseLoop.Remove(group);
            }
        }

        private void DrawChorusLoopTab()
        {
            ImGui.Text("Chorus Loop Configuration");
            ImGui.Separator();

            ImGui.Text("Chorus loop plays when the rating reaches S rank. It works the same as the verse loop.");

            if (ImGui.Button("Add Group"))
            {
                currentProject!.ChorusLoop.AddLast(new Group());
            }

            ImGui.SameLine();

            if (ImGui.Button("Clear All Groups"))
            {
                currentProject!.ChorusLoop.Clear();
            }

            // Draw groups
            var groupIndex = 0;
            var groupsToRemove = new List<Group>();

            foreach (var group in currentProject!.ChorusLoop)
            {
                ImGui.PushID($"chorus_group_{groupIndex}");

                if (ImGui.CollapsingHeader($"Group {groupIndex + 1} ({group.Stems.Count} stems)"))
                {
                    DrawGroupContent(group, "chorus");

                    if (ImGui.Button("Remove Group"))
                    {
                        groupsToRemove.Add(group);
                    }
                }

                ImGui.PopID();
                groupIndex++;
            }

            // Remove marked groups
            foreach (var group in groupsToRemove)
            {
                currentProject.ChorusLoop.Remove(group);
            }
        }

        private void DrawGroupContent(Group group, string prefix)
        {
            if (ImGui.Button($"Add Stem##{prefix}"))
            {
                group.AddStem(new Stem());
            }

            ImGui.SameLine();

            if (ImGui.Button($"Clear Stems##{prefix}"))
            {
                group.ClearStems();
            }

            DrawStemList(group.Stems, prefix);
        }

        private void DrawTransitionsTab()
        {
            ImGui.Text("Transition Configuration");
            ImGui.Separator();

            // Chorus Transitions
            ImGui.Text("Chorus Transitions");
            ImGui.Text("One is played randomly when transitioning from verse to chorus (S rank achieved).");

            DrawGroupContent(currentProject!.ChorusTransitions, "chorusTransistion");


            ImGui.Separator();

            // Demotion Transitions
            ImGui.Text("Demotion Transitions");
            ImGui.Text(
                "One is played randomly when transitioning from chorus back to verse (rating drops to A or lower).");

            DrawGroupContent(currentProject.DemotionTransitions, "demotionTransistion");
        }

        private void DrawStemList(ICollection<Stem> stems, string prefix)
        {
            var stemIndex = 0;
            var stemsToRemove = new List<Stem>();

            foreach (var stem in stems)
            {
                DrawGroupStem(prefix, stemIndex, stem, stemsToRemove);
                ImGui.PopID();
                stemIndex++;
            }
            ImGui.Separator();

            // Remove marked stems
            foreach (var stem in stemsToRemove)
            {
                stems.Remove(stem);
            }
        }
        private void DrawGroupStem(string prefix, int stemIndex, Stem stem, List<Stem> stemsToRemove)
        {
            var fileName = Path.GetFileName(stem.AudioPath);
            ImGui.PushID($"{prefix}_{stemIndex}");
            ImGui.Separator();
            ImGui.Text($"Stem {stemIndex + 1}");
            DrawStemConfiguration(stem);

            if (ImGui.Button($"Remove Stem##{prefix}-{stemIndex}"))
            {
                stemsToRemove.Add(stem);
            }
        }

        private void DrawStemConfiguration(Stem stem)
        {
            var filePath = stem.AudioPath;
            if (ImGui.InputText($"##filePath-{stem.Id}", ref filePath, 600))
            {
                stem.AudioPath = filePath;
            }
            ImGui.SameLine();
            if (ImGuiComponents.IconButton(FontAwesomeIcon.Folder))
            {
                var startPath = string.IsNullOrEmpty(filePath)
                                    ? Environment.ExpandEnvironmentVariables("%USERPROFILE%/Music")
                                    : Path.GetDirectoryName(filePath);
                fileDialogManager.OpenFileDialog("Select a file", ".ogg",
                                                 (success, paths) => UpdateFilePath(stem, success, paths), 1,
                                                 startPath);
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Audio file selection...");
            }

            var transitionTime = stem.TransitionTime ?? 0;
            if (ImGui.InputInt("Transition Time (ms)", ref transitionTime, 1, 100))
            {
                transitionTime = Math.Max(0, transitionTime);
                stem.TransitionTime = transitionTime;
            }
        }


        private void UpdateFilePath(Stem stem, bool success, List<string> paths)
        {
            if (success && paths.Count > 0)
            {
                stem.AudioPath = paths[0];
            }
        }

        private void DrawCombatEndTab()
        {
            ImGui.Text("Combat End Configuration");
            ImGui.Separator();

            ImGui.Text("This audio plays when combat ends, regardless of current state.");

            if (currentProject?.CombatEnd == null)
            {
                currentProject!.CombatEnd = new Stem();
            }
            DrawStemConfiguration(currentProject.CombatEnd);
            var endFadeOutDelay = currentProject!.EndFadeOutDelay;
            if (ImGui.SliderInt("Combat End Fade Out Delay (ms)", ref endFadeOutDelay, 0, 10000))
            {
                currentProject.EndFadeOutDelay = endFadeOutDelay;
            }

            var endFadeOutDuration = currentProject.EndFadeOutDuration;
            if (ImGui.SliderInt("Combat End Fade Out Duration (ms)", ref endFadeOutDuration, 0, 10000))
            {
                currentProject.EndFadeOutDuration = endFadeOutDuration;
            }

            var nextSongTransitionStart = currentProject.NextSongTransistionStart;
            if (ImGui.SliderInt("Next Song Transition Start (ms)", ref nextSongTransitionStart, 0, 10000))
            {
                currentProject.NextSongTransistionStart = nextSongTransitionStart;
            }
        }

        private void DrawNewProjectDialog()
        {
            if (showNewProjectDialog)
            {
                showNewProjectDialog = false;
                ImGui.OpenPopup("Create New Dynamic Bgm");
            }
            if (ImGui.BeginPopupModal("Create New Dynamic Bgm"))
            {
                ImGui.Text("Enter project name:");
                ImGui.InputText("##new_project_name", ref newProjectName, 100);
                if (displayProjectNameError)
                {
                    ImGui.TextColored(errorColor, projectNameError);
                }
                if (ImGui.Button("Create"))
                {
                    if (!string.IsNullOrWhiteSpace(newProjectName))
                    {
                        if (customBgmService.IsNameUnique(newProjectName))
                        {
                            currentProject = customBgmService.CreateNewProject(newProjectName);
                            projects[currentProject.Id] = currentProject;
                            currentProjectNewName = currentProject.Name;
                            displayProjectNameError = false;
                            projectNameError = "";
                            ImGui.CloseCurrentPopup();
                        }
                        else
                        {
                            displayProjectNameError = true;
                            projectNameError = "Project name already exists!";
                        }
                    }
                    else
                    {
                        displayProjectNameError = true;
                        projectNameError = "Name cannot be empty";
                    }
                }

                ImGui.SameLine();

                if (ImGui.Button("Cancel"))
                {
                    ImGui.CloseCurrentPopup();
                }

                ImGui.EndPopup();
            }
        }

        private void DrawDeleteConfirmDialog()
        {
            if (showDeleteConfirmDialog && projectToDelete != null)
            {
                ImGui.OpenPopup($"Delete {projectToDelete.Name}");
                showDeleteConfirmDialog = false;
            }

            if (projectToDelete != null &&
                ImGui.BeginPopupModal($"Delete {projectToDelete.Name}"))
            {
                ImGui.Text($"Are you sure you want to delete '{projectToDelete.Name}'?");
                ImGui.Text("\nThis action cannot be undone.");

                if (ImGui.Button("Delete"))
                {
                    if (customBgmService.DeleteProject(projectToDelete))
                    {
                        projects.Remove(projectToDelete.Id);
                        if (currentProject?.Id == projectToDelete.Id)
                        {
                            currentProject = null;
                        }
                        projectToDelete = null;
                    }

                    ImGui.CloseCurrentPopup();
                }

                ImGui.SameLine();

                if (ImGui.Button("Cancel"))
                {
                    projectToDelete = null;
                    ImGui.CloseCurrentPopup();
                }

                ImGui.EndPopup();
            }
        }
    }
}
