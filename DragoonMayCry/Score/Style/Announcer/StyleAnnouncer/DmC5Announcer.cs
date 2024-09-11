using DragoonMayCry.Audio;
using DragoonMayCry.Score.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace DragoonMayCry.Score.Style.Announcer.StyleAnnouncer
{
    internal class DmC5Announcer : IStyleAnnouncer
    {
        private readonly Dictionary<SoundId, string> announcerFiles = new Dictionary<SoundId, string>
            {
                { SoundId.DeadWeight1, StyleAnnouncerService.GetPathToAnnouncerAudio("dead_weight1.ogg") },
                { SoundId.DeadWeight2, StyleAnnouncerService.GetPathToAnnouncerAudio("dead_weight2.ogg") },
                { SoundId.Dismal1, StyleAnnouncerService.GetPathToAnnouncerAudio("DmC5/dismal.ogg") },
                { SoundId.Crazy1, StyleAnnouncerService.GetPathToAnnouncerAudio("DmC5/crazy.ogg") },
                { SoundId.Badass1, StyleAnnouncerService.GetPathToAnnouncerAudio("DmC5/badass.ogg") },
                { SoundId.Apocalyptic1, StyleAnnouncerService.GetPathToAnnouncerAudio("DmC5/apocalyptic.ogg") },
                { SoundId.Savage1, StyleAnnouncerService.GetPathToAnnouncerAudio("DmC5/savage.ogg") },
                { SoundId.SickSkills1, StyleAnnouncerService.GetPathToAnnouncerAudio("DmC5/sick_skills.ogg") },
                { SoundId.SmokinSexyStyle1, StyleAnnouncerService.GetPathToAnnouncerAudio("DmC5/smokin_sexy_style.ogg") }
            };

        private readonly List<SoundId> blunderVariations = new List<SoundId> { SoundId.DeadWeight1, SoundId.DeadWeight2 };

        private readonly Dictionary<StyleType, IList<SoundId>> styleAnnouncementVariation = new Dictionary<StyleType, IList<SoundId>> {
            { StyleType.D, new List<SoundId>{ SoundId.Dismal1 } },
            { StyleType.C, new List<SoundId>{ SoundId.Crazy1 } },
            { StyleType.B, new List<SoundId>{ SoundId.Badass1 } },
            { StyleType.A, new List<SoundId>{ SoundId.Apocalyptic1 } },
            { StyleType.S, new List<SoundId>{ SoundId.Savage1 } },
            { StyleType.SS, new List<SoundId>{ SoundId.SickSkills1 } },
            { StyleType.SSS, new List<SoundId>{ SoundId.SmokinSexyStyle1 } },
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
