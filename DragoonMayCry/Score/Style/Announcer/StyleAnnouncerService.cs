using DragoonMayCry.Audio;
using DragoonMayCry.Data;
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
using static DragoonMayCry.UI.JobConfigurationWindow;

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
        private readonly Dictionary<SoundId, int> soundIdsNextAvailability = new();
        private readonly Random random = new Random();
        private readonly PlayerState playerState;
        private bool isCastingLimitBreak;

        private double lastPlayTime = 0f;
        private IStyleAnnouncer? announcer;

        public StyleAnnouncerService(StyleRankHandler styleRankHandler, PlayerActionTracker actionTracker)
        {
            audioService = AudioService.Instance;
            AssetsManager.AssetsReady += OnAssetsReady;

            playerState = PlayerState.GetInstance();
            playerState.RegisterCombatStateChangeHandler(OnCombat);
            playerState.RegisterJobChangeHandler(OnJobChange);

            rankHandler = styleRankHandler;
            rankHandler.StyleRankChange += OnRankChange;
            actionTracker.UsingLimitBreak += OnLimitBreak;

            dmcAnnouncer = new DmCAnnouncer();
            dmc5Announcer = new DmC5Announcer();
            balrogAnnouncer = new DmC5BalrogAnnouncer();
            nicoAnnouncer = new NicoAnnouncer();
            morrisonAnnouncer = new MorrisonAnnouncer();
            UpdateAnnouncer();
        }

        public void PlaySfx(SoundId key, bool force = false)
        {
            if (!Plugin.Configuration!.PlaySoundEffects || announcer == null)
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

        private void OnJobChange(object? sender, JobIds job)
        {
            UpdateAnnouncer();
        }

        private bool CanPlaySfx(SoundId type)
        {
            double lastPlayTimeDiff = ImGui.GetTime() - lastPlayTime;
            return lastPlayTimeDiff > sfxCooldown
                && !isCastingLimitBreak
                && (!soundIdsNextAvailability.ContainsKey(type) ||
                   soundIdsNextAvailability[type] <= 0);
        }

        private IStyleAnnouncer GetCurrentJobAnnouncer()
        {
            JobIds currentJob = playerState.GetCurrentJob();
            if (!Plugin.Configuration!.JobConfiguration.ContainsKey(currentJob))
            {
                return dmc5Announcer;
            }
            var jobAnnouncer = Plugin.Configuration!.JobConfiguration[currentJob].Announcer.Value;
            return GetAnnouncerByType(jobAnnouncer);
        }

        public static string GetPathToAnnouncerAudio(string name)
        {
            return Path.Combine(AssetsManager.GetAssetsDirectory(), $"Audio\\Announcer\\{name}");
        }

        public void OnAnnouncerTypeChange(object? sender, JobAnnouncerType jobAnnouncer)
        {
            if(playerState.GetCurrentJob() != jobAnnouncer.job)
            {
                return;
            }

            UdpateAnnouncer(jobAnnouncer.type);
        }

        private void UpdateAnnouncer()
        {
            announcer = GetCurrentJobAnnouncer();
            LoadAnnouncer();
        }

        private void UdpateAnnouncer(AnnouncerType type)
        {
            announcer = GetAnnouncerByType(type);
            LoadAnnouncer();
        }

        private IStyleAnnouncer GetAnnouncerByType(AnnouncerType type)
        {
            return type switch
            {
                AnnouncerType.DmC5 => dmc5Announcer,
                AnnouncerType.Nico => nicoAnnouncer,
                AnnouncerType.Morrison => morrisonAnnouncer,
                AnnouncerType.DmC5Balrog => balrogAnnouncer,
                AnnouncerType.DmC => dmcAnnouncer,
                _ => dmc5Announcer,
            };
        }

        public void PlayRandomAnnouncerLine(AnnouncerType announcerType)
        {
            var styleAnnouncer = GetAnnouncerByType(announcerType);
            var lines = styleAnnouncer.GetAnnouncerFilesById().Keys.ToList();
            var randomId = SelectRandomSfx(lines);
            audioService.PlaySfx(styleAnnouncer.GetAnnouncerFilesById()[randomId]);
        }

        private void OnAssetsReady(object? sender, bool assetsReady)
        {
            if(!assetsReady || announcer == null)
            {
                return;
            }
            
            LoadAnnouncer();
        }

        private void LoadAnnouncer()
        {
            soundIdsNextAvailability.Clear();
            audioService.RegisterAnnouncerSfx(announcer!.GetAnnouncerFilesById());
        }

        private void OnRankChange(object? sender, StyleRankHandler.RankChangeData data) {
            if (!Plugin.CanRunDmc() || announcer == null)
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
            var sfx = SelectRandomSfx(dmc5Announcer.GetBlunderVariations());
            PlaySfx(sfx, Plugin.Configuration!.ForceSoundEffectsOnBlunder);
        }

        [Conditional("DEBUG")]
        public void PlayForStyle(StyleType type)
        {
            if (!dmc5Announcer.GetStyleAnnouncementVariations().ContainsKey(type)){
                return;
            }
            var sfx = SelectRandomSfx(dmc5Announcer.GetStyleAnnouncementVariations()[type]);
            PlaySfx(sfx, Plugin.Configuration!.ForceSoundEffectsOnBlunder);
        }
    }
}
