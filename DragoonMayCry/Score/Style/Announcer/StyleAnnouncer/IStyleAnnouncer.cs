using DragoonMayCry.Audio;
using DragoonMayCry.Score.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DragoonMayCry.Score.Style.Announcer.StyleAnnouncer
{
    public interface IStyleAnnouncer
    {
        public Dictionary<SoundId, string> GetAnnouncerFilesById();
        public Dictionary<StyleType, IList<SoundId>> GetStyleAnnouncementVariations();
        public List<SoundId> GetBlunderVariations();
    }
}
