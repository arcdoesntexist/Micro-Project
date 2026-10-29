using System.Collections.Generic;
using NUnit.Framework;

namespace CPS.KitchenCatch.Tests
{
    public sealed class VersusSpawnStreamTests
    {
        [Test]
        public void SameSeedProducesIdenticalImmutableSchedule()
        {
            var first = Generate(1234, 90);
            var second = Generate(1234, 90);

            Assert.That(second, Has.Count.EqualTo(first.Count));
            for (int index = 0; index < first.Count; index++)
                Assert.That(second[index], Is.EqualTo(first[index]));
        }

        [Test]
        public void DifferentSeedChangesTheSchedule()
        {
            var first = Generate(1234, 30);
            var second = Generate(4321, 30);

            Assert.That(second, Is.Not.EqualTo(first));
        }

        [Test]
        public void ChallengeStartsGentleAndCapsDuringLongMatches()
        {
            var challenge = new VersusChallenge();

            var start = challenge.At(0);
            var middle = challenge.At(40);
            var capped = challenge.At(10000);

            Assert.That(start.Tier, Is.Zero);
            Assert.That(start.IntervalSeconds, Is.EqualTo(1.2f).Within(0.0001f));
            Assert.That(start.TrashProbability, Is.EqualTo(0.2f).Within(0.0001f));
            Assert.That(middle.Tier, Is.EqualTo(2));
            Assert.That(middle.IntervalSeconds, Is.LessThan(start.IntervalSeconds));
            Assert.That(middle.FallSpeed, Is.GreaterThan(start.FallSpeed));
            Assert.That(capped.Tier, Is.EqualTo(8));
            Assert.That(capped.IntervalSeconds, Is.EqualTo(0.56f).Within(0.0001f));
            Assert.That(capped.TrashProbability, Is.EqualTo(0.6f).Within(0.0001f));
            Assert.That(capped.FallSpeed, Is.EqualTo(8.3f).Within(0.0001f));
        }

        [Test]
        public void RecordsStayBoundedAndUseThreeFoodTwoTrashDefinitions()
        {
            var records = Generate(7, 10000);

            Assert.That(records.Count, Is.GreaterThan(100));
            long expectedId = 1;
            foreach (var record in records)
            {
                Assert.That(record.Id, Is.EqualTo(expectedId++));
                Assert.That(record.NormalizedX, Is.InRange(-1f, 1f));
                Assert.That(record.FallSpeed, Is.InRange(5.5f, 8.3f));
                Assert.That(record.SpawnTime, Is.GreaterThanOrEqualTo(0));
                Assert.That(record.DefinitionIndex, Is.InRange(0,
                    record.Kind == ObjectKind.Food ? 2 : 1));
            }
        }

        [Test]
        public void TrashLanesCoverTheEntireReachableFieldWithoutPermanentSafeGaps()
        {
            var lanes = new HashSet<float>();
            foreach (var record in Generate(7, 10000))
                if (record.Kind == ObjectKind.Trash) lanes.Add(record.NormalizedX);
            for (float x = -7.1f; x <= 7.1f; x += .05f)
            {
                bool covered = false;
                foreach (float lane in lanes)
                    if (System.Math.Abs(lane * 7.1f - x) <= 1) { covered = true; break; }
                Assert.That(covered, Is.True, "Permanent safe position at " + x);
            }
        }

        [Test]
        public void ConsumerOutcomesCannotChangeFutureRecords()
        {
            var left = new VersusSpawnStream(new VersusChallenge(), 88);
            var right = new VersusSpawnStream(new VersusChallenge(), 88);

            for (int second = 0; second <= 120; second++)
            {
                var leftRecords = Drain(left, second);
                var rightRecords = Drain(right, second);
                Assert.That(rightRecords, Is.EqualTo(leftRecords));
            }
        }

        private static List<VersusSpawnRecord> Generate(int seed, float duration)
        {
            var stream = new VersusSpawnStream(new VersusChallenge(), seed);
            return Drain(stream, duration);
        }

        private static List<VersusSpawnRecord> Drain(VersusSpawnStream stream, float activeSeconds)
        {
            var records = new List<VersusSpawnRecord>();
            while (stream.TryNext(activeSeconds, out var record)) records.Add(record);
            return records;
        }
    }
}
