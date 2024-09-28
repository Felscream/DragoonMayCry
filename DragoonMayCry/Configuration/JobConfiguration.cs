using DragoonMayCry.Audio.StyleAnnouncer;
using KamiLib.Configuration;

namespace DragoonMayCry.Configuration
{
    public class JobConfiguration
    {
        public enum BgmConfiguration
        {
            Off,
            BuryTheLight,
            DevilTrigger,
            CrimsonCloud,
            Subhuman,
            Randomize
        }
        public Setting<bool> EnableDmc = new(true);
        public Setting<AnnouncerType> Announcer = new(AnnouncerType.DmC5);
        public Setting<BgmConfiguration> Bgm = new(BgmConfiguration.Off);
    }
}
