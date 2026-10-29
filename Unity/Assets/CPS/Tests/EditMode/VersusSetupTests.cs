using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CPS.KitchenCatch.Tests
{
    public sealed class VersusSetupTests
    {
        private GameObject sessionObject;

        [TearDown]
        public void TearDown()
        {
            if (sessionObject != null) Object.DestroyImmediate(sessionObject);
        }

        [Test]
        public void ConfigureVersusSnapshotsDistinctProfilesAndCanonicalColors()
        {
            var session = CreateSession(out var store);
            var first = store.Create("Arc");
            var second = store.Create("Arc");

            session.ConfigureVersus(first.PlayerId, second.PlayerId,
                "#fa7d30", "#112233", "#3c78d8", "#abcdef");
            store.Rename(first.PlayerId, "Renamed");

            Assert.That(session.SelectedGameMode, Is.EqualTo(GameMode.Versus));
            Assert.That(session.VersusSetup.Player1.PlayerId, Is.EqualTo(first.PlayerId));
            Assert.That(session.VersusSetup.Player1.DisplayName, Is.EqualTo("Arc"));
            Assert.That(session.VersusSetup.Player1.HatColorHex, Is.EqualTo("#FA7D30"));
            Assert.That(session.VersusSetup.Player1.ShoeColorHex, Is.EqualTo("#112233"));
            Assert.That(session.VersusSetup.Player2.PlayerId, Is.EqualTo(second.PlayerId));
            Assert.That(session.VersusSetup.Player2.DisplayName, Is.EqualTo("Arc"));
            Assert.That(session.VersusSetup.Player2.HatColorHex, Is.EqualTo("#3C78D8"));
            Assert.That(session.VersusSetup.Player2.ShoeColorHex, Is.EqualTo("#ABCDEF"));

            var copy = session.VersusSetup.Copy();
            Assert.That(copy, Is.Not.SameAs(session.VersusSetup));
            Assert.That(copy.Player1, Is.Not.SameAs(session.VersusSetup.Player1));
            Assert.That(copy.Player1.DisplayName, Is.EqualTo("Arc"));
        }

        [Test]
        public void InvalidVersusSelectionDoesNotReplaceExistingSetup()
        {
            var session = CreateSession(out var store);
            var first = store.Create("Arc");
            var second = store.Create("Tet");
            session.ConfigureVersus(first.PlayerId, second.PlayerId,
                "#FA7D30", "#FA7D30", "#3C78D8", "#3C78D8");
            var original = session.VersusSetup;

            Assert.Throws<ArgumentException>(() => session.ConfigureVersus(
                first.PlayerId, first.PlayerId, "#FA7D30", "#FA7D30", "#3C78D8", "#3C78D8"));
            Assert.Throws<ArgumentException>(() => session.ConfigureVersus(
                first.PlayerId, "missing", "#FA7D30", "#FA7D30", "#3C78D8", "#3C78D8"));
            Assert.Throws<ArgumentException>(() => session.ConfigureVersus(
                "", second.PlayerId, "#FA7D30", "#FA7D30", "#3C78D8", "#3C78D8"));
            Assert.Throws<ArgumentException>(() => session.ConfigureVersus(
                first.PlayerId, second.PlayerId, "orange", "#FA7D30", "#3C78D8", "#3C78D8"));
            Assert.Throws<ArgumentException>(() => session.ConfigureVersus(
                first.PlayerId, second.PlayerId, "#FFF", "#FA7D30", "#3C78D8", "#3C78D8"));

            Assert.That(session.VersusSetup, Is.SameAs(original));
        }

        [Test]
        public void SinglePlayerSelectionClearsVersusSetup()
        {
            var session = CreateSession(out var store);
            var first = store.Create("Arc");
            var second = store.Create("Tet");
            session.ConfigureVersus(first.PlayerId, second.PlayerId,
                "#FA7D30", "#FA7D30", "#3C78D8", "#3C78D8");

            session.Select(first.PlayerId, DifficultyId.Normal);

            Assert.That(session.SelectedGameMode, Is.EqualTo(GameMode.SinglePlayer));
            Assert.That(session.VersusSetup, Is.Null);
            Assert.That(session.SelectedProfile, Is.SameAs(first));
        }

        private AppSession CreateSession(out ProfileStore store)
        {
            var values = new Dictionary<string, string>();
            store = new ProfileStore(key => values.TryGetValue(key, out var raw) ? raw : null,
                (key, raw) => values[key] = raw, () => { });
            sessionObject = new GameObject("VersusSetupTestSession");
            sessionObject.SetActive(false);
            var session = sessionObject.AddComponent<AppSession>();
            session.Initialize(store);
            return session;
        }
    }
}
