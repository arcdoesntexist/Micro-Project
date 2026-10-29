using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace CPS.KitchenCatch
{
    public sealed class ProfileStore
    {
        public const string StorageKey = "CPS.KitchenCatch.Profiles";
        public const string BackupKey = "CPS.KitchenCatch.Profiles.Backup";
        private readonly Func<string, string> read;
        private readonly Action<string, string> write;
        private readonly Action flush;
        private readonly List<PlayerProfile> profiles = new List<PlayerProfile>();
        private readonly HashSet<string> completedSessions = new HashSet<string>(StringComparer.Ordinal);
        private bool readSucceeded;
        private string rawAwaitingBackup;
        public IReadOnlyList<PlayerProfile> Profiles { get; }
        public string Warning { get; private set; } = "";

        public ProfileStore(Func<string, string> read = null, Action<string, string> write = null, Action flush = null)
        {
            this.read = read ?? (key => PlayerPrefs.HasKey(key) ? PlayerPrefs.GetString(key) : null);
            this.write = write ?? PlayerPrefs.SetString;
            this.flush = flush ?? PlayerPrefs.Save;
            Profiles = profiles.AsReadOnly();
        }

        public void Load()
        {
            profiles.Clear();
            rawAwaitingBackup = null;
            readSucceeded = false;
            Warning = "";
            string raw;
            try
            {
                raw = read(StorageKey);
                readSucceeded = true;
            }
            catch (Exception exception)
            {
                Warning = "Profiles could not be read; saving is blocked. " + exception.Message;
                return;
            }

            if (raw == null) return;
            try
            {
                ValidateJson(raw);
                var payload = JsonUtility.FromJson<Payload>(raw);
                if (payload == null || payload.schemaVersion != 1 || payload.profiles == null)
                    throw new ArgumentException("Unsupported or missing profile schema.");
                ValidateProfiles(payload.profiles);
                profiles.AddRange(payload.profiles);
            }
            catch (Exception exception)
            {
                Warning = "Profiles could not be loaded; using an empty list. " + exception.Message;
                rawAwaitingBackup = raw;
                BackupRaw();
            }
        }

        public PlayerProfile Create(string displayName)
        {
            string name = displayName?.Trim();
            if (!ValidName(name))
                throw new ArgumentException("Nickname must contain 1–24 text elements.", nameof(displayName));
            var profile = new PlayerProfile { PlayerId = Guid.NewGuid().ToString("N"), DisplayName = name };
            profiles.Add(profile);
            Save();
            return profile;
        }

        public bool Rename(string playerId, string displayName)
        {
            var profile = profiles.Find(item => item.PlayerId == playerId);
            if (profile == null)
                throw new ArgumentException("Unknown player ID.", nameof(playerId));
            string name = displayName?.Trim();
            if (!ValidName(name))
                throw new ArgumentException("Nickname must contain 1–24 text elements.", nameof(displayName));
            string previousName = profile.DisplayName;
            profile.DisplayName = name;
            if (Save()) return true;
            profile.DisplayName = previousName;
            return false;
        }

        public bool Delete(string playerId)
        {
            int index = profiles.FindIndex(item => item.PlayerId == playerId);
            if (index < 0)
                throw new ArgumentException("Unknown player ID.", nameof(playerId));
            var profile = profiles[index];
            profiles.RemoveAt(index);
            if (Save()) return true;
            profiles.Insert(index, profile);
            return false;
        }

        public bool RecordResult(string playerId, string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                throw new ArgumentException("A session ID is required.", nameof(sessionId));
            var profile = profiles.Find(item => item.PlayerId == playerId);
            if (profile == null)
                throw new ArgumentException("Unknown player ID.", nameof(playerId));
            if (completedSessions.Contains(sessionId)) return false;
            if (profile.PlayCount == int.MaxValue)
                throw new InvalidOperationException("Play count has reached its limit.");
            completedSessions.Add(sessionId);
            profile.PlayCount++;
            Save();
            return true;
        }

        public bool Save()
        {
            if (!readSucceeded)
            {
                Warning = "Profiles have not been read successfully; saving is blocked.";
                return false;
            }
            if (!BackupRaw()) return false;
            try
            {
                ValidateProfiles(profiles);
                write(StorageKey, JsonUtility.ToJson(new Payload { schemaVersion = 1, profiles = profiles }));
                flush();
                Warning = "";
                return true;
            }
            catch (Exception exception)
            {
                Warning = "Profiles are available in memory but could not be saved. " + exception.Message;
                return false;
            }
        }

        private bool BackupRaw()
        {
            if (rawAwaitingBackup == null) return true;
            try
            {
                write(BackupKey, rawAwaitingBackup);
                flush();
                rawAwaitingBackup = null;
                return true;
            }
            catch (Exception exception)
            {
                Warning = "Raw profile backup failed; original storage is protected. " + exception.Message;
                return false;
            }
        }

        private static bool ValidName(string name)
        {
            if (string.IsNullOrEmpty(name) || name != name.Trim()) return false;
            int length = new StringInfo(name).LengthInTextElements;
            return length >= 1 && length <= 24;
        }

        private static void ValidateJson(string raw)
        {
            JObject json;
            using (var input = new StringReader(raw))
            using (var reader = new JsonTextReader(input) { DateParseHandling = DateParseHandling.None })
            {
                json = JObject.Load(reader);
                if (reader.Read()) throw new ArgumentException("Unexpected content after profile JSON.");
            }
            if (json["schemaVersion"]?.Type != JTokenType.Integer ||
                json["schemaVersion"].Value<int>() != 1 || !(json["profiles"] is JArray entries))
                throw new ArgumentException("Unsupported or missing profile schema.");
            foreach (var entry in entries)
                if (!(entry is JObject profile) || profile["PlayerId"]?.Type != JTokenType.String ||
                    profile["DisplayName"]?.Type != JTokenType.String ||
                    profile["PlayCount"]?.Type != JTokenType.Integer ||
                    profile["PlayCount"].Value<long>() < 0 || profile["PlayCount"].Value<long>() > int.MaxValue)
                    throw new ArgumentException("Invalid profile field types or play count.");
        }

        private static void ValidateProfiles(List<PlayerProfile> values)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var profile in values)
                if (profile == null || string.IsNullOrWhiteSpace(profile.PlayerId) ||
                    profile.PlayerId != profile.PlayerId.Trim() || !ids.Add(profile.PlayerId) ||
                    !ValidName(profile.DisplayName) || profile.PlayCount < 0)
                    throw new ArgumentException("Invalid profile data or duplicate player ID.");
        }

        [Serializable]
        private sealed class Payload
        {
            public int schemaVersion;
            public List<PlayerProfile> profiles;
        }
    }
}
