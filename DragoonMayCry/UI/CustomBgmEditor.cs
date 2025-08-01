#region

using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Windowing;
using DragoonMayCry.Audio.BGM.CustomBgm;
using DragoonMayCry.Audio.BGM.CustomBgm.Model;
using DragoonMayCry.UI.Utility;
using ImGuiNET;
using KamiLib.Drawing;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

#endregion

namespace DragoonMayCry.UI
{
    public class CustomBgmEditor : Window
    {
        private readonly AudioFileSelector audioFileSelector;
        private readonly CustomBgmService bgmService;
        private readonly Vector4 errorColor = new(219, 45, 26, 255);
        private readonly string[] tabNames =
        {
            "Projects", "Intro", "Combat Start", "Verse Loop", "Chorus Loop", "Transitions", "Combat End", "Settings",
        };
        private CustomBgmProject? currentProject;
        private string currentProjectNewName;
        private FileDialogManager fileDialog;
        private bool nameExistsOpened;
        private string newProjectName = "";
        private CustomBgmProject? projectToDelete;
        private int selectedTab;
        private bool showDeleteConfirmDialog;
        private bool showErrorDialog;
        private bool showNewProjectDialog;
        private bool showSuccessDialog;

        public CustomBgmEditor() : base("DragoonMayCry - BGM Editor ##dmc-bgm-editor")
        {
            Size = new Vector2(800, 600);
            SizeCondition = ImGuiCond.Appearing;
            bgmService = CustomBgmService.Instance;
            fileDialog = new FileDialogManager();
            audioFileSelector = new AudioFileSelector();
        }

        public override void Draw()
        {

            DrawMainInterface();
            DrawNewProjectDialog();
            DrawDeleteConfirmDialog();
        }

        private void DrawMainInterface()
        {
            // Project selection and management
            DrawProjectManagement();

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

        private Vector4 GetProjectColor(CustomBgmProject project)
        {
            return bgmService.IsProjectValid(project) ? Colors.White : Colors.SoftRed;
        }

        private void DrawProjectManagement()
        {
            ImGui.Text("Custom BGM Projects");
            ImGui.SameLine();

            if (ImGui.Button("New Project"))
            {
                showNewProjectDialog = true;
                newProjectName = "";
                ImGui.OpenPopup("Create New Dynamic Bgm");
            }

            ImGui.SameLine();

            if (ImGui.Button("Refresh"))
            {
                bgmService.LoadProjects();
            }

            // Project list
            ImGui.BeginChild("##project_list", new Vector2(300, 200), true);

            foreach (var project in bgmService.Projects)
            {
                var isSelected = currentProject?.Id == project.Id;
                var name = $"##{project.Name}";

                if (ImGui.Selectable(name, isSelected))
                {
                    currentProject = project;
                    currentProjectNewName = currentProject.Name;
                }

                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Text, GetProjectColor(project));
                ImGui.Text(project.Name);
                ImGui.PopStyleColor();

                if (ImGui.BeginPopupContextItem($"##context_{project.Id}"))
                {
                    if (ImGui.MenuItem("Delete"))
                    {
                        projectToDelete = project;
                        showDeleteConfirmDialog = true;
                        ImGui.OpenPopup($"Delete {projectToDelete.Name}");
                    }
                    ImGui.EndPopup();
                }
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
                bgmService.SaveProject(currentProject, currentProjectNewName);
            }

            ImGui.SameLine();

            if (ImGui.Button("Validate"))
            {
                var errors = bgmService.ValidateProject(currentProject);
                if (errors.Count == 0)
                {
                    ImGui.OpenPopup("Validation Result");
                }
                else
                {
                    ImGui.OpenPopup("Validation Errors");
                }
            }

            // Draw validation popups
            DrawValidationPopups();

            // Tab bar for different sections
            if (ImGui.BeginTabBar("##project_tabs"))
            {
                for (var i = 0; i < tabNames.Length; i++)
                {
                    if (ImGui.BeginTabItem(tabNames[i]))
                    {
                        selectedTab = i;
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
                case 0: // Projects (shouldn't happen, but just in case)
                    break;
                case 1: // Intro
                    DrawIntroTab();
                    break;
                case 2: // Combat Start
                    DrawCombatStartTab();
                    break;
                case 3: // Verse Loop
                    DrawVerseLoopTab();
                    break;
                case 4: // Chorus Loop
                    DrawChorusLoopTab();
                    break;
                case 5: // Transitions
                    DrawTransitionsTab();
                    break;
                case 6: // Combat End
                    DrawCombatEndTab();
                    break;
                case 7: // Settings
                    DrawSettingsTab();
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
                currentProject!.Intro = new Stem("", 0);
            }

            if (ImGuiComponents.IconButton(FontAwesomeIcon.Folder))
            {
                currentProject.Intro.AudioPath = SelectFilePath(currentProject.Intro.AudioPath);
            }

            var transitionTime = currentProject.Intro.TransitionTime ?? 0;
            if (ImGui.SliderInt("Transition Time (ms)", ref transitionTime, 0, 10000))
            {
                currentProject.Intro.TransitionTime = transitionTime;
            }

            ImGui.Text("Transition time is when the audio should start transitioning to the next state.");
        }

        private void DrawCombatStartTab()
        {
            ImGui.Text("Combat Start Transition");
            ImGui.Separator();

            ImGui.Text("This audio plays when entering combat to transition from intro to verse loop.");

            if (currentProject?.CombatStart == null)
            {
                currentProject!.CombatStart = new Stem("", 0);
            }

            if (ImGuiComponents.IconButton(FontAwesomeIcon.Folder))
            {
                currentProject.CombatStart.AudioPath = SelectFilePath(currentProject.CombatStart.AudioPath);
            }

            var transitionTime = currentProject.CombatStart.TransitionTime ?? 0;
            if (ImGui.SliderInt("Transition Time (ms)", ref transitionTime, 0, 10000))
            {
                currentProject.CombatStart.TransitionTime = transitionTime;
            }
        }

        private void DrawVerseLoopTab()
        {
            ImGui.Text("Verse Loop Configuration");
            ImGui.Separator();

            ImGui.Text(
                "Verse loop is the base combat state. It consists of groups of audio stems that play sequentially.");
            ImGui.Text("Each group contains multiple stems that are chosen randomly. Groups play in order and loop.");

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

            ImGui.Text("Chorus loop plays when player performance reaches S rank. It works the same as verse loop.");

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
            if (ImGui.Button("Add Stem"))
            {
                group.AddStem(new Stem("", 0));
            }

            ImGui.SameLine();

            if (ImGui.Button("Clear Stems"))
            {
                group.ClearStems();
            }

            var stemIndex = 0;
            var stemsToRemove = new List<Stem>();

            foreach (var stem in group.Stems)
            {
                ImGui.PushID($"{prefix}_stem_{stemIndex}");

                if (ImGui.CollapsingHeader($"Stem {stemIndex + 1}"))
                {
                    if (ImGuiComponents.IconButton(FontAwesomeIcon.Folder))
                    {
                        stem.AudioPath = SelectFilePath(stem.AudioPath);
                    }

                    var transitionTime = stem.TransitionTime ?? 0;
                    if (ImGui.SliderInt("Transition Time (ms)", ref transitionTime, 0, 10000))
                    {
                        stem.TransitionTime = transitionTime;
                    }

                    if (ImGui.Button("Remove Stem"))
                    {
                        stemsToRemove.Add(stem);
                    }
                }

                ImGui.PopID();
                stemIndex++;
            }

            // Remove marked stems
            foreach (var stem in stemsToRemove)
            {
                group.RemoveStem(stem);
            }
        }

        private void DrawTransitionsTab()
        {
            ImGui.Text("Transition Configuration");
            ImGui.Separator();

            // Chorus Transitions
            ImGui.Text("Chorus Transitions");
            ImGui.Text("These play when transitioning from verse to chorus (S rank achieved).");

            if (ImGui.Button("Add Chorus Transition"))
            {
                currentProject!.ChorusTransitions.Add(new Stem("", 0));
            }

            ImGui.SameLine();

            if (ImGui.Button("Clear Chorus Transitions"))
            {
                currentProject!.ChorusTransitions.Clear();
            }

            DrawStemList(currentProject!.ChorusTransitions, "chorus_transition");

            ImGui.Separator();

            // Demotion Transitions
            ImGui.Text("Demotion Transitions");
            ImGui.Text("These play when transitioning from chorus back to verse (rating drops to A or lower).");

            if (ImGui.Button("Add Demotion Transition"))
            {
                currentProject.DemotionTransitions.Add(new Stem("", 0));
            }

            ImGui.SameLine();

            if (ImGui.Button("Clear Demotion Transitions"))
            {
                currentProject.DemotionTransitions.Clear();
            }

            DrawStemList(currentProject.DemotionTransitions, "demotion_transition");
        }

        private void DrawStemList(List<Stem> stems, string prefix)
        {
            var stemIndex = 0;
            var stemsToRemove = new List<Stem>();

            foreach (var stem in stems)
            {
                ImGui.PushID($"{prefix}_{stemIndex}");

                if (ImGui.CollapsingHeader($"Stem {stemIndex + 1}"))
                {
                    if (ImGuiComponents.IconButton(FontAwesomeIcon.Folder))
                    {
                        stem.AudioPath = SelectFilePath(stem.AudioPath);
                    }

                    var transitionTime = stem.TransitionTime ?? 0;
                    if (ImGui.SliderInt("Transition Time (ms)", ref transitionTime, 0, 10000))
                    {
                        stem.TransitionTime = transitionTime;
                    }

                    if (ImGui.Button("Remove Stem"))
                    {
                        stemsToRemove.Add(stem);
                    }
                }

                ImGui.PopID();
                stemIndex++;
            }

            // Remove marked stems
            foreach (var stem in stemsToRemove)
            {
                stems.Remove(stem);
            }
        }
        private string SelectFilePath(string audioPath)
        {
            var path = audioFileSelector.SelectAudioFile(audioPath);
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                return path;
            }
            return audioPath;
        }

        private void DrawCombatEndTab()
        {
            ImGui.Text("Combat End Configuration");
            ImGui.Separator();

            ImGui.Text("This audio plays when combat ends, regardless of current state.");

            if (currentProject?.CombatEnd == null)
            {
                currentProject!.CombatEnd = new Stem("", 0);
            }

            if (ImGuiComponents.IconButton(FontAwesomeIcon.Folder))
            {
                currentProject.CombatEnd.AudioPath = SelectFilePath(currentProject.CombatEnd.AudioPath);
            }


            var transitionTime = currentProject.CombatEnd.TransitionTime ?? 0;
            if (ImGui.SliderInt("Transition Time (ms)", ref transitionTime, 0, 10000))
            {
                currentProject.CombatEnd.TransitionTime = transitionTime;
            }
        }

        private void DrawSettingsTab()
        {
            ImGui.Text("Advanced Settings");
            ImGui.Separator();

            var endFadeOutDelay = currentProject!.EndFadeOutDelay;
            if (ImGui.SliderInt("End Fade Out Delay (ms)", ref endFadeOutDelay, 0, 10000))
            {
                currentProject.EndFadeOutDelay = endFadeOutDelay;
            }

            var endFadeOutDuration = currentProject.EndFadeOutDuration;
            if (ImGui.SliderInt("End Fade Out Duration (ms)", ref endFadeOutDuration, 0, 10000))
            {
                currentProject.EndFadeOutDuration = endFadeOutDuration;
            }

            var nextSongTransitionStart = currentProject.NextSongTransistionStart;
            if (ImGui.SliderInt("Next Song Transition Start (ms)", ref nextSongTransitionStart, 0, 10000))
            {
                currentProject.NextSongTransistionStart = nextSongTransitionStart;
            }
        }

        private void DrawValidationPopups()
        {
            // Validation success popup
            if (showSuccessDialog && ImGui.BeginPopupModal("Validation Result", ref showSuccessDialog))
            {
                ImGui.Text("Project is valid! ✓");
                if (ImGui.Button("OK"))
                {
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }

            // Validation errors popup
            if (showErrorDialog && ImGui.BeginPopupModal("Validation Errors", ref showErrorDialog))
            {
                var errors = bgmService.ValidateProject(currentProject!);
                ImGui.Text("The following errors were found:");
                ImGui.Separator();

                foreach (var error in errors)
                {
                    ImGui.Text($"• {error}");
                }

                if (ImGui.Button("OK"))
                {
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }
        }

        private void DrawNewProjectDialog()
        {
            if (ImGui.BeginPopupModal("Create New Dynamic Bgm", ref showNewProjectDialog))
            {
                ImGui.Text("Enter project name:");
                ImGui.InputText("##new_project_name", ref newProjectName, 100);

                if (ImGui.Button("Create"))
                {
                    if (!string.IsNullOrWhiteSpace(newProjectName))
                    {
                        if (bgmService.IsNameUnique(newProjectName))
                        {
                            currentProject = bgmService.CreateNewProject(newProjectName);
                            currentProjectNewName = currentProject.Name;
                            showNewProjectDialog = false;
                        }
                        else
                        {
                            ImGui.TextColored(errorColor, "Project name already exists!");
                        }
                    }
                    else
                    {
                        ImGui.TextColored(errorColor, "Name cannot be empty");
                    }
                }

                ImGui.SameLine();

                if (ImGui.Button("Cancel"))
                {
                    showNewProjectDialog = false;
                }

                ImGui.EndPopup();
            }
        }

        private void DrawDeleteConfirmDialog()
        {
            if (projectToDelete != null &&
                ImGui.BeginPopupModal($"Delete {projectToDelete.Name}", ref showDeleteConfirmDialog))
            {
                ImGui.Text($"Are you sure you want to delete '{projectToDelete.Name}'?");
                ImGui.Text("This action cannot be undone.");

                if (ImGui.Button("Delete"))
                {
                    bgmService.DeleteProject(projectToDelete);
                    if (currentProject?.Id == projectToDelete.Id)
                    {
                        currentProject = null;
                    }
                    projectToDelete = null;
                    showDeleteConfirmDialog = false;
                }

                ImGui.SameLine();

                if (ImGui.Button("Cancel"))
                {
                    projectToDelete = null;
                    showDeleteConfirmDialog = false;
                }

                ImGui.EndPopup();
            }
        }
    }
}
