using DragoonMayCry.Audio.BGM;
using DragoonMayCry.Audio.StyleAnnouncer;
using DragoonMayCry.Configuration;
using DragoonMayCry.Data;
using DragoonMayCry.State;
using ImGuiNET;
using KamiLib;
using KamiLib.Drawing;
using KamiLib.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace DragoonMayCry.UI
{
    internal class SelectedJobConfiguration : ISelectable, IDrawable
    {
        public delegate void JobAnnouncerChange(JobIds job, AnnouncerType announcer);
        public delegate void DmcToggleChange(JobIds job);
        public delegate void ApplyToAll(bool enabled, AnnouncerType announcer, JobConfiguration.BgmConfiguration bgm);
        public JobAnnouncerChange jobAnnouncerChange;
        public DmcToggleChange dmcToggleChange;
        public ApplyToAll applyToAll;
        private readonly JobIds job;
        private readonly JobConfiguration configuration;
        private readonly IList<AnnouncerType> announcers;
        private readonly IList<JobConfiguration.BgmConfiguration> bgms;
        public SelectedJobConfiguration(JobIds job, JobConfiguration configuration, IList<AnnouncerType> announcers, IList<JobConfiguration.BgmConfiguration> bgms)
        {
            this.job = job;
            this.configuration = configuration;
            this.announcers = announcers;
            this.bgms = [.. bgms.OrderBy(bgm =>
            {
                if (bgm == JobConfiguration.BgmConfiguration.Off)
                {
                    return 0;
                }
                if (bgm == JobConfiguration.BgmConfiguration.Randomize)
                {
                    return 6;
                }
                return (int)bgm;
            })];
        }

        IDrawable ISelectable.Contents => this;

        string ISelectable.ID => job.ToString();

        private Vector4 GetJobSelectionItemColor()
        {
            if (!configuration.EnableDmc)
            {
                return Colors.Grey;
            }

            if (configuration.EstinienMustDie)
            {
                return Colors.SoftRed;
            }

            return Colors.White;
        }

        void IDrawable.Draw()
        {
            InfoBox.Instance.AddTitle(job.ToString())
                .AddAction(() =>
                {
                    var cursorPos = ImGui.GetCursorPos();
                    var enabled = configuration.EnableDmc.Value;
                    if (ImGui.Checkbox("", ref enabled))
                    {
                        configuration.EnableDmc.Value = enabled;
                        KamiCommon.SaveConfiguration();
                        dmcToggleChange?.Invoke(job);
                    }
                    ConfigWindow.AddLabel("Enable DragoonMayCry", cursorPos);
                })
                .BeginDisabled(PlayerState.GetInstance().IsInCombat)
                .AddConfigCheckbox($"Estinien Must Die", configuration.EstinienMustDie, "You have no leeway", $"EMD-{job}")
                .AddAction(() =>
                {
                    ImGui.SetNextItemWidth(200f);
                    if (ImGui.BeginCombo($"Announcer##announcer-{job}", StyleAnnouncerService.GetAnnouncerTypeLabel(configuration.Announcer.Value)))
                    {
                        for (var i = 0; i < announcers.Count(); i++)
                        {
                            if (ImGui.Selectable(StyleAnnouncerService.GetAnnouncerTypeLabel(announcers[i]), configuration.Announcer.Value.Equals(announcers[i])))
                            {
                                configuration.Announcer.Value = announcers[i];
                                KamiCommon.SaveConfiguration();
                                jobAnnouncerChange?.Invoke(job, announcers[i]);
                            }
                        }
                        ImGui.EndCombo();
                    }
                })

                .EndDisabled()
                .BeginDisabled(PlayerState.GetInstance().IsInsideInstance)
                .AddConfigCombo(bgms, configuration.Bgm, DynamicBgmService.GetBgmLabel, $"Dynamic BGM##bgm-{job}", 200f)
                .AddButton("Apply to all jobs", () =>
                {
                    applyToAll?.Invoke(configuration.EnableDmc.Value, configuration.Announcer.Value, configuration.Bgm.Value);
                })
                .EndDisabled()
                .Draw();
        }

        void ISelectable.DrawLabel()
        {
            ImGui.PushStyleColor(ImGuiCol.Text, GetJobSelectionItemColor());
            ImGui.Text(job.ToString());
            ImGui.PopStyleColor();
        }
    }
}
