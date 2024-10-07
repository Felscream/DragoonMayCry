using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using DragoonMayCry.Data;
using DragoonMayCry.Record;
using DragoonMayCry.Record.Model;
using DragoonMayCry.State;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DragoonMayCry.UI
{
    public class CharacterRecordWindow : Window
    {
        private readonly IClientState clientState;
        private readonly RecordService recordService;
        private readonly PlayerState playerState;

        private Dictionary<JobIds, JobRecord> characterRecords = new();
        private readonly Extension[] extensions = new Extension[0];
        private readonly List<String> extensionValues = new();
        private readonly List<JobIds> jobs = new();
        private ExtensionCategory[] categories = [];
        private List<string> subcategories = new();
        private JobIds selectedJob;
        private int selectedExtensionId = 0;
        private int selectedCategoryId = 0;
        private int selectedSubcategoryId = 0;

        public CharacterRecordWindow(RecordService recordService) : base("DragoonMayCry - Character records")
        {
            Size = new System.Numerics.Vector2(800f, 600f);
            SizeCondition = ImGuiCond.Appearing;

            playerState = PlayerState.GetInstance();
            playerState.RegisterJobChangeHandler(OnJobChange);

            clientState = Service.ClientState;
            clientState.Login += OnLogin;

            this.recordService = recordService;
            this.recordService.CharacterRecordsChanged += OnCharacterRecordChanged;

            extensions = this.recordService.Extensions;
            extensionValues = extensions.Select(extension => extension.Name).ToList();
            selectedExtensionId = extensionValues.Count - 1;

            UpdateCategories();

            jobs = [.. Enum.GetValues(typeof(JobIds)).Cast<JobIds>().Where(job => job != JobIds.OTHER).OrderBy(job => job.ToString())];
            selectedJob = jobs[0];

            if (clientState.IsLoggedIn)
            {
                characterRecords = recordService.GetCharacterRecords();
                var job = playerState.GetCurrentJob();
                if (job != JobIds.OTHER && jobs.Contains(job))
                {
                    selectedJob = job;
                }
            }


        }

        public override void Draw()
        {
            if (characterRecords.Count == 0)
            {
                DrawEmptyWindow();
            }
            else
            {
                DrawRecords();
            }
        }

        private void OnCharacterRecordChanged(object? sender, Dictionary<JobIds, JobRecord> records)
        {
            characterRecords = records;
        }

        private void OnLogin()
        {
            characterRecords = recordService.GetCharacterRecords();
        }

        private void OnJobChange(object? sender, JobIds job)
        {
            if (job == JobIds.OTHER || this.IsOpen)
            {
                return;
            }
            selectedJob = job;
        }

        private void DrawEmptyWindow()
        {
            ImGui.Text("Please log in with a character to access this window");
        }

        private void DrawRecords()
        {
            ImGui.SetNextItemWidth(75f);
            if (ImGui.BeginCombo("Job", selectedJob.ToString()))
            {
                for (var i = 0; i < jobs.Count; i++)
                {
                    if (ImGui.Selectable(jobs[i].ToString(), jobs[i] == selectedJob))
                    {
                        selectedJob = jobs[i];
                    }

                    if (selectedJob == jobs[i])
                    {
                        ImGui.SetItemDefaultFocus();
                    }
                }
                ImGui.EndCombo();
            }

            #region filters
            if (ImGui.BeginChild("Filters"))
            {

                ImGui.SetNextItemWidth(150f);
                if (ImGui.BeginCombo("Extension", extensionValues[selectedExtensionId]))
                {
                    for (var i = 0; i < extensionValues.Count; i++)
                    {
                        if (ImGui.Selectable(extensionValues[i], i == selectedExtensionId))
                        {
                            selectedExtensionId = i;
                            UpdateCategories();
                        }

                        if (selectedExtensionId == i)
                        {
                            ImGui.SetItemDefaultFocus();
                        }
                    }
                    ImGui.EndCombo();
                }

                ImGui.SameLine();
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 20f);
                ImGui.SetNextItemWidth(125f);
                if (ImGui.BeginCombo("Duty type", categories[selectedCategoryId].Type.ToString()))
                {
                    for (var i = 0; i < categories.Length; i++)
                    {
                        if (ImGui.Selectable(categories[i].Type.ToString(), i == selectedCategoryId))
                        {
                            selectedCategoryId = i;
                            UpdateSubcategories();
                        }

                        if (selectedCategoryId == i)
                        {
                            ImGui.SetItemDefaultFocus();
                        }
                    }
                    ImGui.EndCombo();
                }

                if (subcategories.Count > 0)
                {
                    ImGui.SameLine();
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 20f);
                    ImGui.SetNextItemWidth(200f);

                    if (ImGui.BeginCombo("Series", subcategories[selectedSubcategoryId]))
                    {
                        for (var i = 0; i < subcategories.Count; i++)
                        {
                            if (ImGui.Selectable(subcategories[i], i == selectedSubcategoryId))
                            {
                                selectedSubcategoryId = i;
                            }

                            if (selectedSubcategoryId == i)
                            {
                                ImGui.SetItemDefaultFocus();
                            }
                        }
                        ImGui.EndCombo();
                    }
                }

                ImGui.EndChild();
                #endregion
            }
        }

        private void UpdateCategories()
        {
            categories = extensions[selectedExtensionId].Categories;
            selectedCategoryId = 0;
            UpdateSubcategories();
        }

        private void UpdateSubcategories()
        {
            subcategories = [.. categories[selectedCategoryId].Subcategories];
            selectedSubcategoryId = 0;
        }
    }
}
