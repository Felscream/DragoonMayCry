using DragoonMayCry.Score.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DragoonMayCry.Audio.StyleAnnouncer.Announcer
{
    public interface IStyleAnnouncer
    {
        public Dictionary<SoundId, string> GetAnnouncerFilesById();
        public Dictionary<StyleType, IList<SoundId>> GetStyleAnnouncementVariations();
        public List<SoundId> GetBlunderVariations();
    }
}
