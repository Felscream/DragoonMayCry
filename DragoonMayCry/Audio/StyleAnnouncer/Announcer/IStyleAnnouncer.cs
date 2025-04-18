using DragoonMayCry.Score.Model;
using System.Collections.Generic;

namespace DragoonMayCry.Audio.StyleAnnouncer.Announcer
{
    public interface IStyleAnnouncer
    {
        public Dictionary<SoundId, string> GetAnnouncerFilesById();
        public Dictionary<StyleType, IList<SoundId>> GetStyleAnnouncementVariations();
        public List<SoundId> GetBlunderVariations();
    }
}
