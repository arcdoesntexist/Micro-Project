using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

namespace CPS.KitchenCatch.Tests
{
    public sealed class GameSessionFlowTests : InputTestFixture
    {
        private readonly List<Object> owned = new List<Object>();
        private GameSessionController controller;
        private FallingObjectSpawner spawner;
        private PlayerController player;
        private DifficultyConfig config;
        private AppSession app;
        private InputSource input;

        [SetUp]
        public override void Setup()
        {
            base.Setup();
            var values = new Dictionary<string, string>();
            var store = new ProfileStore(key => values.TryGetValue(key, out var value) ? value : null,
                (key, value) => values[key] = value, () => { });
            var appObject = Own(new GameObject("Injected AppSession"));
            appObject.SetActive(false);
            app = appObject.AddComponent<AppSession>();
            app.Initialize(store);
            appObject.SetActive(true);
            var profile = store.Create("Flow Player");
            app.Select(profile.PlayerId, DifficultyId.Easy);
            player = Own(new GameObject("Player")).AddComponent<PlayerController>();
            var prefab = Own(new GameObject("Prefab")).AddComponent<FallingObject>();
            prefab.gameObject.SetActive(false);
            config = Own(ScriptableObject.CreateInstance<DifficultyConfig>());
            config.difficulty = DifficultyId.Easy; config.speed = 0.1f;
            config.interval = 1; config.lanes = new[] { 0f };
            var food = Own(ScriptableObject.CreateInstance<FallingObjectDefinition>());
            food.kind = ObjectKind.Food; food.points = 10;
            var trash = Own(ScriptableObject.CreateInstance<FallingObjectDefinition>());
            trash.kind = ObjectKind.Trash;
            spawner = Own(new GameObject("Spawner")).AddComponent<FallingObjectSpawner>();
            Set(spawner, "player", player); Set(spawner, "prefab", prefab);
            Set(spawner, "foodDefinitions", new[] { food, food, food });
            Set(spawner, "trashDefinitions", new[] { trash, trash });
            controller = Own(new GameObject("Controller")).AddComponent<GameSessionController>();
            controller.enabled = false;
            input = new InputSource();
            controller.Initialize(player, input, spawner, new[] { config }, app);
        }

        [TearDown]
        public void Cleanup()
        {
            for (int i = owned.Count - 1; i >= 0; i--)
                if (owned[i] != null) Object.DestroyImmediate(owned[i]);
            owned.Clear();
        }

        [Test]
        public void RealKeyboardFeedsOnlyActiveGameplayAndStopsMovementWhilePaused()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var adapter = player.gameObject.AddComponent<KeyboardInput>();
            controller.Initialize(player, adapter, spawner, new[] { config }, app);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D)); InputSystem.Update();
            controller.BeginRound(); controller.Step(3);
            Assert.That(player.transform.position.x, Is.Zero);
            controller.Step(.5f);
            Assert.That(player.transform.position.x, Is.EqualTo(3.5f).Within(.001));
            controller.Pause(); controller.Step(10);
            Assert.That(player.MovementDirection, Is.Zero);
            Assert.That(player.transform.position.x, Is.EqualTo(3.5f).Within(.001));
            controller.Resume(); controller.Step(3);
            Assert.That(controller.Model.PlayedSeconds, Is.EqualTo(.5f).Within(.001));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A)); InputSystem.Update();
            controller.Step(.5f);
            Assert.That(player.transform.position.x, Is.Zero.Within(.001));
        }

        [Test]
        public void ReadyResetsCenterAndOnlyStartsAfterThreeSeconds()
        {
            player.transform.position = new Vector3(5, -3, 0);
            Assert.That(controller.BeginRound(), Is.True);
            Assert.That(player.transform.position, Is.EqualTo(new Vector3(0, -3, 0)));
            Assert.That(controller.Model.Health.CurrentHp, Is.EqualTo(3));
            controller.Step(2.9f);
            Assert.That(controller.State, Is.EqualTo(SessionState.Ready));
            Assert.That(spawner.ActiveCount, Is.Zero);
            Assert.That(controller.Model.PlayedSeconds, Is.Zero);
            controller.Step(0.1f);
            Assert.That(controller.State, Is.EqualTo(SessionState.Playing));
            Assert.That(controller.Model.PlayedSeconds, Is.Zero);
        }

        [Test]
        public void TimeoutCapsInputAndPublishesIndependentFrozenResultsOnce()
        {
            controller.BeginRound(); controller.Step(3);
            input.Snapshot = new InputSnapshot(new HandInput(true, 1, true, null), default, "Keyboard");
            SessionResult observed = null;
            int emitted = 0;
            controller.Finished += result => { result.Score = -9; result.InputStatistics.Left.PressedDurationSeconds = -9; };
            controller.Finished += result => { emitted++; observed = result; };
            controller.Step(59); controller.Step(20);
            Assert.That(controller.State, Is.EqualTo(SessionState.Triumph));
            Assert.That(observed, Is.Not.Null);
            Assert.That(observed.PlayedSeconds, Is.EqualTo(60).Within(0.001));
            Assert.That(observed.InputStatistics.Left.PressedDurationSeconds, Is.EqualTo(60).Within(0.001));
            Assert.That(observed.Score, Is.Zero);
            Assert.That(observed.InputSource, Is.EqualTo("Keyboard"));
            Assert.That(observed.InputStatistics.Left.HasForce, Is.False);
            Assert.That(Guid.TryParse(observed.SessionId, out _), Is.True);
            Assert.That(DateTime.Parse(observed.EndedAtUtc, CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind).Kind, Is.EqualTo(DateTimeKind.Utc));
            Assert.That(observed.PlayerId, Is.EqualTo(app.SelectedProfile.PlayerId));
            Assert.That(observed.DisplayName, Is.EqualTo("Flow Player"));
            observed.InputStatistics.Left.PressedDurationSeconds = 999;
            var copy = controller.LastResult; copy.Score = 999;
            Assert.That(controller.LastResult.Score, Is.Zero);
            Assert.That(controller.LastResult.InputStatistics.Left.PressedDurationSeconds, Is.EqualTo(60).Within(0.001));
            Assert.That(spawner.ActiveCount, Is.Zero);
            controller.Step(1.19f);
            Assert.That(controller.State, Is.EqualTo(SessionState.Triumph));
            controller.Step(0.01f); controller.Step(10);
            Assert.That(controller.State, Is.EqualTo(SessionState.Results));
            Assert.That(emitted, Is.EqualTo(1));
            Assert.That(app.SelectedProfile.PlayCount, Is.EqualTo(1));
        }

        [Test]
        public void FinalStepDamageWinsTimeoutAndDeathDelayCannotBePaused()
        {
            controller.BeginRound(); controller.Step(3); controller.Step(59);
            ResolveFoodMisses(3);
            controller.Step(10);
            Assert.That(controller.State, Is.EqualTo(SessionState.Dying));
            Assert.That(controller.LastResult.EndReason, Is.EqualTo(EndReason.HpDepleted));
            Assert.That(controller.LastResult.PlayedSeconds, Is.EqualTo(60).Within(0.001));
            controller.Pause();
            Assert.That(controller.IsPaused, Is.False);
            controller.Step(1.39f);
            Assert.That(controller.State, Is.EqualTo(SessionState.Dying));
            controller.Step(0.01f);
            Assert.That(controller.State, Is.EqualTo(SessionState.Results));
        }

        [Test]
        public void PauseAndResumeCountdownFreezePlayerObjectsTimeAndStatistics()
        {
            controller.BeginRound(); controller.Step(1); controller.Pause(); controller.Step(50);
            Assert.That(controller.IsPaused, Is.True);
            Assert.That(controller.State, Is.EqualTo(SessionState.Ready));
            controller.Resume(); controller.Step(2.9f);
            Assert.That(controller.State, Is.EqualTo(SessionState.Ready));
            controller.Step(0.1f); controller.Step(1);
            var active = spawner.GetComponentsInChildren<FallingObject>();
            var objectPosition = active[0].transform.position;
            var played = controller.Model.PlayedSeconds;
            controller.Pause(); controller.Step(20);
            Assert.That(active[0].transform.position, Is.EqualTo(objectPosition));
            Assert.That(controller.Model.PlayedSeconds, Is.EqualTo(played));
            controller.Resume(); controller.Step(3);
            Assert.That(controller.Model.PlayedSeconds, Is.EqualTo(played));
            controller.Step(1);
            Assert.That(controller.Model.PlayedSeconds, Is.EqualTo(played + 1));
        }

        [Test]
        public void ReturningToMenuFromPauseDiscardsRoundWithoutSaving()
        {
            controller.BeginRound();
            controller.Step(3);
            controller.Step(5);
            controller.Pause();
            float played = controller.Model.PlayedSeconds;

            controller.ReturnToMenu();
            controller.Step(20);

            Assert.That(controller.LastResult, Is.Null);
            Assert.That(app.SelectedProfile.PlayCount, Is.Zero);
            Assert.That(controller.Model.PlayedSeconds, Is.EqualTo(played));
            Assert.That(spawner.ActiveCount, Is.Zero);
        }

        [Test]
        public void FocusReturnRequiresExplicitResume()
        {
            controller.BeginRound(); controller.Step(3);
            controller.SendMessage("OnApplicationFocus", false);
            controller.SendMessage("OnApplicationFocus", true);
            controller.Step(20);
            Assert.That(controller.IsPaused, Is.True);
            Assert.That(controller.Model.PlayedSeconds, Is.Zero);
        }

        [Test]
        public void TenRestartsClearObjectsAndKeepOneSelectionAndPublicationPerRound()
        {
            int emitted = 0; controller.Finished += result => emitted++;
            string selected = app.SelectedProfile.PlayerId;
            controller.BeginRound();
            for (int i = 0; i < 10; i++)
            {
                controller.Step(3); controller.Step(20); ResolveFoodMisses(3); controller.Step(0.1f);
                Assert.That(controller.State, Is.EqualTo(SessionState.Dying));
                var old = spawner.GetComponentsInChildren<FallingObject>(true);
                controller.Restart();
                Assert.That(controller.State, Is.EqualTo(SessionState.Ready));
                Assert.That(controller.LastResult, Is.Null);
                Assert.That(controller.Model.Health.CurrentHp, Is.EqualTo(3));
                Assert.That(controller.Model.Score.Score, Is.Zero);
                Assert.That(spawner.ActiveCount, Is.Zero);
                foreach (var item in old) Assert.That(item.TryResolve(item.SpawnId, ObjectOutcome.Missed), Is.False);
            }
            Assert.That(emitted, Is.EqualTo(10));
            Assert.That(app.SelectedProfile.PlayCount, Is.EqualTo(10));
            Assert.That(app.SelectedProfile.PlayerId, Is.EqualTo(selected));
            Assert.That(Object.FindObjectsByType<AppSession>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
        }

        [Test]
        public void MissingDependencyRejectsBeginWithSharedNamedValidation()
        {
            Set(spawner, "prefab", null);
            Assert.That(controller.GetValidationError(app), Is.Not.Null);
            Assert.That(controller.GetValidationError(app), Does.Contain("prefab"));
            Assert.That(controller.BeginRound(), Is.False);
            Assert.That(spawner.ActiveCount, Is.Zero);
        }

        [Test]
        public void DifferentSpawnerPlayerRejectsBeginBeforeRoundStarts()
        {
            var other = Own(new GameObject("Other catcher")).AddComponent<PlayerController>();
            Set(spawner, "player", other);
            string error = controller.GetValidationError(app);
            Assert.That(error, Is.Not.Null);
            Assert.That(error, Does.Contain("player"));
            Assert.That(controller.BeginRound(), Is.False);
            Assert.That(controller.Model.PlayedSeconds, Is.Zero);
            Assert.That(spawner.ActiveCount, Is.Zero);
        }

        [Test]
        public void RoundSnapshotsAuthoredDifficultyInsteadOfFollowingMutableAsset()
        {
            controller.BeginRound(); config.speed = 100; config.interval = 0.001f;
            controller.Step(3); controller.Step(1);
            Assert.That(spawner.ActiveCount, Is.EqualTo(1));
            Assert.That(controller.Model.Health.CurrentHp, Is.EqualTo(3));
        }

        private void ResolveFoodMisses(int count)
        {
            foreach (var item in spawner.GetComponentsInChildren<FallingObject>())
                if (item.IsActive && item.TryResolve(item.SpawnId, ObjectOutcome.Missed) && --count == 0) return;
            Assert.Fail("Fixture requires enough real active falling food.");
        }
        private T Own<T>(T value) where T : Object { owned.Add(value); return value; }
        private static void Set(object instance, string name, object value) =>
            instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(instance, value);
        private sealed class InputSource : IPlayerInput
        {
            public InputSnapshot Snapshot = new InputSnapshot(new HandInput(false, 0, true, null),
                new HandInput(false, 0, true, null), "Keyboard");
            public InputSnapshot Read() => Snapshot;
        }
    }
}
