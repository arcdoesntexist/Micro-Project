using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace CPS.KitchenCatch.Tests
{
    public sealed class SpawnPolicyTests
    {
        private DifficultyConfig config;
        [SetUp] public void SetUp() { config = ScriptableObject.CreateInstance<DifficultyConfig>(); }
        [TearDown] public void TearDown() { UnityEngine.Object.DestroyImmediate(config); }

        private void Configure(DifficultyId difficulty)
        {
            config.difficulty = difficulty;
            config.speed = difficulty == DifficultyId.Easy ? 2 : difficulty == DifficultyId.Normal ? 2.8f : 3.5f;
            config.interval = difficulty == DifficultyId.Easy ? 1.4f : difficulty == DifficultyId.Normal ? 1.1f : 0.85f;
            config.trashProbability = difficulty == DifficultyId.Easy ? 0 : difficulty == DifficultyId.Normal ? 0.15f : 0.25f;
            config.lanes = difficulty == DifficultyId.Easy ? new[] { -6f, 0, 6 } :
                difficulty == DifficultyId.Normal ? new[] { -6f, -3, 0, 3, 6 } : new[] { -6f, -4, -2, 0, 2, 4, 6 };
        }

        [Test]
        public void EasyWrapDefersWithoutSkippingCyclicLane()
        {
            Configure(DifficultyId.Easy);
            var policy = new SpawnPolicy(config, 17);
            Assert.That(policy.TryChoose(0, 7, 0.75f, out var first), Is.True);
            Assert.That(first.X, Is.EqualTo(-6));
            Assert.That(policy.TryChoose(1.4f, 7, 0.75f, out var second), Is.True);
            Assert.That(second.X, Is.Zero);
            Assert.That(policy.TryChoose(2.8f, 7, 0.75f, out var third), Is.True);
            Assert.That(third.X, Is.EqualTo(6));
            Assert.That(policy.TryChoose(4.2f, 7, 0.75f, out _), Is.False);
            Assert.That(policy.TryChoose(5f, 7, 0.75f, out var wrap), Is.True);
            Assert.That(wrap.X, Is.EqualTo(-6));
            Assert.That(wrap.Kind, Is.EqualTo(ObjectKind.Food));
        }

        [TestCase(DifficultyId.Easy)]
        [TestCase(DifficultyId.Normal)]
        [TestCase(DifficultyId.Hard)]
        public void ThousandSpawnsHaveReproducibleSafeCenteredCatchPath(DifficultyId difficulty)
        {
            Configure(difficulty);
            var policy = new SpawnPolicy(config, 723);
            var duplicate = new SpawnPolicy(config, 723);
            var accepted = new List<SpawnChoice>();
            var counts = new int[3];
            var trashCounts = new int[2];
            float now = 0;
            for (int attempt = 0; accepted.Count < 1000 && attempt < 100000; attempt++, now += 0.05f)
            {
                bool selected = policy.TryChoose(now, 7, 0.75f, out var choice);
                Assert.That(duplicate.TryChoose(now, 7, 0.75f, out var same), Is.EqualTo(selected));
                if (!selected) continue;
                Assert.That(same, Is.EqualTo(choice));
                Assert.That(choice.X, Is.InRange(-7.5f, 7.5f));
                Assert.That(choice.ArrivalTime, Is.EqualTo(now + 7 / config.speed).Within(0.001f));
                if (difficulty == DifficultyId.Easy) Assert.That(choice.Kind, Is.EqualTo(ObjectKind.Food));
                if (choice.Kind == ObjectKind.Food) counts[choice.DefinitionIndex]++;
                else trashCounts[choice.DefinitionIndex]++;
                if (difficulty == DifficultyId.Easy) Assert.That(choice.X, Is.EqualTo(new[] { -6f, 0, 6 }[accepted.Count % 3]));
                accepted.Add(choice);
            }
            Assert.That(accepted.Count, Is.EqualTo(1000));
            var foods = accepted.FindAll(c => c.Kind == ObjectKind.Food);
            foreach (int count in counts)
                Assert.That((float)count / foods.Count, Is.EqualTo(1f / 3f).Within(0.07f));
            if (difficulty != DifficultyId.Easy)
                foreach (int count in trashCounts) Assert.That(count, Is.GreaterThan(30));
            foods.Insert(0, new SpawnChoice(ObjectKind.Food, 0, 0, 0));
            float window = 0.25f / config.speed;
            for (int i = 1; i < foods.Count; i++)
            {
                var before = foods[i - 1];
                var after = foods[i];
                float departure = Mathf.Max(before.ArrivalTime + window, after.ArrivalTime - 7 / config.speed);
                float travelTime = after.ArrivalTime - window - departure;
                Assert.That(Mathf.Abs(after.X - before.X), Is.LessThanOrEqualTo(7 * travelTime + 0.001f));
                foreach (var trash in accepted)
                {
                    if (trash.Kind != ObjectKind.Trash || trash.ArrivalTime + window < before.ArrivalTime ||
                        trash.ArrivalTime - window > after.ArrivalTime) continue;
                    float lowTime = Mathf.Max(before.ArrivalTime, trash.ArrivalTime - window);
                    float highTime = Mathf.Min(after.ArrivalTime, trash.ArrivalTime + window);
                    float lowX = PathX(before, after, lowTime, window, departure);
                    float highX = PathX(before, after, highTime, window, departure);
                    Assert.That(trash.X < Mathf.Min(lowX, highX) - 0.75f ||
                        trash.X > Mathf.Max(lowX, highX) + 0.75f, Is.True,
                        $"Trash at {trash.X}/{trash.ArrivalTime} crosses food path {before.X}->{after.X}");
                }
            }
        }

        private static float PathX(SpawnChoice before, SpawnChoice after, float time, float window, float departure)
        {
            float t = Mathf.Clamp01((time - departure) / (after.ArrivalTime - window - departure));
            return Mathf.Lerp(before.X, after.X, t);
        }

        [TestCase(0)] [TestCase(-1)] [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)]
        public void InvalidFallSpeedRejectsConfig(float speed)
        { Configure(DifficultyId.Normal); config.speed = speed; Assert.Throws<ArgumentException>(() => config.Validate()); }

        [TestCase(0)] [TestCase(-1)] [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)]
        public void InvalidIntervalRejectsConfig(float interval)
        { Configure(DifficultyId.Normal); config.interval = interval; Assert.Throws<ArgumentException>(() => config.Validate()); }

        [TestCase(-0.1f)] [TestCase(1.1f)] [TestCase(float.NaN)]
        public void InvalidTrashProbabilityRejectsConfig(float probability)
        { Configure(DifficultyId.Normal); config.trashProbability = probability; Assert.Throws<ArgumentException>(() => config.Validate()); }

        [TestCase(8)] [TestCase(float.NaN)] [TestCase(float.NegativeInfinity)]
        public void UnreachableLaneRejectsConfig(float lane)
        { Configure(DifficultyId.Normal); config.lanes = new[] { lane }; Assert.Throws<ArgumentException>(() => config.Validate()); }

        [Test]
        public void EmptyLanesAndInvalidCatchGeometryRejectPolicy()
        {
            Configure(DifficultyId.Normal); config.lanes = Array.Empty<float>();
            Assert.Throws<ArgumentException>(() => new SpawnPolicy(config, 1));
            Configure(DifficultyId.Normal);
            Assert.Throws<ArgumentException>(() => new SpawnPolicy(config, 1, float.NaN));
            Assert.Throws<ArgumentException>(() => new SpawnPolicy(config, 1, -1));
        }
    }
}
