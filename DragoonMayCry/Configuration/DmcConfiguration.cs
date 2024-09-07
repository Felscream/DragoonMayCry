using Dalamud.Configuration;
using KamiLib.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DragoonMayCry.Configuration
{
    internal class DmcConfiguration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;
        public int SfxVolume = 80;
        public bool PlaySoundEffects = true;
        public bool ForceSoundEffectsOnBlunder = false;
        public int PlaySfxEveryOccurrences = 3;
        public bool ApplyGameVolume = true;
        public bool ActiveOutsideInstance = false;

        public StyleRankUiConfiguration StyleRankUiConfiguration = new();


        // the below exist just to make saving less cumbersome
        public void Save()
        {
            Plugin.PluginInterface.SavePluginConfig(this);
        }

        public DmcConfigurationOne MigrateToOne()
        {
            var configOne = new DmcConfigurationOne();
            configOne.SfxVolume = new(SfxVolume);
            configOne.PlaySoundEffects = new(PlaySoundEffects);
            configOne.ForceSoundEffectsOnBlunder = new(ForceSoundEffectsOnBlunder);
            configOne.PlaySfxEveryOccurrences = new(PlaySfxEveryOccurrences);
            configOne.ApplyGameVolume = new(ApplyGameVolume);
            configOne.ActiveOutsideInstance = new(ActiveOutsideInstance);
            configOne.LockScoreWindow = new(StyleRankUiConfiguration.LockScoreWindow);
            return configOne;
        }
    }
}
