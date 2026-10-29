using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace CPS.KitchenCatch.Tests
{
    public sealed class FallingObjectTests
    {
        private readonly List<Object> owned = new List<Object>();
        private FallingObject falling;
        private FallingObjectDefinition food;
        [SetUp]
        public void SetUp()
        {
            food = Own(ScriptableObject.CreateInstance<FallingObjectDefinition>());
            food.kind = ObjectKind.Food;
            falling = Own(new GameObject("Falling")).AddComponent<FallingObject>();
        }
        [TearDown] public void TearDown() { foreach (var item in owned) Object.DestroyImmediate(item); owned.Clear(); }
        private T Own<T>(T value) where T : Object { owned.Add(value); return value; }

        [TestCase(1f / 30)] [TestCase(1f)]
        public void SweptCrossingCatchesExactlyOnceBeforeMiss(float dt)
        {
            int reports = 0;
            ObjectOutcome? outcome = null;
            falling.Spawn(1, food, new Vector2(0, 0.5f), 30);
            falling.BindResolution((id, definition, result) => { reports++; outcome = result; });
            falling.Step(dt, new Bounds(Vector3.zero, new Vector3(1.5f, 0.5f, 0.2f)));
            falling.Step(dt, new Bounds(Vector3.zero, new Vector3(1.5f, 0.5f, 0.2f)));
            Assert.That(outcome, Is.EqualTo(ObjectOutcome.Caught));
            Assert.That(reports, Is.EqualTo(1));
        }

        [Test]
        public void DefaultPlayerCatchZoneUsesApprovedForgivingSize()
        {
            var player = Own(new GameObject("Player")).AddComponent<PlayerController>();

            Assert.That(player.CatchZone.size, Is.EqualTo(new Vector3(2f, .8f, .2f)));
        }

        [Test]
        public void CrossingOutsideCatchWidthReportsOneMiss()
        {
            var outcomes = new List<ObjectOutcome>();
            falling.Spawn(1, food, new Vector2(2, 1), 3);
            falling.BindResolution((id, definition, result) => outcomes.Add(result));
            falling.Step(1, new Bounds(Vector3.zero, new Vector3(1.5f, 0.5f, 0.2f)));
            falling.Step(1, new Bounds(Vector3.zero, new Vector3(1.5f, 0.5f, 0.2f)));
            Assert.That(outcomes, Is.EqualTo(new[] { ObjectOutcome.Missed }));
        }

        [Test]
        public void ReuseRejectsStaleIdAndClearsOldCallback()
        {
            int oldReports = 0, newReports = 0;
            falling.Spawn(10, food, new Vector2(0, 7), 2);
            falling.BindResolution((id, definition, outcome) => oldReports++);
            falling.Despawn();
            falling.Spawn(11, food, new Vector2(0, 7), 2);
            Assert.That(falling.TryResolve(10, ObjectOutcome.Caught), Is.False);
            falling.BindResolution((id, definition, outcome) => newReports++);
            Assert.That(falling.TryResolve(11, ObjectOutcome.Caught), Is.True);
            Assert.That(falling.TryResolve(11, ObjectOutcome.Missed), Is.False);
            Assert.That(oldReports, Is.Zero);
            Assert.That(newReports, Is.EqualTo(1));
        }

        [Test]
        public void SpawnerClearReuseKeepsIdsFreshAndOldReportSilent()
        {
            var spawner = Own(new GameObject("Spawner")).AddComponent<FallingObjectSpawner>();
            var player = Own(new GameObject("Player")).AddComponent<PlayerController>();
            var config = Own(ScriptableObject.CreateInstance<DifficultyConfig>());
            config.difficulty = DifficultyId.Easy; config.speed = 2; config.interval = 1.4f;
            config.lanes = new[] { -6f, 0, 6 };
            var trash = Own(ScriptableObject.CreateInstance<FallingObjectDefinition>()); trash.kind = ObjectKind.Trash;
            Set(spawner, "player", player); Set(spawner, "prefab", falling);
            Set(spawner, "foodDefinitions", new[] { food, food, food });
            Set(spawner, "trashDefinitions", new[] { trash, trash });
            int oldReports = 0;
            spawner.Begin(config, (id, definition, outcome) => oldReports++);
            spawner.Step(0.01f);
            Assert.That(spawner.ActiveCount, Is.EqualTo(1));
            var pooled = spawner.GetComponentInChildren<FallingObject>();
            long oldId = pooled.SpawnId;
            spawner.StopAndClear();
            Assert.That(spawner.ActiveCount, Is.Zero);
            int newReports = 0;
            long newId = 0;
            spawner.Begin(config, (id, definition, outcome) => { newReports++; newId = id; });
            spawner.Step(0.01f);
            Assert.That(pooled.TryResolve(oldId, ObjectOutcome.Caught), Is.False);
            spawner.Step(4);
            Assert.That(oldReports, Is.Zero);
            Assert.That(newReports, Is.EqualTo(1));
            Assert.That(newId, Is.GreaterThan(oldId));
            spawner.StopAndClear();
            Assert.That(spawner.ActiveCount, Is.Zero);
        }

        [TestCase(1)] [TestCase(300)] [TestCase(600)] [TestCase(1200)]
        public void StepRatePreservesSpawnCadencePositionsAndSingleReports(int steps)
        {
            var spawner = CreateSpawner(out var config, out _);
            config.lanes = new[] { 0f };
            var ids = new HashSet<long>();
            spawner.Begin(config, (id, definition, outcome) => Assert.That(ids.Add(id), Is.True));
            for (int i = 0; i < steps; i++) spawner.Step(10f / steps);
            Assert.That(ids.Count + spawner.ActiveCount, Is.EqualTo(8));
            Assert.That(ids.Count, Is.EqualTo(5));
            var remaining = new List<float>();
            foreach (var item in spawner.GetComponentsInChildren<FallingObject>()) remaining.Add(item.transform.position.y);
            remaining.Sort();
            Assert.That(remaining, Is.EqualTo(new[] { 1.5f, 4.3f, 7.1f }).Within(0.01f));
        }

        [Test]
        public void ValidationNamesMissingReferencesAndInvalidCatchGeometryWithoutStartingPool()
        {
            var empty = Own(new GameObject("EmptySpawner")).AddComponent<FallingObjectSpawner>();
            Assert.That(empty.GetValidationError(null), Does.Contain("player"));
            var spawner = CreateSpawner(out var config, out var player);
            Set(player, "catchZoneHalfExtents", new Vector3(float.NaN, 0.25f, 0.1f));
            Assert.That(spawner.GetValidationError(config), Is.Not.Null);
            Assert.That(spawner.GetValidationError(config), Does.Contain("Catch zone"));
            Assert.That(spawner.ActiveCount, Is.Zero);
        }

        [Test]
        public void ActiveObjectsStayBoundedAndClearDuringOutcomeCallbackIsSafe()
        {
            var spawner = CreateSpawner(out var config, out var player);
            config.lanes = new[] { 0f }; config.interval = 0.0001f;
            config.speed = 0.1f;
            Set(player, "catchZoneHalfExtents", new Vector3(0.75f, 0, 0.1f));
            spawner.Begin(config, (id, definition, outcome) => spawner.StopAndClear());
            for (int i = 0; i < 30; i++) spawner.Step(0.1f);
            Assert.That(spawner.ActiveCount, Is.EqualTo(16));
            spawner.Step(100);
            Assert.That(spawner.ActiveCount, Is.Zero);
        }

        [Test]
        public void WiderMascotRejectsImpossibleAuthoredLaneBeforeBegin()
        {
            var spawner = CreateSpawner(out var config, out var player);
            Set(player, "mascotHalfWidth", 3f);
            player.Step(new InputSnapshot(default, new HandInput(true, 1, true, null), "Test"), 10);
            Assert.That(player.transform.position.x, Is.EqualTo(5));
            Assert.That(player.CatchZone.max.x, Is.EqualTo(6f));
            Assert.That(spawner.GetValidationError(config), Is.Not.Null);
            Assert.That(spawner.GetValidationError(config), Does.Contain("lane"));
            Assert.Throws<System.InvalidOperationException>(() => spawner.Begin(config, (id, definition, outcome) => { }));
            Assert.That(spawner.ActiveCount, Is.Zero);
        }

        [Test]
        public void LaneInsideCatcherCoverageButOutsideCenteredPathIsRejected()
        {
            var spawner = CreateSpawner(out var config, out var player);
            Set(player, "mascotHalfWidth", 3f);
            config.lanes = new[] { 5.5f };
            Assert.That(spawner.GetValidationError(config), Is.Not.Null);
            Assert.That(spawner.GetValidationError(config), Does.Contain("lane"));
            config.lanes = new[] { -5f, 5f };
            Assert.That(spawner.GetValidationError(config), Is.Null);
        }

        [TestCase(-1f)] [TestCase(1f)]
        public void UnsupportedHorizontalCatcherOffsetIsRejected(float offset)
        {
            var spawner = CreateSpawner(out var config, out var player);
            Set(player, "catchZoneOffset", new Vector3(offset, 0.5f, 0));
            Assert.That(spawner.GetValidationError(config), Is.Not.Null);
            Assert.That(spawner.GetValidationError(config), Does.Contain("horizontal offset"));
        }

        [Test]
        public void NonfinitePlayerMovementRangeIsRejected()
        {
            var spawner = CreateSpawner(out var config, out var player);
            Set(player, "mascotHalfWidth", float.NaN);
            Assert.That(spawner.GetValidationError(config), Is.Not.Null);
            Assert.That(spawner.GetValidationError(config), Does.Contain("movement bounds"));
        }

        private FallingObjectSpawner CreateSpawner(out DifficultyConfig config, out PlayerController player)
        {
            var spawner = Own(new GameObject("Spawner")).AddComponent<FallingObjectSpawner>();
            player = Own(new GameObject("Player")).AddComponent<PlayerController>();
            config = Own(ScriptableObject.CreateInstance<DifficultyConfig>());
            config.difficulty = DifficultyId.Easy; config.speed = 2; config.interval = 1.4f;
            config.lanes = new[] { -6f, 0, 6 };
            var trash = Own(ScriptableObject.CreateInstance<FallingObjectDefinition>()); trash.kind = ObjectKind.Trash;
            Set(spawner, "player", player); Set(spawner, "prefab", falling);
            Set(spawner, "foodDefinitions", new[] { food, food, food });
            Set(spawner, "trashDefinitions", new[] { trash, trash });
            return spawner;
        }

        private static void Set(object instance, string field, object value)
        { instance.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(instance, value); }
    }
}
