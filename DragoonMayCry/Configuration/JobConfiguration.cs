using DragoonMayCry.Audio.StyleAnnouncer;
using KamiLib.Configuration;

namespace DragoonMayCry.Configuration
{
    public class JobConfiguration
    {
        public enum BgmConfiguration
        {
            Off = 0,
            BuryTheLight = 1,
            DevilTrigger = 2,
            CrimsonCloud = 3,
            Subhuman = 5,
            Randomize = 4
        }
        public Setting<bool> EnableDmc = new(true);
        public Setting<AnnouncerType> Announcer = new(AnnouncerType.DmC5);
        public Setting<BgmConfiguration> Bgm = new(BgmConfiguration.Off);
    }
}
