using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace CPS.KitchenCatch.Tests
{
    public sealed class AppSessionTests
    {
        private readonly List<GameObject> created = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in created)
                if (go != null) Object.DestroyImmediate(go);
            created.Clear();
        }

        [UnityTest]
        public IEnumerator ActivatedSessionBecomesPersistentSingleton()
        {
            var session = CreateSession(out _);
            yield return null;
            Assert.That(AppSession.Instance, Is.SameAs(session));
            Assert.That(session.gameObject.scene.name, Is.EqualTo("DontDestroyOnLoad"));
        }

        [UnityTest]
        public IEnumerator DuplicateCannotReplaceOriginalSelectionOrStore()
        {
            var first = CreateSession(out var store);
            var profile = store.Create("Arc");
            first.Select(profile.PlayerId, DifficultyId.Hard);
            var duplicate = CreateSession(out _);
            yield return null;
            Assert.That(duplicate == null, Is.True);
            Assert.That(AppSession.Instance, Is.SameAs(first));
            Assert.That(first.Store, Is.SameAs(store));
            Assert.That(first.SelectedProfile, Is.SameAs(profile));
            Assert.That(first.SelectedDifficulty, Is.EqualTo(DifficultyId.Hard));
        }

        [UnityTest]
        public IEnumerator SceneChangesKeepSelectionAndCompletedSessionIds()
        {
            var originalScene = SceneManager.GetActiveScene();
            var session = CreateSession(out var store);
            var profile = store.Create("Arc");
            session.Select(profile.PlayerId, DifficultyId.Normal);
            store.RecordResult(profile.PlayerId, "same-session");
            var temporary = SceneManager.CreateScene("Task3_ProfilePersistence");
            SceneManager.SetActiveScene(temporary);
            yield return SceneManager.UnloadSceneAsync(temporary);
            SceneManager.SetActiveScene(originalScene);
            Assert.That(AppSession.Instance, Is.SameAs(session));
            Assert.That(session.gameObject.scene.name, Is.EqualTo("DontDestroyOnLoad"));
            Assert.That(session.SelectedProfile, Is.SameAs(profile));
            Assert.That(session.SelectedDifficulty, Is.EqualTo(DifficultyId.Normal));
            Assert.That(store.RecordResult(profile.PlayerId, "same-session"), Is.False);
            Assert.That(profile.PlayCount, Is.EqualTo(1));
        }

        [Test]
        public void InvalidSelectionDoesNotReplaceCurrentChoice()
        {
            var session = CreateSession(out var store);
            var profile = store.Create("Arc");
            session.Select(profile.PlayerId, DifficultyId.Easy);
            Assert.Throws<ArgumentException>(() => session.Select("missing", DifficultyId.Hard));
            Assert.Throws<ArgumentException>(() => session.Select(profile.PlayerId, (DifficultyId)999));
            Assert.That(session.SelectedProfile, Is.SameAs(profile));
            Assert.That(session.SelectedDifficulty, Is.EqualTo(DifficultyId.Easy));
        }

        [Test]
        public void InitializeLoadsProfilesWithoutNavigatingAndCannotReplaceStore()
        {
            string sceneBefore = SceneManager.GetActiveScene().name;
            var values = new Dictionary<string, string>();
            var initial = new ProfileStore(key => values.TryGetValue(key, out var raw) ? raw : null,
                (key, raw) => values[key] = raw, () => { });
            initial.Load();
            var profile = initial.Create("Arc");
            var store = new ProfileStore(key => values.TryGetValue(key, out var raw) ? raw : null,
                (key, raw) => values[key] = raw, () => { });
            var go = new GameObject("AppSession");
            created.Add(go);
            go.SetActive(false);
            var session = go.AddComponent<AppSession>();
            session.Initialize(store);
            go.SetActive(true);
            Assert.That(session.Store.Profiles[0].PlayerId, Is.EqualTo(profile.PlayerId));
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(sceneBefore));
            Assert.Throws<InvalidOperationException>(() => session.Initialize(initial));
            Assert.That(session.Store, Is.SameAs(store));
            Object.DestroyImmediate(go);
            Assert.That(AppSession.Instance == null, Is.True);
        }

        private AppSession CreateSession(out ProfileStore store)
        {
            var values = new Dictionary<string, string>();
            store = new ProfileStore(key => values.TryGetValue(key, out var raw) ? raw : null,
                (key, raw) => values[key] = raw, () => { });
            var go = new GameObject("AppSession");
            created.Add(go);
            go.SetActive(false);
            var session = go.AddComponent<AppSession>();
            session.Initialize(store);
            go.SetActive(true);
            return session;
        }
    }
}
