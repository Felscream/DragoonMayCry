using DragoonMayCry.Audio.BGM;
using DragoonMayCry.Audio.StyleAnnouncer;
using DragoonMayCry.Configuration;
using DragoonMayCry.Data;
using DragoonMayCry.State;
using DragoonMayCry.Util;
using ImGuiNET;
using KamiLib;
using KamiLib.Drawing;
using KamiLib.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace DragoonMayCry.UI
{
    internal class JobSelection : ISelectable, IDrawable
    {
        public delegate void JobAnnouncerChange(JobIds job, AnnouncerType announcer);
        public delegate void JobBgmChange(JobIds job, JobConfiguration.BgmConfiguration bgm);
        public JobAnnouncerChange jobAnnouncerChange;
        private readonly JobIds job;
        private readonly JobConfiguration configuration;
        private readonly IList<AnnouncerType> announcers;
        private readonly IList<JobConfiguration.BgmConfiguration> bgms;
        public JobSelection(JobIds job, JobConfiguration configuration, IList<AnnouncerType> announcers, IList<JobConfiguration.BgmConfiguration> bgms)
        {
            this.job = job;
            this.configuration = configuration;
            this.announcers = announcers;
            this.bgms = bgms;
        }

        IDrawable ISelectable.Contents => this;

        string ISelectable.ID => job.ToString();

        private Vector4 GetJobColor() {
            if (!configuration.EnableDmc)
            {
                return Colors.Grey;
            }

            return Colors.White;
        }

        void IDrawable.Draw()
        {
            InfoBox.Instance.AddTitle(job.ToString())
                .AddConfigCheckbox("Enable DragoonMayCry", configuration.EnableDmc)
                .AddString("Announcer")
                .SameLine()
                .BeginDisabled(PlayerState.GetInstance().IsInCombat)
                .AddAction(() =>
                {
                    if (ImGui.BeginCombo($"##announcer-{job}", StyleAnnouncerService.GetAnnouncerTypeLabel(configuration.Announcer.Value)))
                    {
                        for (int i = 0; i < announcers.Count(); i++)
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
                .AddString("Dynamic BGM")
                .SameLine()
                .BeginDisabled(PlayerState.GetInstance().IsInsideInstance)
                .AddConfigCombo(bgms, configuration.Bgm, DynamicBgmService.GetBgmLabel, "", 150f)
                .EndDisabled()
                .Draw();
        }

        void ISelectable.DrawLabel()
        {
            ImGui.PushStyleColor(ImGuiCol.Text, GetJobColor());
            ImGui.Text(job.ToString());
            ImGui.PopStyleColor();
        }
    }
}
