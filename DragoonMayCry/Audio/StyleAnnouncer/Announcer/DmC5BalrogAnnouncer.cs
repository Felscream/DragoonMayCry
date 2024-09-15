using DragoonMayCry.Score.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace DragoonMayCry.Audio.StyleAnnouncer.Announcer
{
    internal class DmC5BalrogAnnouncer : IStyleAnnouncer
    {
        private readonly Dictionary<SoundId, string> announcerFiles = new Dictionary<SoundId, string>
            {
                { SoundId.DeadWeight1, StyleAnnouncerService.GetPathToAnnouncerAudio("dead_weight1.ogg") },
                { SoundId.DeadWeight2, StyleAnnouncerService.GetPathToAnnouncerAudio("dead_weight2.ogg") },
                { SoundId.Crazy1, StyleAnnouncerService.GetPathToAnnouncerAudio("Balrog/crazy.ogg") },
                { SoundId.Crazy2, StyleAnnouncerService.GetPathToAnnouncerAudio("Balrog/crazy2.ogg") },
                { SoundId.Badass1, StyleAnnouncerService.GetPathToAnnouncerAudio("Balrog/badass.ogg") },
                { SoundId.Badass2, StyleAnnouncerService.GetPathToAnnouncerAudio("Balrog/badass2.ogg") },
                { SoundId.Apocalyptic1, StyleAnnouncerService.GetPathToAnnouncerAudio("Balrog/apocalyptic.ogg") },
                { SoundId.Apocalyptic2, StyleAnnouncerService.GetPathToAnnouncerAudio("Balrog/apocalyptic2.ogg") },
                { SoundId.Savage1, StyleAnnouncerService.GetPathToAnnouncerAudio("Balrog/savage.ogg") },
                { SoundId.Savage2, StyleAnnouncerService.GetPathToAnnouncerAudio("Balrog/savage2.ogg") },
                { SoundId.SickSkills1, StyleAnnouncerService.GetPathToAnnouncerAudio("Balrog/sick_skills.ogg") },
                { SoundId.SickSkills2, StyleAnnouncerService.GetPathToAnnouncerAudio("Balrog/sick_skills2.ogg") },
                { SoundId.SmokinSexyStyle1, StyleAnnouncerService.GetPathToAnnouncerAudio("Balrog/smokin_sexy_style.ogg") },
                { SoundId.SmokinSexyStyle2, StyleAnnouncerService.GetPathToAnnouncerAudio("Balrog/smokin_sexy_style2.ogg") }
            };

        private readonly List<SoundId> blunderVariations = new List<SoundId> { SoundId.DeadWeight1, SoundId.DeadWeight2 };

        private readonly Dictionary<StyleType, IList<SoundId>> styleAnnouncementVariation = new Dictionary<StyleType, IList<SoundId>> {
            { StyleType.C, new List<SoundId>{ SoundId.Crazy1, SoundId.Crazy2 } },
            { StyleType.B, new List<SoundId>{ SoundId.Badass1, SoundId.Badass2 } },
            { StyleType.A, new List<SoundId>{ SoundId.Apocalyptic1, SoundId.Apocalyptic2 } },
            { StyleType.S, new List<SoundId>{ SoundId.Savage1, SoundId.Savage2 } },
            { StyleType.SS, new List<SoundId>{ SoundId.SickSkills1, SoundId.SickSkills2 } },
            { StyleType.SSS, new List<SoundId>{ SoundId.SmokinSexyStyle1, SoundId.SmokinSexyStyle2 } },
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
