using Dalamud.Configuration;

namespace DragoonMayCry.Configuration
{
    public class BaseConfiguration : IPluginConfiguration
    {
        public int Version { get; set; } = -1;
    }
}
