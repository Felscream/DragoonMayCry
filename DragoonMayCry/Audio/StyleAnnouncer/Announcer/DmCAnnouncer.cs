using DragoonMayCry.Score.Model;
using System.Collections.Generic;

namespace DragoonMayCry.Audio.StyleAnnouncer.Announcer
{
    internal class DmCAnnouncer : IStyleAnnouncer
    {
        private readonly Dictionary<SoundId, string> announcerFiles = new()
        {
            { SoundId.DeadWeight1, StyleAnnouncerService.GetPathToAnnouncerAudio("dead_weight1.ogg") },
            { SoundId.DeadWeight2, StyleAnnouncerService.GetPathToAnnouncerAudio("dead_weight2.ogg") },
            { SoundId.Dismal1, StyleAnnouncerService.GetPathToAnnouncerAudio("DmC/dirty.ogg") },
            { SoundId.Crazy1, StyleAnnouncerService.GetPathToAnnouncerAudio("DmC/cruel.ogg") },
            { SoundId.Badass1, StyleAnnouncerService.GetPathToAnnouncerAudio("DmC/brutal.ogg") },
            { SoundId.Apocalyptic1, StyleAnnouncerService.GetPathToAnnouncerAudio("DmC/anarchic.ogg") },
            { SoundId.Savage1, StyleAnnouncerService.GetPathToAnnouncerAudio("DmC/savage.ogg") },
            { SoundId.SickSkills1, StyleAnnouncerService.GetPathToAnnouncerAudio("DmC/sadistic.ogg") },
            { SoundId.SmokinSexyStyle1, StyleAnnouncerService.GetPathToAnnouncerAudio("DmC/sensational.ogg") },
        };

        private readonly List<SoundId> blunderVariations = new()
            { SoundId.DeadWeight1, SoundId.DeadWeight2 };

        private readonly Dictionary<StyleType, IList<SoundId>> styleAnnouncementVariation = new()
        {
            { StyleType.D, new List<SoundId> { SoundId.Dismal1 } },
            { StyleType.C, new List<SoundId> { SoundId.Crazy1 } },
            { StyleType.B, new List<SoundId> { SoundId.Badass1 } },
            { StyleType.A, new List<SoundId> { SoundId.Apocalyptic1 } },
            { StyleType.S, new List<SoundId> { SoundId.Savage1 } },
            { StyleType.SS, new List<SoundId> { SoundId.SickSkills1 } },
            { StyleType.SSS, new List<SoundId> { SoundId.SmokinSexyStyle1 } },
        };

        public Dictionary<SoundId, string> GetAnnouncerFilesById()
        {
            return announcerFiles;
        }

        public List<SoundId> GetBlunderVariations()
        {
            return blunderVariations;
        }

        public Dictionary<StyleType, IList<SoundId>> GetStyleAnnouncementVariations()
        {
            return styleAnnouncementVariation;
        }
    }
}
