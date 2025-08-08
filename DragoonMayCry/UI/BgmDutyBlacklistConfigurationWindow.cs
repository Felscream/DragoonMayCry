#region

using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
using DragoonMayCry.Configuration;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using KamiLib;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Numerics;
using PlayerState = DragoonMayCry.State.PlayerState;

#endregion

namespace DragoonMayCry.UI
{
    public class BgmDutyBlacklistConfigurationWindow : Window, IDisposable
    {
        private readonly DmcConfiguration configuration;
        private readonly ExcelSheet<ContentFinderCondition> contentFinder;
        private readonly PlayerState playerState;
        public EventHandler? BgmBlacklistChanged;
        private ISet<uint> blacklistedIds;
        private int idToBlacklist;

        private string searchInput = "";
        private ISet<LightContentFinderCondition> searchResults = new HashSet<LightContentFinderCondition>();

        public BgmDutyBlacklistConfigurationWindow(
            DmcConfiguration configuration,
            ImGuiWindowFlags flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse,
            bool forceMainWindow = false) : base(
            "DragoonMayCry - Dynamic BGM Duty Blacklist##DmCBlacklistBgmConfiguration", flags,
            forceMainWindow)
        {
            Size = new Vector2(750, 300);
            SizeCondition = ImGuiCond.Appearing;
            this.configuration = configuration;
            contentFinder = Service.DataManager.GetExcelSheet<ContentFinderCondition>();
            playerState = PlayerState.GetInstance();
            blacklistedIds = new SortedSet<uint>(this.configuration.DynamicBgmBlacklistDuties.Value);
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public override void Draw()
        {
            if (playerState.GetCurrentContentId() != 0)
            {
                CreateBlacklistCurrentDutyButton();
            }
            ImGui.SetNextItemWidth(200);
            if (ImGui.InputInt("ID to blacklist##idToBlacklist", ref idToBlacklist))
            {
                idToBlacklist = Math.Max(0, idToBlacklist);
            }
            if (contentFinder.TryGetRow((uint)idToBlacklist, out var contentFinderRow))
            {
                if (!string.IsNullOrEmpty(contentFinderRow.Name.ExtractText())
                    && UIState.IsInstanceContentUnlocked(contentFinderRow.Content.RowId))
                {
                    if (ImGui.Button("Add to blacklist##addToBlacklistButton"))
                    {
                        AddToBlacklist((uint)idToBlacklist);
                    }
                }
            }

            ImGui.Separator();
            if (ImGui.BeginTable("##dutySearchResults", 2,
                                 ImGuiTableFlags.Resizable | ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.Borders))
            {
                ImGui.TableSetupColumn("Duty search", ImGuiTableColumnFlags.None, 280f);
                ImGui.TableSetupColumn("Blacklisted duties", ImGuiTableColumnFlags.None, 240f);

                ImGui.TableHeadersRow();

                ImGui.TableNextColumn();
                DrawDutySearch();

                ImGui.TableNextColumn();
                DrawBlacklistedDuties();

                ImGui.EndTable();
            }
        }
        private void DrawBlacklistedDuties()
        {
            if (ImGui.BeginChild("##blacklistedItems", new Vector2(0, 0)))
            {
                foreach (var blacklistedId in blacklistedIds)
                {
                    var endOfLineX = ImGui.GetContentRegionAvail().X - 20f - ImGui.GetStyle().WindowPadding.X;
                    if (contentFinder.TryGetRow(blacklistedId, out var row)
                        && UIState.IsInstanceContentUnlocked(row.Content.RowId))
                    {
                        var content = ToLightContentFinderCondition(row);
                        ImGui.Text($"{content.Name} - ID {content.RowId}");
                        ImGui.SameLine();
                        ImGui.SetCursorPosX(endOfLineX);
                        if (ImGuiComponents.IconButton((int)content.RowId, FontAwesomeIcon.Trash))
                        {
                            RemoveFromBlacklist(content.RowId);
                        }
                    }
                }
                blacklistedIds = new SortedSet<uint>(configuration.DynamicBgmBlacklistDuties.Value);
                ImGui.EndChild();
            }
        }
        private void DrawDutySearch()
        {
            ImGui.SetNextItemWidth(180);
            if (ImGui.InputText("Search duty by name or ID##contentSearchInput", ref searchInput, 30))
            {
                searchResults.Clear();
                searchResults = FindContentWithNameOrId(searchInput.Trim().ToLower());
            }
            ImGuiComponents.HelpMarker("You must have unlocked the duty for it to appear in the results.");
            if (ImGui.BeginChild("##dutySearchResults", new Vector2(0, 0)))
            {
                foreach (var content in searchResults)
                {
                    if (ImGui.Selectable($"{content.Name} - ID {content.RowId}##result-{content.RowId}",
                                         idToBlacklist == content.RowId,
                                         ImGuiSelectableFlags.AllowDoubleClick))
                    {
                        idToBlacklist = (int)content.RowId;
                        if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                        {
                            AddToBlacklist(content.RowId);
                        }
                    }
                }
                ImGui.EndChild();
            }
        }
        private void CreateBlacklistCurrentDutyButton()
        {
            if (contentFinder.TryGetRow(playerState.GetCurrentContentId(), out var row))
            {
                var content = ToLightContentFinderCondition(row);
                ImGui.TextUnformatted($"Current duty : {content.Name} - ID {content.RowId}");
                ImGui.SameLine();
                if (ImGui.Button($"Blacklist current duty##blacklist-add-{content.RowId}"))
                {
                    AddToBlacklist(content.RowId);
                }
            }
        }
        private void AddToBlacklist(uint contentRowId)
        {
            configuration.DynamicBgmBlacklistDuties.Value.Add(contentRowId);
            KamiCommon.SaveConfiguration();
            BgmBlacklistChanged?.Invoke(this, EventArgs.Empty);
        }

        private void RemoveFromBlacklist(uint contentRowId)
        {
            configuration.DynamicBgmBlacklistDuties.Value.Remove(contentRowId);
            KamiCommon.SaveConfiguration();
            BgmBlacklistChanged?.Invoke(this, EventArgs.Empty);
        }

        private ISet<LightContentFinderCondition> FindContentWithNameOrId(string contentNameOrId)
        {
            var results = new HashSet<LightContentFinderCondition>();
            if (string.IsNullOrEmpty(contentNameOrId))
            {
                return results;
            }

            foreach (var row in contentFinder)
            {
                var content = ToLightContentFinderCondition(row);
                if (string.IsNullOrEmpty(content.Name) || !UIState.IsInstanceContentUnlocked(content.ContentId))
                {
                    continue;
                }
                if (content.RowId.ToString().StartsWith(searchInput)
                    || content.Name.StartsWith(searchInput, StringComparison.CurrentCultureIgnoreCase))
                {
                    results.Add(content);
                }
            }
            return results;
        }

        private static LightContentFinderCondition ToLightContentFinderCondition(ContentFinderCondition condition)
        {
            return new LightContentFinderCondition
            {
                RowId = condition.RowId,
                ContentId = condition.Content.RowId,
                Name = condition.Name.ExtractText(),
            };
        }

        private struct LightContentFinderCondition : IEquatable<LightContentFinderCondition>
        {
            public bool Equals(LightContentFinderCondition other)
            {
                return RowId == other.RowId;
            }

            public override bool Equals(object? obj)
            {
                return obj is LightContentFinderCondition other && Equals(other);
            }

            public override int GetHashCode()
            {
                return (int)RowId;
            }

            public uint RowId;
            public uint ContentId;
            public string Name;
        }
    }
}
