using DragoonMayCry.Score.Style.Announcer;
using KamiLib.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            Randomize
        }
        public Setting<AnnouncerType> Announcer = new (AnnouncerType.DmC5);
        public Setting<BgmConfiguration> Bgm = new Setting<BgmConfiguration>(BgmConfiguration.Off);
    }
}
