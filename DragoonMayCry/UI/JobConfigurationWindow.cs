#region

using Dalamud.Interface;
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

#endregion

namespace DragoonMayCry.UI
{
    public class JobConfigurationWindow : Window, IDisposable
    {

        private readonly IList<AnnouncerType> announcers =
            Enum.GetValues(typeof(AnnouncerType)).Cast<AnnouncerType>().ToList();

        private readonly IList<AnnouncerType> announcersPreview =
            Enum.GetValues(typeof(AnnouncerType)).Cast<AnnouncerType>()
                .Where(announcer => announcer != AnnouncerType.Randomize).ToList();

        private readonly IList<JobConfiguration.BgmConfiguration> bgmOptions = Enum
                                                                               .GetValues(
                                                                                   typeof(JobConfiguration.
                                                                                       BgmConfiguration))
                                                                               .Cast<
                                                                                   JobConfiguration.BgmConfiguration>()
                                                                               .ToList();
        private readonly DmcConfiguration configuration;
        private readonly IList<ISelectable> selectableJobConfiguration = new List<ISelectable>();

        private readonly Setting<AnnouncerType> selectedAnnouncerPreview = new(AnnouncerType.DmC5);
        public EventHandler<JobId>? EnabledForJobChange;

        public EventHandler<JobAnnouncerType>? JobAnnouncerTypeChange;
        private ISelectable selected;

        public JobConfigurationWindow(DmcConfiguration configuration) : base(
            "DragoonMayCry - Job configuration##DmCJobConfiguration",
            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
        {
            Size = new Vector2(675, 375f);
            SizeCondition = ImGuiCond.Appearing;

            this.configuration = configuration;
            foreach (var entry in configuration.JobConfiguration)
            {
                var jobSelection = new SelectedJobConfiguration(entry.Key, entry.Value, announcers, bgmOptions);
                jobSelection.JobAnnouncerChange = JobAnnouncerChange;
                jobSelection.DmcToggleChange = DmcEnabledForJobChange;
                jobSelection.ApplyToAll = ApplyToAll;
                selectableJobConfiguration.Add(jobSelection);
            }

            selected = selectableJobConfiguration[0];
        }

        public void Dispose() { }

        public override void Draw()
        {
            InfoBox.Instance
                   .AddTitle("Announcer Preview")
                   .AddConfigCombo(announcersPreview, selectedAnnouncerPreview,
                                   StyleAnnouncerService.GetAnnouncerTypeLabel,
                                   "##", 150)
                   .SameLine().AddIconButton("preview", FontAwesomeIcon.Play,
                                             () => Plugin.StyleAnnouncerService?.PlayRandomAnnouncerLine(
                                                 selectedAnnouncerPreview.Value))
                   .Draw();

            var region = ImGui.GetContentRegionAvail();
            var itemSpacing = ImGui.GetStyle().ItemSpacing;

            var topLeftSideHeight = region.Y * ImGuiHelpers.GlobalScale - itemSpacing.Y / 2.0f;

            if (ImGui.BeginTable("DragoonMayCryJobSelectionTable", 2, ImGuiTableFlags.Resizable))
            {
                ImGui.TableSetupColumn("##LeftColumn", ImGuiTableColumnFlags.WidthFixed, topLeftSideHeight);
                ImGui.TableNextColumn();
                var regionSize = ImGui.GetContentRegionAvail();
                if (ImGui.BeginChild("##SelectableJobs", regionSize with { Y = topLeftSideHeight }, false,
                                     ImGuiWindowFlags.NoDecoration))
                {
                    DrawSelectables();
                    ImGui.EndChild();
                }

                ImGui.TableNextColumn();
                if (ImGui.BeginChild("##SelectedJob", Vector2.Zero, false))
                {
                    selected.Contents.Draw();
                    ImGui.EndChild();
                }
                ImGui.EndTable();
            }
        }

        private void ApplyToAll(JobConfiguration targetConfiguration)
        {
            foreach (var entry in configuration.JobConfiguration)
            {
                entry.Value.Announcer = new Setting<AnnouncerType>(targetConfiguration.Announcer.Value);
                entry.Value.RandomizeAnnouncement = new Setting<bool>(targetConfiguration.RandomizeAnnouncement);
                entry.Value.Bgm = new Setting<JobConfiguration.BgmConfiguration>(targetConfiguration.Bgm.Value);
                entry.Value.GcdDropThreshold = new Setting<float>(targetConfiguration.GcdDropThreshold.Value);
                entry.Value.ScoreMultiplier = new Setting<float>(targetConfiguration.ScoreMultiplier.Value);
                entry.Value.BgmRandomSelection =
                    new Setting<HashSet<long>>(targetConfiguration.BgmRandomSelection.Value);
            }

            KamiCommon.SaveConfiguration();
        }

        private void JobAnnouncerChange(JobId job, AnnouncerType announcer)
        {
            JobAnnouncerTypeChange?.Invoke(this, new JobAnnouncerType(announcer, job));
        }

        private void DmcEnabledForJobChange(JobId job)
        {
            EnabledForJobChange?.Invoke(this, job);
        }

        private void DrawSelectables()
        {
            if (ImGui.BeginListBox("##", new Vector2(-1, -1)))
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

        public struct JobAnnouncerType(AnnouncerType type, JobId job)
        {
            public readonly AnnouncerType Type = type;
            public readonly JobId Job = job;

        }
    }
}
