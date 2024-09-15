using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using DragoonMayCry.Audio.StyleAnnouncer;
using DragoonMayCry.Configuration;
using DragoonMayCry.Data;
using DragoonMayCry.State;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using ImGuiNET;
using KamiLib;
using KamiLib.Configuration;
using KamiLib.Drawing;
using KamiLib.Interfaces;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using PlayerState = DragoonMayCry.State.PlayerState;

namespace DragoonMayCry.UI
{
    public class JobConfigurationWindow : Window, IDisposable
    {
        public struct JobAnnouncerType
        {
            public AnnouncerType type;
            public JobIds job;
            public JobAnnouncerType(AnnouncerType type, JobIds job)
            {
                this.type = type;
                this.job = job;
            }
        }

        public EventHandler<JobAnnouncerType>? JobAnnouncerTypeChange;
        private readonly DmcConfigurationOne configuration;
        private readonly IList<AnnouncerType> announcers = Enum.GetValues(typeof(AnnouncerType)).Cast<AnnouncerType>().ToList();
        private readonly IList<JobConfiguration.BgmConfiguration> bgmOptions = Enum.GetValues(typeof(JobConfiguration.BgmConfiguration)).Cast<JobConfiguration.BgmConfiguration>().ToList();
        private Setting<AnnouncerType> selectedAnnouncerPreview = new(AnnouncerType.DmC5);
        private ISelectable selected;
        private IList<ISelectable> selectableJobConfiguration = new List<ISelectable>();
        public JobConfigurationWindow(DmcConfigurationOne configuration) : base("DragoonMayCry - Job configuration##DmCJobConfiguration", ImGuiWindowFlags.NoScrollbar)
        {
            Size = new System.Numerics.Vector2(600, 320);
            SizeCondition = ImGuiCond.Appearing;

            this.configuration = configuration;
            foreach(KeyValuePair<JobIds, JobConfiguration> entry in configuration.JobConfiguration)
            {
                selectableJobConfiguration.Add(new JobSelection(entry.Key, entry.Value, announcers, bgmOptions));
            }
            selected = selectableJobConfiguration[0];
        }

        

        public override void Draw()
        {
            InfoBox.Instance
                .AddTitle("Announcer Preview")
                .AddConfigCombo(announcers, selectedAnnouncerPreview, StyleAnnouncerService.GetAnnouncerTypeLabel, "", 150)
                .SameLine().AddIconButton("preview", Dalamud.Interface.FontAwesomeIcon.Play, () => Plugin.StyleAnnouncerService?.PlayRandomAnnouncerLine(selectedAnnouncerPreview.Value))
                .Draw();

            var region = ImGui.GetContentRegionAvail();
            var itemSpacing = ImGui.GetStyle().ItemSpacing;

            var topLeftSideHeight = region.Y * ImGuiHelpers.GlobalScale - itemSpacing.Y / 2.0f;

            if (ImGui.BeginTable("DragoonMayCryJobSelectionTable", 2, ImGuiTableFlags.Resizable))
            {
                ImGui.TableSetupColumn("##LeftColumn", ImGuiTableColumnFlags.WidthFixed, topLeftSideHeight);
                ImGui.TableNextColumn();
                var regionSize = ImGui.GetContentRegionAvail();
                if (ImGui.BeginChild("##SelectableJobs", regionSize with { Y = topLeftSideHeight }, false, ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoScrollbar))
                {
                    DrawSelectables();
                }
                ImGui.EndChild();
                ImGui.TableNextColumn();
                if (ImGui.BeginChild("##SelecteJob", Vector2.Zero, false, ImGuiWindowFlags.NoDecoration))
                {
                    selected.Contents.Draw();
                }
                ImGui.EndChild();

                ImGui.EndTable();
            }
            
            
        }

        private void DrawSelectables()
        {
            if (ImGui.BeginListBox(""))
            {
                foreach (var item in selectableJobConfiguration)
                {
                    var headerHoveredColor = ImGui.GetStyle().Colors[(int)ImGuiCol.HeaderHovered];
                    var textSelectedColor = ImGui.GetStyle().Colors[(int)ImGuiCol.Header];
                    ImGui.PushStyleColor(ImGuiCol.HeaderHovered, headerHoveredColor with { W = 0.1f });
                    ImGui.PushStyleColor(ImGuiCol.Header, textSelectedColor with { W = 0.1f });
                    if (ImGui.Selectable($"##SelectableID{item.ID}", selected?.ID == item.ID))
                    {
                        selected = item;
                    }
                    ImGui.PopStyleColor();
                    ImGui.PopStyleColor();

                    ImGui.SameLine(3.0f);

                    item.DrawLabel();

                    ImGui.Spacing();
                }
                ImGui.EndListBox();
            }
        }

        

        public void Dispose()
        {

        }
    }
}
