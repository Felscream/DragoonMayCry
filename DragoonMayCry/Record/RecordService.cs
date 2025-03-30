using Dalamud.Plugin;
using Dalamud.Plugin.Services;
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
using DragoonMayCry.Configuration;

namespace DragoonMayCry.Record
{
    public class RecordService
    {
        public Extension[] Extensions { get; private set; } = [];
        public EventHandler<Dictionary<JobId, JobRecord>>? CharacterRecordsChanged;

        private const string TrackedDutiesResource = "DragoonMayCry.Data.TrackedDuties.json";
        private readonly IDalamudPluginInterface pluginInterface;
        private readonly IClientState clientState;
        private readonly IDutyState dutyState;
        private readonly FinalRankCalculator finalRankCalculator;
        private readonly PlayerState playerState;
        private readonly string recordDirectoryPath;
        private ulong characterId = 0;
        private Dictionary<JobId, JobRecord>? characterRecords;
        private Dictionary<ushort, TrackableDuty> trackableDuties = new();
        private bool ready = false;

        public RecordService(FinalRankCalculator finalRankCalculator)
        {
            pluginInterface = Plugin.PluginInterface;
            clientState = Service.ClientState;
            this.finalRankCalculator = finalRankCalculator;
            dutyState = Service.DutyState;
            dutyState.DutyStarted += OnDutyStarted;
            playerState = PlayerState.GetInstance();

            clientState.Login += OnLogin;
            finalRankCalculator.DutyCompletedFinalRank += OnDutyCompletedFinalRank;

            recordDirectoryPath = $"{pluginInterface.GetPluginConfigDirectory()}/records";
        }

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
                    using (var reader = new StreamReader(stream))
                    {
                        var content = reader.ReadToEnd();
                        Extensions = JsonConvert.DeserializeObject<Extension[]>(content) ?? [];
                        trackableDuties = ExtractTrackableDuties(Extensions);
                        ready = true;
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

            if (clientState.LocalContentId == 0)
            {
                characterRecords = null;
                return;
            }

            if (clientState.LocalContentId == characterId && characterRecords != null)
            {
                return;
            }

            characterId = clientState.LocalContentId;
            characterRecords = LoadCharacterRecords(characterId);
        }

        private Dictionary<JobId, JobRecord> LoadCharacterRecords(ulong characterId)
        {
            var characterRecordPath = GetCharacterRecordsPath(characterId);
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

                var recordBackUp = $"{recordDirectoryPath}/{characterId}_back.json";
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
            if (clientState.LocalContentId == 0)
            {
                return;
            }

            characterId = clientState.LocalContentId;
            characterRecords = LoadCharacterRecords(characterId);
        }

        private void OnDutyCompletedFinalRank(object? sender, FinalRank finalRank)
        {
            if (IsInvalidEntry(finalRank) || clientState.LocalContentId == 0)
            {
                return;
            }

            if (characterRecords == null || characterId == 0)
            {
                characterId = clientState.LocalContentId;
                characterRecords = LoadCharacterRecords(characterId);
            }

            var currentJob = playerState.GetCurrentJob();
            if (!CanTrackJobRecord(currentJob))
            {
                return;
            }

            var emdEnabled = Plugin.IsEmdModeEnabled();

            var jobRecord = GetJobRecord(finalRank, currentJob, emdEnabled);

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
            
            var playerLevel = playerState.Player != null ? playerState.Player.Level : int.MaxValue;
            return !ready
                   || !trackableDuties.ContainsKey(finalRank.InstanceId)
                   || !Plugin.IsEnabledForCurrentJob()
                   || trackableDuties[finalRank.InstanceId].LvlSync < playerLevel;
        }

        private JobRecord GetJobRecord(FinalRank finalRank, JobId currentJob, bool emdEnabled)
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
                   || (targetDifficulty[finalRank.InstanceId].Result == finalRank.Rank
                       && targetDifficulty[finalRank.InstanceId].KillTime > finalRank.KillTime);
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
            if (clientState.LocalContentId == 0)
            {
                return new Dictionary<JobId, JobRecord>();
            }

            if (characterId == clientState.LocalContentId && characterRecords != null)
            {
                return new(characterRecords);
            }

            return LoadCharacterRecords(clientState.LocalContentId);
        }
    }
}
