using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using DragoonMayCry.Audio.StyleAnnouncer;
using DragoonMayCry.Configuration;
using DragoonMayCry.Data;
using ImGuiNET;
using KamiLib;
using KamiLib.Configuration;
using KamiLib.Drawing;
using KamiLib.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

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
        public EventHandler<JobIds>? EnabledForJobChange;
        private readonly DmcConfigurationOne configuration;
        private readonly IList<AnnouncerType> announcers = Enum.GetValues(typeof(AnnouncerType)).Cast<AnnouncerType>().ToList();
        private readonly IList<JobConfiguration.BgmConfiguration> bgmOptions = Enum.GetValues(typeof(JobConfiguration.BgmConfiguration)).Cast<JobConfiguration.BgmConfiguration>().ToList();
        private readonly Setting<AnnouncerType> selectedAnnouncerPreview = new(AnnouncerType.DmC5);
        private ISelectable selected;
        private readonly IList<ISelectable> selectableJobConfiguration = new List<ISelectable>();
        public JobConfigurationWindow(DmcConfigurationOne configuration) : base("DragoonMayCry - Job configuration##DmCJobConfiguration", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
        {
            Size = new Vector2(650f, 300);
            SizeCondition = ImGuiCond.Appearing;

            this.configuration = configuration;
            foreach (var entry in configuration.JobConfiguration)
            {
                var jobSelection = new SelectedJobConfiguration(entry.Key, entry.Value, announcers, bgmOptions);
                jobSelection.jobAnnouncerChange = JobAnnouncerChange;
                jobSelection.dmcToggleChange = DmcEnabledForJobChange;
                jobSelection.applyToAll = ApplyToAll;
                selectableJobConfiguration.Add(jobSelection);
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
                if (ImGui.BeginChild("##SelectableJobs", regionSize with { Y = topLeftSideHeight }, false, ImGuiWindowFlags.NoDecoration))
                {
                    DrawSelectables();
                }
                ImGui.EndChild();
                ImGui.TableNextColumn();
                if (ImGui.BeginChild("##SelectedJob", Vector2.Zero, false, ImGuiWindowFlags.NoDecoration))
                {
                    selected.Contents.Draw();
                }
                ImGui.EndChild();

                ImGui.EndTable();
            }
        }

        private void ApplyToAll(JobConfiguration targetConfiguration)
        {
            foreach (var entry in configuration.JobConfiguration)
            {
                entry.Value.Announcer = new(targetConfiguration.Announcer.Value);
                entry.Value.Bgm = new(targetConfiguration.Bgm.Value);
                entry.Value.GcdDropThreshold = new(targetConfiguration.GcdDropThreshold.Value);
            }
            KamiCommon.SaveConfiguration();
        }

        private void JobAnnouncerChange(JobIds job, AnnouncerType announcer)
        {
            JobAnnouncerTypeChange?.Invoke(this, new(announcer, job));
        }

        private void DmcEnabledForJobChange(JobIds job)
        {
            EnabledForJobChange?.Invoke(this, job);
        }

        private void DrawSelectables()
        {
            if (ImGui.BeginListBox("", new Vector2(-1, -1)))
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
