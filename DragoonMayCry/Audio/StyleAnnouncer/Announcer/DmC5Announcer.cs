using DragoonMayCry.Score.Model;
using System.Collections.Generic;

namespace DragoonMayCry.Audio.StyleAnnouncer.Announcer
{
    internal class DmC5Announcer : IStyleAnnouncer
    {
        private readonly Dictionary<SoundId, string> announcerFiles = new()
        {
            { SoundId.DeadWeight1, StyleAnnouncerService.GetPathToAnnouncerAudio("dead_weight1.ogg") },
            { SoundId.DeadWeight2, StyleAnnouncerService.GetPathToAnnouncerAudio("dead_weight2.ogg") },
            { SoundId.Dismal1, StyleAnnouncerService.GetPathToAnnouncerAudio("DmC5/dismal.ogg") },
            { SoundId.Dismal2, StyleAnnouncerService.GetPathToAnnouncerAudio("DmC5/dismal2.ogg") },
            { SoundId.Crazy1, StyleAnnouncerService.GetPathToAnnouncerAudio("DmC5/crazy.ogg") },
            { SoundId.Crazy2, StyleAnnouncerService.GetPathToAnnouncerAudio("DmC5/crazy2.ogg") },
            { SoundId.Badass1, StyleAnnouncerService.GetPathToAnnouncerAudio("DmC5/badass.ogg") },
            { SoundId.Badass2, StyleAnnouncerService.GetPathToAnnouncerAudio("DmC5/badass2.ogg") },
            { SoundId.Apocalyptic1, StyleAnnouncerService.GetPathToAnnouncerAudio("DmC5/apocalyptic.ogg") },
            { SoundId.Apocalyptic2, StyleAnnouncerService.GetPathToAnnouncerAudio("DmC5/apocalyptic2.ogg") },
            { SoundId.Savage1, StyleAnnouncerService.GetPathToAnnouncerAudio("DmC5/savage.ogg") },
            { SoundId.Savage2, StyleAnnouncerService.GetPathToAnnouncerAudio("DmC5/savage2.ogg") },
            { SoundId.SickSkills1, StyleAnnouncerService.GetPathToAnnouncerAudio("DmC5/sick_skills.ogg") },
            { SoundId.SickSkills2, StyleAnnouncerService.GetPathToAnnouncerAudio("DmC5/sick_skills2.ogg") },
            { SoundId.SmokinSexyStyle1, StyleAnnouncerService.GetPathToAnnouncerAudio("DmC5/smokin_sexy_style.ogg") },
            { SoundId.SmokinSexyStyle2, StyleAnnouncerService.GetPathToAnnouncerAudio("DmC5/smokin_sexy_style2.ogg") },
        };

        private readonly List<SoundId> blunderVariations = new()
            { SoundId.DeadWeight1, SoundId.DeadWeight2 };

        private readonly Dictionary<StyleType, IList<SoundId>> styleAnnouncementVariation = new()
        {
            { StyleType.D, new List<SoundId> { SoundId.Dismal1, SoundId.Dismal2 } },
            { StyleType.C, new List<SoundId> { SoundId.Crazy1, SoundId.Crazy2 } },
            { StyleType.B, new List<SoundId> { SoundId.Badass1, SoundId.Badass2 } },
            { StyleType.A, new List<SoundId> { SoundId.Apocalyptic1, SoundId.Apocalyptic2 } },
            { StyleType.S, new List<SoundId> { SoundId.Savage1, SoundId.Savage2 } },
            { StyleType.SS, new List<SoundId> { SoundId.SickSkills1, SoundId.SickSkills2 } },
            { StyleType.SSS, new List<SoundId> { SoundId.SmokinSexyStyle1, SoundId.SmokinSexyStyle2 } },
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
