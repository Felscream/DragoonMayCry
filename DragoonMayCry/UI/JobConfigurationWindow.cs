using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
using DragoonMayCry.Configuration;
using DragoonMayCry.Data;
using DragoonMayCry.Score.Style.Announcer;
using DragoonMayCry.State;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using ImGuiNET;
using KamiLib;
using KamiLib.Configuration;
using KamiLib.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public JobConfigurationWindow(DmcConfigurationOne configuration) : base("DragoonMayCry - Job configuration")
        {
            Size = new System.Numerics.Vector2(600, 320);
            SizeCondition = ImGuiCond.Appearing;

            this.configuration = configuration;
        }

        

        public override void Draw()
        {
            InfoBox.Instance
                .AddTitle("Announcer Preview")
                .AddConfigCombo(announcers, selectedAnnouncerPreview, GetAnnouncerTypeLabel, "", 150)
                .SameLine().AddIconButton("preview", Dalamud.Interface.FontAwesomeIcon.Play, () => Plugin.StyleAnnouncerService?.PlayRandomAnnouncerLine(selectedAnnouncerPreview.Value))
                .Draw();

            InfoBox.Instance
                .AddTitle("Job Configuration");
            var table = InfoBox.Instance.BeginTable();
            InfoBoxTableRow? row = null;
            foreach (var item in configuration.JobConfiguration.Select((entry, index) => new { index, entry}))
            {
                if(item.index % 2 == 0)
                {
                    row = table.BeginRow();
                }

                row?.AddAction(() =>
                {
                    ImGui.Text(item.entry.Key.ToString());
                    ImGui.Indent();
                    ImGui.BeginDisabled(PlayerState.GetInstance().IsInCombat);
                    ImGui.Text("Announcer");
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(150);
                    if (ImGui.BeginCombo($"##announcer-{item.entry.Key}", GetAnnouncerTypeLabel(item.entry.Value.Announcer.Value)))
                    {
                        for (int i = 0; i < announcers.Count(); i++)
                        {
                            if (ImGui.Selectable(GetAnnouncerTypeLabel(announcers[i]), item.entry.Value.Announcer.Value.Equals(announcers[i])))
                            {
                                configuration.JobConfiguration[item.entry.Key].Announcer.Value = announcers[i];
                                KamiCommon.SaveConfiguration();
                                JobAnnouncerTypeChange?.Invoke(this, new(announcers[i], item.entry.Key));
                            }
                        }
                        ImGui.EndCombo();
                    }
                    ImGui.EndDisabled();
                    ImGui.BeginDisabled(PlayerState.GetInstance().IsInsideInstance);
                    ImGui.Text("Dynamic BGM");
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(130);
                    if (ImGui.BeginCombo($"##bgm-{item.entry.Key}", GetBgmLabel(item.entry.Value.Bgm.Value)))
                    {
                        for (int i = 0; i < bgmOptions.Count(); i++)
                        {
                            if (ImGui.Selectable(GetBgmLabel(bgmOptions[i]), item.entry.Value.Bgm.Value.Equals(bgmOptions[i])))
                            {
                                configuration.JobConfiguration[item.entry.Key].Bgm.Value = bgmOptions[i];
                                KamiCommon.SaveConfiguration();
                            }
                        }
                        ImGui.EndCombo();
                    }
                    ImGui.EndDisabled();
                    ImGui.Unindent();
                });

                if (item.index % 2 != 0 || item.index == configuration.JobConfiguration.Count - 1)
                {
                    row?.EndRow();
                }
            }
            table.EndTable();
            InfoBox.Instance.Draw();
        }

        private static string GetAnnouncerTypeLabel(AnnouncerType type)
        {
            return type switch
            {
                AnnouncerType.DmC => "DmC: Devil May Cry",
                AnnouncerType.DmC5 => "Devil May Cry 5",
                AnnouncerType.DmC5Balrog => "Devil May Cry 5 / Balrog VA",
                AnnouncerType.Nico => "Nico",
                AnnouncerType.Morrison => "Morrison",
                _ => "Unknown"
            };
        }

        private static string GetBgmLabel(JobConfiguration.BgmConfiguration bgm)
        {
            return bgm switch
            {
                JobConfiguration.BgmConfiguration.Off => "Off",
                JobConfiguration.BgmConfiguration.BuryTheLight => "Bury the Light",
                JobConfiguration.BgmConfiguration.DevilTrigger => "Devil Trigger",
                JobConfiguration.BgmConfiguration.Randomize => "Randomize",
                _ => "Unknown"
            };
        }

        public void Dispose()
        {

        }
    }
}
