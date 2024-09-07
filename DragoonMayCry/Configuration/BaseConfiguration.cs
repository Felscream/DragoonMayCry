using Dalamud.Configuration;
using KamiLib.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DragoonMayCry.Configuration
{
    public class BaseConfiguration : IPluginConfiguration
    {
        public int Version { get; set; } = -1;
    }
}
