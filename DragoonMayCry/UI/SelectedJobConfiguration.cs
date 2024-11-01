using Dalamud.Interface.Components;
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
        public delegate void JobAnnouncerChangeDelegate(JobId job, AnnouncerType announcer);

        public delegate void DmcToggleChangeDelegate(JobId job);

        public delegate void ApplyToAllDelegate(JobConfiguration configuration);

        public JobAnnouncerChangeDelegate? JobAnnouncerChange;
        public DmcToggleChangeDelegate? DmcToggleChange;
        public ApplyToAllDelegate? ApplyToAll;
        private readonly JobId job;
        private readonly JobConfiguration configuration;
        private readonly IList<AnnouncerType> announcers;
        private readonly IList<JobConfiguration.BgmConfiguration> bgms;

        public SelectedJobConfiguration(
            JobId job, JobConfiguration configuration, IList<AnnouncerType> announcers,
            IList<JobConfiguration.BgmConfiguration> bgms)
        {
            this.job = job;
            this.configuration = configuration;
            this.announcers = announcers;
            this.bgms =
            [
                .. bgms.OrderBy(bgm =>
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
                })
            ];
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
                           DmcToggleChange?.Invoke(job);
                       }

                       ConfigWindow.AddLabel("Enable DragoonMayCry", cursorPos);
                   })
                   .BeginDisabled(PlayerState.GetInstance().IsInCombat)
                   .AddConfigCheckbox($"Estinien Must Die", configuration.EstinienMustDie, "You have no leeway",
                                      $"EMD-{job}")
                   .BeginDisabled(configuration.EstinienMustDie)
                   .AddAction(() =>
                   {
                       var curPos = ImGui.GetCursorPos();
                       ImGui.SetNextItemWidth(200f);

                       if (ImGui.InputFloat("GCD clip / drop threshold", ref configuration.GcdDropThreshold.Value,
                                            0.01f, 0.1f))
                       {
                           configuration.GcdDropThreshold.Value =
                               Math.Min(1, Math.Max(0, configuration.GcdDropThreshold.Value));
                           KamiCommon.SaveConfiguration();
                       }

                       ImGui.EndDisabled();
                       ImGuiComponents.HelpMarker("In seconds.\nOnly used if 'Estinien Must Die' is disabled");
                   })
                   .AddAction(() =>
                   {
                       ImGui.SetNextItemWidth(200f);
                       if (ImGui.BeginCombo($"Announcer##announcer-{job}",
                                            StyleAnnouncerService.GetAnnouncerTypeLabel(configuration.Announcer.Value)))
                       {
                           for (var i = 0; i < announcers.Count(); i++)
                           {
                               if (ImGui.Selectable(StyleAnnouncerService.GetAnnouncerTypeLabel(announcers[i]),
                                                    configuration.Announcer.Value.Equals(announcers[i])))
                               {
                                   configuration.Announcer.Value = announcers[i];
                                   KamiCommon.SaveConfiguration();
                                   JobAnnouncerChange?.Invoke(job, announcers[i]);
                               }
                           }

                           ImGui.EndCombo();
                       }

                       if (configuration.Announcer == AnnouncerType.Randomize)
                       {
                           ImGuiComponents.HelpMarker("Randomized on instance change");
                       }
                   })
                   .StartConditional(configuration.Announcer == AnnouncerType.Randomize)
                   .AddConfigCheckbox("Randomize for each announcement", configuration.RandomizeAnnouncement)
                   .EndConditional()
                   .EndDisabled()
                   .BeginDisabled(PlayerState.GetInstance().IsInsideInstance || PlayerState.GetInstance().IsInCombat)
                   .AddConfigCombo(bgms, configuration.Bgm, DynamicBgmService.GetBgmLabel, $"Dynamic BGM##bgm-{job}",
                                   200f)
                   .StartConditional(configuration.Bgm == JobConfiguration.BgmConfiguration.Randomize)
                   .SameLine()
                   .AddHelpMarker("Randomized at the end of combat")
                   .EndConditional()
                   .AddButton("Apply to all jobs", () => { ApplyToAll?.Invoke(configuration); })
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
