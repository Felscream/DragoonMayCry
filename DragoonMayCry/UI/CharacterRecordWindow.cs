using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using DragoonMayCry.Data;
using DragoonMayCry.Record;
using DragoonMayCry.Record.Model;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using ImGuiNET;
using KamiLib.Caching;
using Lumina.Excel.GeneratedSheets2;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection;
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

        private Dictionary<JobId, JobRecord> characterRecords = new();
        private readonly Dictionary<ushort, uint> dutyToContentId = new();
        private readonly LuminaCache<ContentFinderCondition> contentFinder;
        private readonly Extension[] extensions = new Extension[0];
        private readonly List<String> extensionValues = new();
        private readonly List<JobId> jobs = new();
        private const string HiddenDutyTexPath = "ui/icon/112000/112036_hr1.tex";
        private const string MissingRankTexPath = "DragoonMayCry.Assets.MissingRank.png";
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
        private readonly ImGuiTableFlags tableFlags = ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.ScrollX | ImGuiTableFlags.BordersInnerH;

        public CharacterRecordWindow(RecordService recordService, ConfigWindow configWindow) : base("DragoonMayCry - Character records")
        {
            textureProvider = Service.TextureProvider;
            contentFinder = LuminaCache<ContentFinderCondition>.Instance;
            this.configWindow = configWindow;

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

            jobs = [.. Enum.GetValues(typeof(JobId)).Cast<JobId>().Where(job => job != JobId.OTHER).OrderBy(job => job.ToString())];
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
            if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.Cog))
            {
                configWindow.Toggle();
            }
            ImGui.SameLine();
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
                    ImGui.SetNextItemWidth(200f);

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
                    ImGui.TableSetupColumn("Normal", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoHeaderWidth, 180f);
                    ImGui.TableSetupColumn("Estinien Must Die", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoHeaderWidth, 180f);
                    ImGui.TableHeadersRow();
                    for (var i = 0; i < displayedDuties.Count; i++)
                    {
                        ImGui.TableNextRow();

                        #region duty
                        ImGui.TableNextColumn();

                        var scaledDutyTextureSize = dutyTextureSize * 1.4f;

                        CenterText(GetDutyName(displayedDuties[i]), scaledDutyTextureSize);
                        if (textureProvider.GetFromGame(GetTexturePath(displayedDuties[i])).TryGetWrap(out var tex, out var _))
                        {
                            ImGui.Image(tex.ImGuiHandle, scaledDutyTextureSize);
                        }
                        #endregion

                        var jobRecords = GetJobRecord();

                        #region normal
                        ImGui.TableNextColumn();
                        var normalRecord = GetDutyRecordPerDifficulty(1, jobRecords);

                        DrawRank(displayedDuties[i].dutyId, normalRecord);
                        CenterText(GetKillTime(displayedDuties[i].dutyId, normalRecord), ImGui.GetContentRegionAvail());

                        var recordDate = GetRecordDate(displayedDuties[i].dutyId, normalRecord);
                        if (!string.IsNullOrEmpty(recordDate))
                        {
                            CenterText(recordDate, ImGui.GetContentRegionAvail());
                        }
                        #endregion


                        #region emd
                        ImGui.TableNextColumn();

                        var emdRecords = GetDutyRecordPerDifficulty(2, jobRecords);

                        DrawRank(displayedDuties[i].dutyId, emdRecords);
                        CenterText(GetKillTime(displayedDuties[i].dutyId, emdRecords), ImGui.GetContentRegionAvail());
                        var emdRecordDate = GetRecordDate(displayedDuties[i].dutyId, emdRecords);
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
                .Where(entry => entry.Value.Category == categories[selectedCategoryId].Type
                    && (subcategories.Count == 0
                        || entry.Value.Subcategory.Contains(subcategories[selectedSubcategoryId])))
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
                .Where(entry => entry.Value.Category == categories[selectedCategoryId].Type
                    && (subcategories.Count == 0
                        || entry.Value.Subcategory.Contains(subcategories[selectedSubcategoryId]))
                    && entry.Value.Difficulty == difficulties[selectedDifficultyId])
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

        private uint GetContentId(DisplayedDuty displayedDuty)
        {
            if (dutyToContentId.TryGetValue(displayedDuty.dutyId, out var contentId))
            {
                return contentId;
            }

            var contentFinderCondition = contentFinder.FirstOrDefault(content => content.TerritoryType.Row == displayedDuty.dutyId);
            if (contentFinderCondition == null)
            {
                return 0;
            }

            dutyToContentId.Add(displayedDuty.dutyId, contentFinderCondition.Content.Row);
            return dutyToContentId[displayedDuty.dutyId];
        }

        private string GetTexturePath(DisplayedDuty displayed)
        {
            var contentId = GetContentId(displayed);
            if (contentId == 0 || !UIState.IsInstanceContentUnlocked(contentId))
            {
                return HiddenDutyTexPath;
            }
            return displayed.duty.TexPath;
        }

        private void CenterText(string text, Vector2 availableSpace)
        {
            var textWidth = ImGui.CalcTextSize(text).X;
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (availableSpace.X - textWidth) * 0.5f);
            ImGui.TextUnformatted(text);
        }

        private string GetKillTime(ushort dutyId, Dictionary<ushort, DutyRecord> difficultyRecord)
        {
            var defaultKillTime = "--:--";
            if (!difficultyRecord.ContainsKey(dutyId))
            {
                return defaultKillTime;
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
            if (Service.TextureProvider.GetFromManifestResource(Assembly.GetExecutingAssembly(), rankIconPath).TryGetWrap(out var rankIcon, out var _))
            {
                ImGui.Image(rankIcon.ImGuiHandle, rankSize);
            }
        }

        private string GetRecordDate(ushort dutyId, Dictionary<ushort, DutyRecord> difficultyRecord)
        {
            if (difficultyRecord.TryGetValue(dutyId, out var dutyRecord))
            {
                return dutyRecord.Date.ToString(CultureInfo.CurrentCulture);
            }
            return "";
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
            var contentId = GetContentId(displayed);
            if (contentId == 0 || !UIState.IsInstanceContentUnlocked(contentId))
            {
                return "???";
            }
            var duty = displayed.duty;
            if (duty.Difficulty == Difficulty.HighEnd)
            {
                if (duty.Category == Category.Trials)
                {
                    return $"{duty.Name} - Extreme";
                }
                if (duty.Category == Category.Raids)
                {
                    return $"{duty.Name} - Savage";
                }
            }
            return duty.Name;
        }

        private struct DisplayedDuty
        {
            public readonly ushort dutyId;
            public readonly TrackableDuty duty;

            public DisplayedDuty(ushort id, TrackableDuty duty)
            {
                dutyId = id;
                this.duty = duty;
            }
        }
    }
}
