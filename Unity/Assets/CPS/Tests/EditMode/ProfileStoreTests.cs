using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace CPS.KitchenCatch.Tests
{
    public sealed class ProfileStoreTests
    {
        private Storage storage;
        private ProfileStore store;

        [SetUp]
        public void SetUp()
        {
            storage = new Storage();
            store = NewStore();
            store.Load();
        }

        [Test]
        public void MissingStorageStartsEmptyWithoutWarning()
        {
            Assert.That(store.Profiles, Is.Empty);
            Assert.That(store.Warning, Is.Empty);
        }

        [TestCase("  อาร์ค  ", "อาร์ค")]
        [TestCase("<b>Arc</b>", "<b>Arc</b>")]
        [TestCase("a\u0301", "a\u0301")]
        [TestCase("2026-10-08T00:00:00Z", "2026-10-08T00:00:00Z")]
        public void NamesAreTrimmedAndLiteral(string supplied, string expected)
        {
            Assert.That(store.Create(supplied).DisplayName, Is.EqualTo(expected));
            var reloaded = NewStore();
            reloaded.Load();
            Assert.That(reloaded.Warning, Is.Empty);
            Assert.That(reloaded.Profiles, Has.Count.EqualTo(1));
            Assert.That(reloaded.Profiles[0].DisplayName, Is.EqualTo(expected));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" \t\n ")]
        [TestCase("abcdefghijklmnopqrstuvwxy")]
        public void InvalidNamesDoNotCreateProfiles(string supplied)
        {
            Assert.Throws<ArgumentException>(() => store.Create(supplied));
            Assert.That(store.Profiles, Is.Empty);
        }

        [Test]
        public void TwentyFourTextElementsMayHaveMoreThanTwentyFourCodeUnits()
        {
            var name = string.Concat(System.Linq.Enumerable.Repeat("a\u0301", 24));
            Assert.That(store.Create(name).DisplayName, Is.EqualTo(name));
            Assert.Throws<ArgumentException>(() => store.Create(name + "b"));
        }

        [Test]
        public void DuplicateNamesHaveDistinctPersistentGuidIds()
        {
            var first = store.Create("Arc");
            var second = store.Create("Arc");
            Assert.That(Guid.TryParse(first.PlayerId, out _), Is.True);
            Assert.That(Guid.TryParse(second.PlayerId, out _), Is.True);
            Assert.That(second.PlayerId, Is.Not.EqualTo(first.PlayerId));
            var reloaded = NewStore();
            reloaded.Load();
            Assert.That(reloaded.Profiles.Count, Is.EqualTo(2));
            Assert.That(reloaded.Profiles[1].PlayerId, Is.EqualTo(second.PlayerId));
        }

        [Test]
        public void RenameKeepsIdentityPlayCountAndAllowsDuplicateNames()
        {
            var first = store.Create("Arc");
            var second = store.Create("Mint");
            Assert.That(store.RecordResult(first.PlayerId, "session"), Is.True);

            Assert.That(store.Rename(first.PlayerId, "  Mint  "), Is.True);

            Assert.That(first.DisplayName, Is.EqualTo(second.DisplayName));
            Assert.That(first.PlayCount, Is.EqualTo(1));
            var reloaded = NewStore();
            reloaded.Load();
            Assert.That(reloaded.Profiles[0].PlayerId, Is.EqualTo(first.PlayerId));
            Assert.That(reloaded.Profiles[0].DisplayName, Is.EqualTo("Mint"));
            Assert.That(reloaded.Profiles[0].PlayCount, Is.EqualTo(1));
        }

        [Test]
        public void DeleteRemovesOnlyRequestedProfileAndPersists()
        {
            var first = store.Create("Arc");
            var second = store.Create("Mint");

            Assert.That(store.Delete(first.PlayerId), Is.True);

            Assert.That(store.Profiles, Has.Count.EqualTo(1));
            Assert.That(store.Profiles[0], Is.SameAs(second));
            var reloaded = NewStore();
            reloaded.Load();
            Assert.That(reloaded.Profiles, Has.Count.EqualTo(1));
            Assert.That(reloaded.Profiles[0].PlayerId, Is.EqualTo(second.PlayerId));
        }

        [Test]
        public void RenameAndDeleteRejectUnknownProfilesWithoutChangingData()
        {
            var profile = store.Create("Arc");

            Assert.Throws<ArgumentException>(() => store.Rename("missing", "Mint"));
            Assert.Throws<ArgumentException>(() => store.Delete("missing"));

            Assert.That(store.Profiles, Has.Count.EqualTo(1));
            Assert.That(profile.DisplayName, Is.EqualTo("Arc"));
        }

        [Test]
        public void ResultCountsOncePerSessionAcrossProfilesAndReloadInSameRuntime()
        {
            var first = store.Create("Arc");
            var second = store.Create("Arc");
            Assert.That(store.RecordResult(first.PlayerId, "session-1"), Is.True);
            Assert.That(store.RecordResult(first.PlayerId, "session-1"), Is.False);
            Assert.That(store.RecordResult(second.PlayerId, "session-1"), Is.False);
            store.Load();
            Assert.That(store.RecordResult(first.PlayerId, "session-1"), Is.False);
            Assert.That(store.RecordResult(first.PlayerId, "session-2"), Is.True);
            var reloaded = NewStore();
            reloaded.Load();
            Assert.That(reloaded.Profiles[0].PlayCount, Is.EqualTo(2));
            Assert.That(reloaded.Profiles[1].PlayCount, Is.Zero);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void InvalidSessionDoesNotConsumeCount(string sessionId)
        {
            var profile = store.Create("Arc");
            Assert.Throws<ArgumentException>(() => store.RecordResult(profile.PlayerId, sessionId));
            Assert.That(profile.PlayCount, Is.Zero);
        }

        [Test]
        public void UnknownPlayerDoesNotConsumeSession()
        {
            Assert.Throws<ArgumentException>(() => store.RecordResult("missing", "session"));
            var profile = store.Create("Arc");
            Assert.That(store.RecordResult(profile.PlayerId, "session"), Is.True);
        }

        [TestCase("not json")]
        [TestCase("")]
        [TestCase("null")]
        [TestCase("{}")]
        [TestCase("{\"schemaVersion\":1,\"profiles\":null}")]
        [TestCase("{\"schemaVersion\":1}")]
        [TestCase("{\"schemaVersion\":\"1\",\"profiles\":[]}")]
        [TestCase("{\"schemaVersion\":1,\"profiles\":[{\"PlayerId\":\"id\",\"DisplayName\":\"Arc\",\"PlayCount\":1.5}]}")]
        [TestCase("{\"schemaVersion\":1,\"profiles\":[{\"PlayerId\":42,\"DisplayName\":\"Arc\",\"PlayCount\":0}]}")]
        [TestCase("{\"schemaVersion\":1,\"profiles\":[{\"PlayerId\":\"id\",\"DisplayName\":\"Arc\"}]}")]
        [TestCase("{\"schemaVersion\":2,\"profiles\":[]}")]
        [TestCase("{\"schemaVersion\":1,\"profiles\":[null]}")]
        [TestCase("{\"schemaVersion\":1,\"profiles\":[{\"PlayerId\":\"\",\"DisplayName\":\"Arc\",\"PlayCount\":0}]}")]
        [TestCase("{\"schemaVersion\":1,\"profiles\":[{\"PlayerId\":\"id\",\"DisplayName\":\" \",\"PlayCount\":0}]}")]
        [TestCase("{\"schemaVersion\":1,\"profiles\":[{\"PlayerId\":\"id\",\"DisplayName\":\"Arc\",\"PlayCount\":-1}]}")]
        [TestCase("{\"schemaVersion\":1,\"profiles\":[{\"PlayerId\":\"id\",\"DisplayName\":\"Arc\",\"PlayCount\":0},{\"PlayerId\":\"id\",\"DisplayName\":\"B\",\"PlayCount\":0}]}")]
        public void InvalidPayloadIsBackedUpBeforeItCanBeReplaced(string raw)
        {
            storage.Seed(raw);
            store.Load();
            Assert.That(store.Profiles, Is.Empty);
            Assert.That(store.Warning, Is.Not.Empty);
            Assert.That(storage.Persisted[ProfileStore.BackupKey], Is.EqualTo(raw));
            store.Create("Recovered");
            Assert.That(storage.Persisted[ProfileStore.BackupKey], Is.EqualTo(raw));
            var reloaded = NewStore();
            reloaded.Load();
            Assert.That(reloaded.Profiles[0].DisplayName, Is.EqualTo("Recovered"));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void FailedBackupNeverOverwritesPrimaryAndCanBeRetried(bool writeFailure)
        {
            const string raw = "recover me";
            storage.Seed(raw);
            storage.FailWriteKey = writeFailure ? ProfileStore.BackupKey : null;
            storage.FailFlush = !writeFailure;
            store.Load();
            var profile = store.Create("Memory");
            Assert.That(store.Save(), Is.False);
            Assert.That(storage.Pending[ProfileStore.StorageKey], Is.EqualTo(raw));
            Assert.That(storage.Persisted[ProfileStore.StorageKey], Is.EqualTo(raw));
            Assert.That(store.Profiles[0], Is.SameAs(profile));
            Assert.That(store.Warning, Is.Not.Empty);
            storage.FailWriteKey = null;
            storage.FailFlush = false;
            Assert.That(store.Save(), Is.True);
            Assert.That(storage.Persisted[ProfileStore.BackupKey], Is.EqualTo(raw));
        }

        [Test]
        public void ReadFailureBlocksWritesUntilStorageCanBeRead()
        {
            const string raw = "unreadable raw";
            storage.Seed(raw);
            storage.FailRead = true;
            Assert.DoesNotThrow(() => store.Load());
            store.Create("Memory");
            Assert.That(store.Save(), Is.False);
            Assert.That(store.Warning, Is.Not.Empty);
            Assert.That(storage.Pending[ProfileStore.StorageKey], Is.EqualTo(raw));
            storage.FailRead = false;
            store.Load();
            Assert.That(storage.Persisted[ProfileStore.BackupKey], Is.EqualTo(raw));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void SaveFailureKeepsProfilesAndSessionCountInMemory(bool writeFailure)
        {
            var profile = store.Create("Arc");
            storage.FailWriteKey = writeFailure ? ProfileStore.StorageKey : null;
            storage.FailFlush = !writeFailure;
            var memoryProfile = store.Create("Memory");
            Assert.That(store.RecordResult(profile.PlayerId, "session"), Is.True);
            Assert.That(store.RecordResult(profile.PlayerId, "session"), Is.False);
            Assert.That(profile.PlayCount, Is.EqualTo(1));
            Assert.That(store.Profiles[1], Is.SameAs(memoryProfile));
            Assert.That(store.Save(), Is.False);
            Assert.That(store.Warning, Is.Not.Empty);
            storage.FailWriteKey = null;
            storage.FailFlush = false;
            Assert.That(store.Save(), Is.True);
            var reloaded = NewStore();
            reloaded.Load();
            Assert.That(reloaded.Profiles.Count, Is.EqualTo(2));
            Assert.That(reloaded.Profiles[0].PlayCount, Is.EqualTo(1));
        }

        [Test]
        public void ValidEmptyListLoadsWithoutWarning()
        {
            storage.Seed("{\"schemaVersion\":1,\"profiles\":[]}");
            store.Load();
            Assert.That(store.Profiles, Is.Empty);
            Assert.That(store.Warning, Is.Empty);
        }

        private ProfileStore NewStore() => new ProfileStore(storage.Read, storage.Write, storage.Flush);

        private sealed class Storage
        {
            public readonly Dictionary<string, string> Pending = new Dictionary<string, string>();
            public readonly Dictionary<string, string> Persisted = new Dictionary<string, string>();
            public bool FailRead;
            public string FailWriteKey;
            public bool FailFlush;

            public void Seed(string raw)
            {
                Pending[ProfileStore.StorageKey] = raw;
                Persisted[ProfileStore.StorageKey] = raw;
            }

            public string Read(string key)
            {
                if (FailRead) throw new InvalidOperationException("read failed");
                return Pending.TryGetValue(key, out var value) ? value : null;
            }

            public void Write(string key, string value)
            {
                if (key == FailWriteKey) throw new InvalidOperationException("write failed");
                Pending[key] = value;
            }

            public void Flush()
            {
                if (FailFlush) throw new InvalidOperationException("flush failed");
                foreach (var entry in Pending) Persisted[entry.Key] = entry.Value;
            }
        }
    }
}
