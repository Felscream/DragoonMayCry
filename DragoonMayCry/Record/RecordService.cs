#region

using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using DragoonMayCry.Configuration;
using DragoonMayCry.Data;
using DragoonMayCry.Record.Model;
using DragoonMayCry.Score.Rank;
using DragoonMayCry.State;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

#endregion

namespace DragoonMayCry.Record
{
    public class RecordService
    {

        private const string TrackedDutiesResource = "DragoonMayCry.Data.TrackedDuties.json";
        private readonly IClientState clientState;
        private readonly DmcPlayerState dmcPlayerState;
        private readonly IDutyState dutyState;
        private readonly FinalRankCalculator finalRankCalculator;
        private readonly IPlayerState playerState;
        private readonly IDalamudPluginInterface pluginInterface;
        private readonly string recordDirectoryPath;
        private ulong characterId;
        private Dictionary<JobId, JobRecord>? characterRecords;
        public EventHandler<Dictionary<JobId, JobRecord>>? CharacterRecordsChanged;
        private bool ready;
        private Dictionary<ushort, TrackableDuty> trackableDuties = new();

        public RecordService(FinalRankCalculator finalRankCalculator)
        {
            pluginInterface = Plugin.PluginInterface;
            clientState = Service.ClientState;
            playerState = Service.PlayerState;
            this.finalRankCalculator = finalRankCalculator;
            dutyState = Service.DutyState;
            dutyState.DutyStarted += OnDutyStarted;
            dmcPlayerState = DmcPlayerState.GetInstance();

            clientState.Login += OnLogin;
            finalRankCalculator.DutyCompletedFinalRank += OnDutyCompletedFinalRank;

            recordDirectoryPath = $"{pluginInterface.GetPluginConfigDirectory()}/records";
        }
        public Extension[] Extensions { get; private set; } = [];

        public void Initialize()
        {
            if (ready)
            {
                return;
            }

            var assembly = Assembly.GetExecutingAssembly();
            try
            {
                using (var stream = assembly.GetManifestResourceStream(TrackedDutiesResource))
                {
                    if (stream != null)
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            var content = reader.ReadToEnd();
                            Extensions = JsonConvert.DeserializeObject<Extension[]>(content) ?? [];
                            trackableDuties = ExtractTrackableDuties(Extensions);
                            ready = true;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (e.StackTrace is not null)
                {
                    Service.Log.Debug(e.StackTrace);
                }

                Service.Log.Warning("Could not retrieve trackable instances, no new record will be tracked");
                Extensions = [];
                ready = false;
            }
        }

        private Dictionary<ushort, TrackableDuty> ExtractTrackableDuties(Extension[] extensions)
        {
            return extensions.ToList().SelectMany(extension => extension.Instances).ToDictionary();
        }

        private void OnDutyStarted(object? sender, ushort dutyId)
        {
            if (!trackableDuties.ContainsKey(dutyId) || !Plugin.IsEnabledForCurrentJob())
            {
                return;
            }

            if (playerState.ContentId == 0)
            {
                characterRecords = null;
                return;
            }

            if (playerState.ContentId == characterId && characterRecords != null)
            {
                return;
            }

            characterId = playerState.ContentId;
            characterRecords = LoadCharacterRecords(characterId);
        }

        private Dictionary<JobId, JobRecord> LoadCharacterRecords(ulong charId)
        {
            var characterRecordPath = GetCharacterRecordsPath(charId);
            if (!File.Exists(characterRecordPath))
            {
                return [];
            }

            try
            {
                var localRecords = File.ReadAllText(characterRecordPath);
                return JsonConvert.DeserializeObject<Dictionary<JobId, JobRecord>>(localRecords) ?? [];
            }
            catch (Exception e)
            {
                if (e.StackTrace is not null)
                {
                    Service.Log.Debug(e.StackTrace);
                }

                var recordBackUp = $"{recordDirectoryPath}/{charId}_back.json";
                File.Move(characterRecordPath, recordBackUp);
                Service.Log.Warning(
                    $"This character records are unreadable. They have been backed up here {recordBackUp}. New empty records will be used.");
                return [];
            }
        }

        private string GetCharacterRecordsPath(ulong charId)
        {
            return $"{recordDirectoryPath}/{charId}.json";
        }

        private void OnLogin()
        {
            if (playerState.ContentId == 0)
            {
                return;
            }

            characterId = playerState.ContentId;
            characterRecords = LoadCharacterRecords(characterId);
        }

        private void OnDutyCompletedFinalRank(object? sender, FinalRank finalRank)
        {
            if (IsInvalidEntry(finalRank) || playerState.ContentId == 0)
            {
                return;
            }

            if (characterRecords == null || characterId == 0)
            {
                characterId = playerState.ContentId;
                characterRecords = LoadCharacterRecords(characterId);
            }

            var currentJob = dmcPlayerState.GetCurrentJob();
            if (!CanTrackJobRecord(currentJob))
            {
                return;
            }

            var emdEnabled = Plugin.IsEmdModeEnabled();

            var jobRecord = GetJobRecord(currentJob);

            var targetDifficulty = emdEnabled ? jobRecord.EmdRecord : jobRecord.Record;
            if (!IsBetterRecord(finalRank, targetDifficulty))
            {
                return;
            }

            UpdateCharacterRecord(finalRank, targetDifficulty);
            SaveCharacterRecords();
            CharacterRecordsChanged?.Invoke(this, characterRecords);
        }

        private static bool CanTrackJobRecord(JobId currentJob)
        {
            return currentJob != JobId.OTHER
                   && Plugin.IsEnabledForCurrentJob()
                   && Plugin.Configuration!.JobConfiguration[currentJob].DifficultyMode != DifficultyMode.Sprout;
        }

        private bool IsInvalidEntry(FinalRank finalRank)
        {

            var playerLevel = dmcPlayerState.Player != null ? dmcPlayerState.Player.Level : int.MaxValue;
            return !ready
                   || !trackableDuties.ContainsKey(finalRank.InstanceId)
                   || !Plugin.IsEnabledForCurrentJob()
                   || trackableDuties[finalRank.InstanceId].LvlSync < playerLevel;
        }

        private JobRecord GetJobRecord(JobId currentJob)
        {
            if (!characterRecords!.ContainsKey(currentJob))
            {
                characterRecords.Add(currentJob, new JobRecord());
            }

            return characterRecords[currentJob];
        }

        private void UpdateCharacterRecord(FinalRank finalRank, Dictionary<ushort, DutyRecord> targetDifficulty)
        {
            if (!targetDifficulty.ContainsKey(finalRank.InstanceId))
            {
                targetDifficulty.Add(finalRank.InstanceId, new DutyRecord(finalRank.Rank, finalRank.KillTime));
            }
            else
            {
                targetDifficulty[finalRank.InstanceId] = new DutyRecord(finalRank.Rank, finalRank.KillTime);
            }
        }

        private static bool IsBetterRecord(FinalRank finalRank, Dictionary<ushort, DutyRecord> targetDifficulty)
        {
            return !targetDifficulty.ContainsKey(finalRank.InstanceId)
                   || targetDifficulty[finalRank.InstanceId].Result < finalRank.Rank
                   || targetDifficulty[finalRank.InstanceId].Result == finalRank.Rank
                   && targetDifficulty[finalRank.InstanceId].KillTime > finalRank.KillTime;
        }

        private void SaveCharacterRecords()
        {
            if (characterId == 0 || characterRecords == null)
            {
                return;
            }

            var characterRecordsPath = GetCharacterRecordsPath(characterId);
            Directory.CreateDirectory(recordDirectoryPath);
            File.WriteAllText(characterRecordsPath, JsonConvert.SerializeObject(characterRecords));
        }

        public Dictionary<JobId, JobRecord> GetCharacterRecords()
        {
            if (playerState.ContentId == 0)
            {
                return new Dictionary<JobId, JobRecord>();
            }

            if (characterId == playerState.ContentId && characterRecords != null)
            {
                return new Dictionary<JobId, JobRecord>(characterRecords);
            }

            return LoadCharacterRecords(playerState.ContentId);
        }
    }
}
