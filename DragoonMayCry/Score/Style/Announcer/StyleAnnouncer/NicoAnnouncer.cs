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
    internal class NicoAnnouncer : IStyleAnnouncer
    {
        private readonly Dictionary<SoundId, string> announcerFiles = new Dictionary<SoundId, string>
            {
                { SoundId.DeadWeight1, StyleAnnouncerService.GetPathToAnnouncerAudio("Nico/loser.ogg") },
                { SoundId.DeadWeight2, StyleAnnouncerService.GetPathToAnnouncerAudio("Nico/dumbass.ogg") },
                { SoundId.DeadWeight3, StyleAnnouncerService.GetPathToAnnouncerAudio("Nico/dude.ogg") },
                { SoundId.DeadWeight4, StyleAnnouncerService.GetPathToAnnouncerAudio("Nico/for_real.ogg") },
                { SoundId.DeadWeight5, StyleAnnouncerService.GetPathToAnnouncerAudio("Nico/ew.ogg") },
                { SoundId.Badass1, StyleAnnouncerService.GetPathToAnnouncerAudio("Nico/baller.ogg") },
                { SoundId.Badass2, StyleAnnouncerService.GetPathToAnnouncerAudio("Nico/not_bad.ogg") },
                { SoundId.Apocalyptic1, StyleAnnouncerService.GetPathToAnnouncerAudio("Nico/yeah.ogg") },
                { SoundId.Apocalyptic2, StyleAnnouncerService.GetPathToAnnouncerAudio("Nico/wow.ogg") },
                { SoundId.Savage1, StyleAnnouncerService.GetPathToAnnouncerAudio("Nico/sassy.ogg") },
                { SoundId.Savage2, StyleAnnouncerService.GetPathToAnnouncerAudio("Nico/trippin.ogg") },
                { SoundId.SickSkills1, StyleAnnouncerService.GetPathToAnnouncerAudio("Nico/super_sexy.ogg") },
                { SoundId.SickSkills2, StyleAnnouncerService.GetPathToAnnouncerAudio("Nico/groovy.ogg") },
                { SoundId.SmokinSexyStyle1, StyleAnnouncerService.GetPathToAnnouncerAudio("Nico/super_sassy_sexy.ogg") },
                { SoundId.SmokinSexyStyle2, StyleAnnouncerService.GetPathToAnnouncerAudio("Nico/hell_of_a_strike.ogg") }
            };

        private readonly List<SoundId> blunderVariations = new List<SoundId> { SoundId.DeadWeight1, SoundId.DeadWeight2, SoundId.DeadWeight3, SoundId.DeadWeight4, SoundId.DeadWeight5 };

        private readonly Dictionary<StyleType, IList<SoundId>> styleAnnouncementVariation = new Dictionary<StyleType, IList<SoundId>> {
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
