using DragoonMayCry.Audio;
using DragoonMayCry.Score.Action;
using DragoonMayCry.Score.Model;
using DragoonMayCry.Score.Style.Announcer.StyleAnnouncer;
using DragoonMayCry.Score.Style.Rank;
using DragoonMayCry.State;
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DragoonMayCry.Score.Style.Announcer
{
    public class StyleAnnouncerService
    {
        private readonly AudioService audioService;
        private readonly StyleRankHandler rankHandler;
        private readonly IStyleAnnouncer dmcAnnouncer;
        private readonly IStyleAnnouncer dmc5Announcer;
        private readonly IStyleAnnouncer balrogAnnouncer;
        private readonly IStyleAnnouncer nicoAnnouncer;
        private readonly IStyleAnnouncer morrisonAnnouncer;
        private readonly float sfxCooldown = 1f;
        private readonly Dictionary<SoundId, int> soundIdsNextAvailability;
        private readonly Random random = new Random();
        private readonly PlayerState playerState;
        private bool isCastingLimitBreak;

        private double lastPlayTime = 0f;
        private IStyleAnnouncer announcer;

        public StyleAnnouncerService(StyleRankHandler styleRankHandler, PlayerActionTracker actionTracker)
        {
            audioService = AudioService.Instance;
            AssetsManager.AssetsReady += OnAssetsReady;

            playerState = PlayerState.GetInstance();
            playerState.RegisterCombatStateChangeHandler(OnCombat);

            rankHandler = styleRankHandler;
            rankHandler.StyleRankChange += OnRankChange;
            actionTracker.UsingLimitBreak += OnLimitBreak;

            dmcAnnouncer = new DmCAnnouncer();
            dmc5Announcer = new DmC5Announcer();
            balrogAnnouncer = new DmC5BalrogAnnouncer();
            nicoAnnouncer = new NicoAnnouncer();
            morrisonAnnouncer = new MorrisonAnnouncer();
            announcer = GetCurrentAnnouncer();
            soundIdsNextAvailability = new();
        }

        public void PlaySfx(SoundId key, bool force = false)
        {
            if (!Plugin.Configuration!.PlaySoundEffects)
            {
                return;
            }

            var effectiveKey = key;
            if (announcer.GetBlunderVariations().Contains(key))
            {
                effectiveKey = SoundId.DeadWeight1;
            }

            if (!force && !CanPlaySfx(effectiveKey))
            {
                if (!soundIdsNextAvailability.ContainsKey(effectiveKey))
                {
                    soundIdsNextAvailability.Add(effectiveKey, 0);
                }
                else
                {
                    soundIdsNextAvailability[effectiveKey]--;
                }
                return;
            }
            audioService.PlaySfx(key);

            lastPlayTime = ImGui.GetTime();
            if (!soundIdsNextAvailability.ContainsKey(effectiveKey) || force || soundIdsNextAvailability[effectiveKey] <= 0)
            {
                soundIdsNextAvailability[effectiveKey] = Plugin.Configuration!.PlaySfxEveryOccurrences.Value - 1;
            }
        }

        private void OnCombat(object? sender, bool enteredCombat)
        {
            soundIdsNextAvailability.Clear();
        }

        private bool CanPlaySfx(SoundId type)
        {
            double lastPlayTimeDiff = ImGui.GetTime() - lastPlayTime;
            return lastPlayTimeDiff > sfxCooldown
                && !isCastingLimitBreak
                && (!soundIdsNextAvailability.ContainsKey(type) ||
                   soundIdsNextAvailability[type] <= 0);
        }

        private IStyleAnnouncer GetCurrentAnnouncer()
        {
            return Plugin.Configuration!.Announcer.Value switch
            {
                AnnouncerType.DmC => dmcAnnouncer,
                AnnouncerType.DmC5 => dmc5Announcer,
                AnnouncerType.DmC5Balrog => balrogAnnouncer,
                AnnouncerType.Morrison => morrisonAnnouncer,
                AnnouncerType.Nico => nicoAnnouncer,
                _ => dmc5Announcer
            };
        }

        public static string GetPathToAnnouncerAudio(string name)
        {
            return Path.Combine(AssetsManager.GetAssetsDirectory(), $"Audio\\Announcer\\{name}");
        }

        public void OnAnnouncerTypeChange(object? sender, AnnouncerType type)
        {
            announcer = type switch { 
                AnnouncerType.DmC5 => dmc5Announcer,
                AnnouncerType.Nico => nicoAnnouncer,
                AnnouncerType.Morrison => morrisonAnnouncer,
                AnnouncerType.DmC5Balrog => balrogAnnouncer,
                AnnouncerType.DmC => dmcAnnouncer,
                _ => dmc5Announcer,
            };

            audioService.RegisterAnnouncerSfx(announcer.GetAnnouncerFilesById());
        }

        public void PlayRandomAnnouncerLine()
        {
            var lines = announcer.GetAnnouncerFilesById().Keys.ToList();
            var index = random.Next(lines.Count);

            audioService.PlaySfx(SelectRandomSfx(lines));
        }

        private void OnAssetsReady(object? sender, bool assetsReady)
        {
            if(!assetsReady)
            {
                return;
            }
            
            LoadAnnouncer();
        }

        private void LoadAnnouncer()
        {
            soundIdsNextAvailability.Clear();
            audioService.RegisterAnnouncerSfx(announcer.GetAnnouncerFilesById());
        }

        private void OnRankChange(object? sender, StyleRankHandler.RankChangeData data) {
            if (!Plugin.CanRunDmc())
            {
                return;
            }

            var newRank = data.NewRank;
            var previousRank = data.PreviousRank;

            if (data.IsBlunder && previousRank != StyleType.NoStyle)
            {
                var sfx = SelectRandomSfx(announcer.GetBlunderVariations());
                PlaySfx(sfx, Plugin.Configuration!.ForceSoundEffectsOnBlunder);
                return;
            }

            if(previousRank >= newRank)
            {
                return;
            }

            var sfxByStyle = announcer.GetStyleAnnouncementVariations();
            if (sfxByStyle.ContainsKey(newRank))
            {
                PlaySfx(SelectRandomSfx(announcer.GetStyleAnnouncementVariations()[newRank]));
            }
        }

        private void OnLimitBreak(object? sender, PlayerActionTracker.LimitBreakEvent el)
        {
            isCastingLimitBreak = el.IsCasting;
        }

        private SoundId SelectRandomSfx(IList<SoundId> soundIds)
        {
            int index = random.Next(soundIds.Count);
            return soundIds[index];
        }

        [Conditional("DEBUG")]
        public void PlayBlunder()
        {
            var sfx = SelectRandomSfx(announcer.GetBlunderVariations());
            PlaySfx(sfx, Plugin.Configuration!.ForceSoundEffectsOnBlunder);
        }

        [Conditional("DEBUG")]
        public void PlayForStyle(StyleType type)
        {
            if (!announcer.GetStyleAnnouncementVariations().ContainsKey(type)){
                return;
            }
            var sfx = SelectRandomSfx(announcer.GetStyleAnnouncementVariations()[type]);
            PlaySfx(sfx, Plugin.Configuration!.ForceSoundEffectsOnBlunder);
        }
    }
}
