using Dalamud.Plugin.Services;
using DragoonMayCry.Audio.StyleAnnouncer.Announcer;
using DragoonMayCry.Data;
using DragoonMayCry.Score.Action;
using DragoonMayCry.Score.Model;
using DragoonMayCry.Score.Rank;
using DragoonMayCry.State;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using static DragoonMayCry.UI.JobConfigurationWindow;

namespace DragoonMayCry.Audio.StyleAnnouncer
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
        private readonly Random random = new();
        private readonly PlayerState playerState;
        private bool isCastingLimitBreak;

        private bool initialized;
        private double lastPlayTime = 0f;
        private IStyleAnnouncer? announcer;

        public StyleAnnouncerService(StyleRankHandler styleRankHandler, PlayerActionTracker actionTracker)
        {
            audioService = AudioService.Instance;
            AssetsManager.AssetsReady += OnAssetsReady;

            playerState = PlayerState.GetInstance();
            playerState.RegisterCombatStateChangeHandler(OnCombat);
            playerState.RegisterJobChangeHandler(OnJobChange);
            playerState.RegisterLoginStateChangeHandler(OnLogin);

            rankHandler = styleRankHandler;
            rankHandler.StyleRankChange += OnRankChange;
            actionTracker.UsingLimitBreak += OnLimitBreak;
            actionTracker.OnLimitBreakEffect += OnLimitBreakEffect;

            dmcAnnouncer = new DmCAnnouncer();
            dmc5Announcer = new DmC5Announcer();
            balrogAnnouncer = new DmC5BalrogAnnouncer();
            nicoAnnouncer = new NicoAnnouncer();
            morrisonAnnouncer = new MorrisonAnnouncer();

            // to update the announcer if the user activates the plugin after character selection
            Service.Framework.Update += Initialize;
        }

        public void PlaySfx(SoundId key, bool force = false)
        {
            if (!Plugin.Configuration!.PlaySoundEffects || announcer == null || !AssetsManager.IsReady)
            {
                return;
            }

            var effectiveKey = GetAnnouncementGroup(key);

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

        private void Initialize(IFramework framework)
        {
            if (!initialized && playerState.Player != null && AssetsManager.IsReady)
            {
                initialized = true;
                UpdateAnnouncer();
                Service.Framework.Update -= Initialize;
            }
        }

        private void OnLimitBreakEffect(object? sender, EventArgs e)
        {
            if (!Plugin.CanRunDmc() || announcer == null)
            {
                return;
            }
            var currentRank = rankHandler.CurrentStyle.Value;
            var sfxByStyle = announcer.GetStyleAnnouncementVariations();
            if (sfxByStyle.TryGetValue(currentRank, out var value))
            {
                audioService.PlaySfx(SelectRandomSfx(value));
            }
        }

        private void OnCombat(object? sender, bool enteredCombat)
        {
            soundIdsNextAvailability.Clear();
        }

        private void OnJobChange(object? sender, JobId job)
        {
            UpdateAnnouncer();
        }

        private void OnLogin(object? sender, bool loggedIn)
        {
            if (loggedIn)
            {
                UpdateAnnouncer();
            }
        }

        private bool CanPlaySfx(SoundId type)
        {
            var lastPlayTimeDiff = ImGui.GetTime() - lastPlayTime;
            return lastPlayTimeDiff > sfxCooldown
                && !isCastingLimitBreak
                && (!soundIdsNextAvailability.ContainsKey(type) ||
                   soundIdsNextAvailability[type] <= 0);
        }

        private IStyleAnnouncer GetCurrentJobAnnouncer()
        {
            var currentJob = playerState.GetCurrentJob();
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
            if (playerState.GetCurrentJob() != jobAnnouncer.job)
            {
                return;
            }

            UdpateAnnouncer(jobAnnouncer.type);
        }

        private void UpdateAnnouncer()
        {
            if (AssetsManager.IsReady)
            {
                announcer = GetCurrentJobAnnouncer();
                LoadAnnouncer();
            }
        }

        private void UdpateAnnouncer(AnnouncerType type)
        {
            if (AssetsManager.IsReady)
            {
                announcer = GetAnnouncerByType(type);
                LoadAnnouncer();
            }
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
            if (!assetsReady || announcer == null)
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

        private void OnRankChange(object? sender, StyleRankHandler.RankChangeData data)
        {
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

            if (previousRank >= newRank)
            {
                return;
            }

            var sfxByStyle = announcer.GetStyleAnnouncementVariations();
            if (sfxByStyle.ContainsKey(newRank))
            {
                PlaySfx(SelectRandomSfx(sfxByStyle[newRank]));
            }
        }

        private void OnLimitBreak(object? sender, PlayerActionTracker.LimitBreakEvent el)
        {
            isCastingLimitBreak = el.IsCasting;
        }

        private SoundId SelectRandomSfx(IList<SoundId> soundIds)
        {
            var index = random.Next(soundIds.Count);
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
            if (!dmc5Announcer.GetStyleAnnouncementVariations().ContainsKey(type))
            {
                return;
            }
            var sfx = SelectRandomSfx(dmc5Announcer.GetStyleAnnouncementVariations()[type]);
            PlaySfx(sfx, Plugin.Configuration!.ForceSoundEffectsOnBlunder);
        }

        private static SoundId GetAnnouncementGroup(SoundId id)
        {
            switch (id)
            {
                case SoundId.DeadWeight1:
                case SoundId.DeadWeight2:
                case SoundId.DeadWeight3:
                case SoundId.DeadWeight4:
                case SoundId.DeadWeight5:
                    return SoundId.DeadWeight1;
                case SoundId.Dismal2:
                case SoundId.Dismal1:
                    return SoundId.Dismal1;
                case SoundId.Crazy1:
                case SoundId.Crazy2:
                    return SoundId.Crazy1;
                case SoundId.Badass1:
                case SoundId.Badass2:
                    return SoundId.Badass1;
                case SoundId.Apocalyptic1:
                case SoundId.Apocalyptic2:
                    return SoundId.Apocalyptic1;
                case SoundId.Savage1:
                case SoundId.Savage2:
                    return SoundId.Savage1;
                case SoundId.SickSkills1:
                case SoundId.SickSkills2:
                    return SoundId.SickSkills1;
                case SoundId.SmokinSexyStyle1:
                case SoundId.SmokinSexyStyle2:
                    return SoundId.SmokinSexyStyle1;
                default:
                    return id;
            }
        }
        public static string GetAnnouncerTypeLabel(AnnouncerType type)
        {
            return type switch
            {
                AnnouncerType.DmC => "DmC: Devil May Cry",
                AnnouncerType.DmC5 => "Devil May Cry 5",
                AnnouncerType.DmC5Balrog => "Devil May Cry 5 / Michael Schwalbe",
                AnnouncerType.Nico => "Nico",
                AnnouncerType.Morrison => "Morrison",
                _ => "Unknown"
            };
        }
    }
}
