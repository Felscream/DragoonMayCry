using Dalamud.Interface.Components;
using System;
using Dalamud.Interface.Windowing;
using DragoonMayCry.Configuration;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using ImGuiNET;
using KamiLib;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using System.Collections.Generic;
using System.Numerics;
using PlayerState = DragoonMayCry.State.PlayerState;

namespace DragoonMayCry.UI;

public class BgmDutyBlacklistConfigurationWindow : Window, IDisposable
{
    public EventHandler? BgmBlacklistChanged;
    private readonly ExcelSheet<ContentFinderCondition> contentFinder;
    private readonly DmcConfiguration configuration;
    private readonly PlayerState playerState;

    private string searchInput = "";
    private ISet<LightContentFinderCondition> searchResults = new HashSet<LightContentFinderCondition>();
    private int idToBlacklist;
    private ISet<uint> blacklistedIds;

    public BgmDutyBlacklistConfigurationWindow(
        DmcConfiguration configuration, ImGuiWindowFlags flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse,
        bool forceMainWindow = false) : base(
        "DragoonMayCry - Dynamic BGM Duty Blacklist##DmCBlacklistBgmConfiguration", flags,
        forceMainWindow)
    {
        Size = new Vector2(0, 0);
        SizeCondition = ImGuiCond.Appearing;
        this.configuration = configuration;
        contentFinder = Service.DataManager.GetExcelSheet<ContentFinderCondition>();
        playerState = PlayerState.GetInstance();
        blacklistedIds = new SortedSet<uint>(this.configuration.DynamicBgmBlacklistDuties.Value);
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
            if (!string.IsNullOrEmpty(contentFinderRow.Name.ToString())
                && UIState.IsInstanceContentUnlocked(contentFinderRow.Content.RowId))
            {
                if (ImGui.Button("Add to blacklist##addToBlacklistButton"))
                {
                    AddToBlacklist((uint)idToBlacklist);
                }
            }
        }

        ImGui.Separator();
        if (ImGui.BeginTable("##dutySearchResults", 2, ImGuiTableFlags.Resizable))
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
        if (ImGui.BeginChild("##blacklistedItems"))
        {
            foreach (var blacklistedId in blacklistedIds)
            {
                var endOfLineX = ImGui.GetContentRegionAvail().X - 20f - ImGui.GetStyle().WindowPadding.X;
                if (contentFinder.TryGetRow(blacklistedId, out var row))
                {
                    var content = ToLightContentFinderCondition(row);
                    ImGui.TextUnformatted($"{content.Name} - ID {content.RowId}");
                    ImGui.SameLine();
                    ImGui.SetCursorPosX(endOfLineX);
                    if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.Trash))
                    {
                        RemoveFromBlacklist(content.RowId);
                    }
                }
            }
            blacklistedIds = new SortedSet<uint>(this.configuration.DynamicBgmBlacklistDuties.Value);
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
        if (ImGui.BeginChild("##dutySearchResults", new Vector2(380, 200)))
        {
            foreach (var content in searchResults)
            {
                if (ImGui.Selectable($"{content.Name} - ID {content.RowId}", idToBlacklist == content.RowId,
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
            ImGui.TextUnformatted($"{content.Name} - ID {content.RowId}");
            ImGui.SameLine();
            if (ImGui.Button("Blacklist current duty"))
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

    public void Dispose()
    {
        throw new NotImplementedException();
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
            Name = condition.Name.ToString(),
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
