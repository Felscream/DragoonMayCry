using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using DragoonMayCry.Data;
using DragoonMayCry.Record;
using DragoonMayCry.Record.Model;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using ImGuiNET;
using KamiLib.Caching;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using DragoonMayCry.Configuration;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using PlayerState = DragoonMayCry.State.PlayerState;

namespace DragoonMayCry.UI
{
    public class CharacterRecordWindow : Window
    {
        private readonly IClientState clientState;
        private readonly ITextureProvider textureProvider;
        private readonly RecordService recordService;
        private readonly PlayerState playerState;
        private readonly ConfigWindow configWindow;
        private readonly HowItWorksWindow howItWorksWindow;

        private Dictionary<JobId, JobRecord> characterRecords = new();
        private readonly ExcelSheet<ContentFinderCondition> contentFinder;
        private readonly Extension[] extensions;
        private readonly List<String> extensionValues;
        private readonly List<JobId> jobs;
        private const uint HiddenDutyIconId = 112056;
        private const string MissingRankTexPath = "DragoonMayCry.Assets.MissingRank.png";
        private const string DefaultKillTime = "--:--";
        private ExtensionCategory[] categories = [];
        private List<string> subcategories = new();
        private List<Difficulty> difficulties = new();
        private List<DisplayedDuty> displayedDuties = new();
        private JobId selectedJob;
        private int selectedExtensionId = 0;
        private int selectedCategoryId = 0;
        private int selectedSubcategoryId = 0;
        private int selectedDifficultyId = 0;
        private readonly Vector2 dutyTextureSize = new(376, 120);
        private readonly Vector2 rankSize = new(130, 130);

        private readonly ImGuiTableFlags tableFlags =
            ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.ScrollX | ImGuiTableFlags.BordersInnerH;

        public CharacterRecordWindow(
            RecordService recordService, ConfigWindow configWindow, HowItWorksWindow howItWorksWindow) : base(
            "DragoonMayCry - Character records")
        {
            textureProvider = Service.TextureProvider;
            contentFinder = Service.DataManager.GetExcelSheet<ContentFinderCondition>();
            this.configWindow = configWindow;
            this.howItWorksWindow = howItWorksWindow;

            Size = new Vector2(955f, 730f);
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

            jobs =
            [
                .. Enum.GetValues(typeof(JobId)).Cast<JobId>().Where(job => job != JobId.OTHER)
                       .OrderBy(job => job.ToString())
            ];
            selectedJob = jobs[0];

            if (clientState.IsLoggedIn)
            {
                characterRecords = recordService.GetCharacterRecords();
                var job = playerState.GetCurrentJob();
                if (job != JobId.OTHER && jobs.Contains(job))
                {
                    selectedJob = job;
                }
            }
        }

        public override void Draw()
        {
            ImGui.SetCursorPosX(ImGui.GetContentRegionAvail().X - 40f - ImGui.GetStyle().WindowPadding.X);
            if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.Cog))
            {
                configWindow.Toggle();
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Configuration");
            }

            ImGui.SameLine();

            if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.Question))
            {
                howItWorksWindow.Toggle();
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("How it works");
            }

            ImGui.SameLine();
            ImGui.SetCursorPosX(ImGui.GetStyle().WindowPadding.X);


            if (!clientState.IsLoggedIn)
            {
                DrawEmptyWindow();
            }
            else
            {
                DrawRecords();
            }
        }

        private void OnCharacterRecordChanged(object? sender, Dictionary<JobId, JobRecord> records)
        {
            characterRecords = records;
        }

        private void OnLogin()
        {
            characterRecords = recordService.GetCharacterRecords();
        }

        private void OnJobChange(object? sender, JobId job)
        {
            if (job == JobId.OTHER || this.IsOpen)
            {
                return;
            }

            selectedJob = job;
        }

        private static void DrawEmptyWindow()
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
                }

                ImGui.EndCombo();
            }

            #region filters

            if (ImGui.BeginChild("Filters", new System.Numerics.Vector2(1000f, 50f)))
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
                    }

                    ImGui.EndCombo();
                }

                ImGui.SameLine();
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 20f);
                ImGui.SetNextItemWidth(100f);
                if (ImGui.BeginCombo("Difficulty", GetDifficultyLabel(difficulties[selectedDifficultyId])))
                {
                    for (var i = 0; i < difficulties.Count; i++)
                    {
                        if (ImGui.Selectable(GetDifficultyLabel(difficulties[i]), i == selectedDifficultyId))
                        {
                            selectedDifficultyId = i;
                            UpdateDisplayedDuties();
                        }
                    }

                    ImGui.EndCombo();
                }

                if (subcategories.Count > 0)
                {
                    ImGui.SameLine();
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 20f);
                    ImGui.SetNextItemWidth(175f);

                    if (ImGui.BeginCombo("Series", subcategories[selectedSubcategoryId]))
                    {
                        for (var i = 0; i < subcategories.Count; i++)
                        {
                            if (ImGui.Selectable(subcategories[i], i == selectedSubcategoryId))
                            {
                                selectedSubcategoryId = i;
                                UpdateDifficulty();
                            }
                        }

                        ImGui.EndCombo();
                    }
                }

                ImGui.EndChild();
            }

            #endregion

            #region duties

            if (ImGui.BeginChild("Duties"))
            {
                if (ImGui.BeginTable("duties-table", 3, tableFlags))
                {
                    ImGui.TableSetupColumn("Duty", ImGuiTableColumnFlags.WidthFixed, 540f);
                    ImGui.TableSetupColumn(
                        DifficultyMode.WyrmHunter.GetLabel(),
                        ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoHeaderWidth, 180f);
                    ImGui.TableSetupColumn(DifficultyMode.EstinienMustDie.GetLabel(),
                                           ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoHeaderWidth,
                                           180f);
                    ImGui.TableHeadersRow();
                    for (var i = 0; i < displayedDuties.Count; i++)
                    {
                        ImGui.TableNextRow();

                        #region duty

                        ImGui.TableNextColumn();

                        var scaledDutyTextureSize = dutyTextureSize * 1.4f;

                        CenterText(GetDutyName(displayedDuties[i]), scaledDutyTextureSize);

                        if (TryGetTexture(displayedDuties[i], out var texture))
                        {
                            ImGui.Image(texture.ImGuiHandle, scaledDutyTextureSize);
                        }

                        #endregion

                        var jobRecords = GetJobRecord();

                        #region normal

                        ImGui.TableNextColumn();
                        var normalRecord = GetDutyRecordPerDifficulty(1, jobRecords);

                        DrawRank(displayedDuties[i].DutyId, normalRecord);
                        CenterText(GetKillTime(displayedDuties[i].DutyId, normalRecord), ImGui.GetContentRegionAvail());

                        var recordDate = GetRecordDate(displayedDuties[i].DutyId, normalRecord);
                        if (!string.IsNullOrEmpty(recordDate))
                        {
                            CenterText(recordDate, ImGui.GetContentRegionAvail());
                        }

                        #endregion


                        #region emd

                        ImGui.TableNextColumn();

                        var emdRecords = GetDutyRecordPerDifficulty(2, jobRecords);

                        DrawRank(displayedDuties[i].DutyId, emdRecords);
                        CenterText(GetKillTime(displayedDuties[i].DutyId, emdRecords), ImGui.GetContentRegionAvail());
                        var emdRecordDate = GetRecordDate(displayedDuties[i].DutyId, emdRecords);
                        if (!string.IsNullOrEmpty(emdRecordDate))
                        {
                            CenterText(emdRecordDate, ImGui.GetContentRegionAvail());
                        }

                        #endregion
                    }

                    ImGui.EndTable();
                }

                ImGui.EndChild();
            }

            #endregion
        }

        private void UpdateCategories()
        {
            if (extensions[selectedExtensionId].Categories.Length == 0)
            {
                return;
            }

            categories = extensions[selectedExtensionId].Categories;
            selectedCategoryId = 0;
            UpdateSubcategories();
        }

        private void UpdateSubcategories()
        {
            subcategories = [.. categories[selectedCategoryId].Subcategories];
            selectedSubcategoryId = 0;

            UpdateDifficulty();
        }

        private void UpdateDifficulty()
        {
            var tempDiff = extensions[selectedExtensionId].Instances
                                                          .Where(entry => entry.Value.Category ==
                                                                          categories[selectedCategoryId].Type
                                                                          && (subcategories.Count == 0 ||
                                                                              subcategories[selectedSubcategoryId]
                                                                                  .Equals(entry.Value.Subcategory)))
                                                          .Select(entry => entry.Value.Difficulty)
                                                          .Distinct()
                                                          .ToList();

            if (difficulties.Count == 0 || !tempDiff.Contains(difficulties[selectedDifficultyId]))
            {
                selectedDifficultyId = 0;
            }
            else
            {
                selectedDifficultyId = tempDiff.IndexOf(difficulties[selectedDifficultyId]);
            }

            difficulties = tempDiff;
            UpdateDisplayedDuties();
        }

        private void UpdateDisplayedDuties()
        {
            displayedDuties = extensions[selectedExtensionId].Instances
                                                             .Where(entry => entry.Value.Category ==
                                                                             categories[selectedCategoryId].Type
                                                                             && (subcategories.Count == 0
                                                                                         || subcategories[
                                                                                                 selectedSubcategoryId]
                                                                                             .Equals(
                                                                                                 entry.Value
                                                                                                     .Subcategory)
                                                                                 )
                                                                             && entry.Value.Difficulty ==
                                                                             difficulties[selectedDifficultyId])
                                                             .Select(entry => new DisplayedDuty(entry.Key, entry.Value))
                                                             .ToList();
        }

        private static string GetDifficultyLabel(Difficulty diff)
        {
            return diff switch
            {
                Difficulty.HighEnd => "High-end",
                Difficulty.Normal => "Normal",
                _ => "Unknown"
            };
        }

        private ContentFinderCondition? GetContent(DisplayedDuty displayedDuty)
        {
            var contentFinderCondition =
                contentFinder.FirstOrDefault(content => content.TerritoryType.RowId == displayedDuty.DutyId);
            return contentFinderCondition;
        }

        private bool TryGetTexture(DisplayedDuty displayed, out IDalamudTextureWrap texture)
        {
            var content = GetContent(displayed);
            var iconToDisplay = displayed.Duty.IconId;
            if (content == null || !UIState.IsInstanceContentUnlocked(content.Value.Content.RowId))
            {
                iconToDisplay = HiddenDutyIconId;
            }

            texture = null!;
            if (Service.TextureProvider.TryGetFromGameIcon(iconToDisplay, out var tex))
            {
                if (tex.TryGetWrap(out var wrap, out var _))
                {
                    texture = wrap;
                    return true;
                }
            }

            return false;
        }

        private void CenterText(string text, Vector2 availableSpace)
        {
            var textWidth = ImGui.CalcTextSize(text).X;
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (availableSpace.X - textWidth) * 0.5f);
            ImGui.TextUnformatted(text);
        }

        private static string GetKillTime(ushort dutyId, Dictionary<ushort, DutyRecord> difficultyRecord)
        {
            if (!difficultyRecord.ContainsKey(dutyId))
            {
                return DefaultKillTime;
            }

            var dutyResult = difficultyRecord[dutyId];
            var minutes = $"{dutyResult.KillTime.Minutes}";
            minutes = minutes.PadLeft(2, '0');
            var seconds = $"{dutyResult.KillTime.Seconds}";
            seconds = seconds.PadLeft(2, '0');

            return $"{minutes}:{seconds}";
        }

        private void DrawRank(ushort dutyId, Dictionary<ushort, DutyRecord> difficultyRecord)
        {
            var rankIconPath = MissingRankTexPath;

            if (difficultyRecord.TryGetValue(dutyId, out var dutyRecord))
            {
                if (StyleRankUI.styleUis.TryGetValue(dutyRecord.Result, out var style))
                {
                    rankIconPath = style.IconPath;
                }
            }

            ImGui.SetCursorPos(new Vector2(ImGui.GetCursorPosX() + 30, ImGui.GetCursorPosY() + 10));
            if (Service.TextureProvider.GetFromManifestResource(Assembly.GetExecutingAssembly(), rankIconPath)
                       .TryGetWrap(out var rankIcon, out var _))
            {
                ImGui.Image(rankIcon.ImGuiHandle, rankSize);
            }
        }

        private static string GetRecordDate(ushort dutyId, Dictionary<ushort, DutyRecord> difficultyRecord)
        {
            return difficultyRecord.TryGetValue(dutyId, out var dutyRecord)
                       ? dutyRecord.Date.ToString(CultureInfo.CurrentCulture)
                       : "";
        }

        private JobRecord GetJobRecord()
        {
            if (characterRecords.TryGetValue(selectedJob, out var jobRecord))
            {
                return jobRecord;
            }

            return new JobRecord();
        }

        private Dictionary<ushort, DutyRecord> GetDutyRecordPerDifficulty(int column, JobRecord jobRecord)
        {
            if (column == 2)
            {
                return jobRecord.EmdRecord;
            }

            return jobRecord.Record;
        }

        private string GetDutyName(DisplayedDuty displayed)
        {
            var content = GetContent(displayed);
            if (content == null || !UIState.IsInstanceContentUnlocked(content.Value.Content.RowId))
            {
                return "???";
            }

            var name = content?.Name.ToString() ?? displayed.Duty.Name;
            return string.Concat(name[0].ToString().ToUpper(), name.AsSpan(1));
        }

        private struct DisplayedDuty(ushort id, TrackableDuty duty)
        {
            public readonly ushort DutyId = id;
            public readonly TrackableDuty Duty = duty;
        }
    }
}
