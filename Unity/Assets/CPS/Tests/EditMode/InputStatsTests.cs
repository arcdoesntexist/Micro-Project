using NUnit.Framework;

namespace CPS.KitchenCatch.Tests
{
    public sealed class InputStatsTests
    {
        [Test]
        public void StrengthMeanIncludesReleaseAndUsesDuration()
        {
            var collector = new ForceStatsCollector();
            collector.Accumulate(Sample(new HandInput(true, 1, true, null)), 1);
            collector.Accumulate(Sample(new HandInput(false, 0, true, null)), 3);
            var stats = collector.Snapshot();
            Assert.That(stats.Left.HasStrength, Is.True);
            Assert.That(stats.Left.MeanStrength01, Is.EqualTo(0.25).Within(0.001));
            Assert.That(stats.Left.MaxStrength01, Is.EqualTo(1));
            Assert.That(stats.Left.ValidDurationSeconds, Is.EqualTo(4));
            Assert.That(stats.Left.PressedDurationSeconds, Is.EqualTo(1));
            Assert.That(stats.Left.HasForce, Is.False);
            Assert.That(stats.Left.ForceValidDurationSeconds, Is.Zero);
        }

        [Test]
        public void ForceMeanUsesOnlyForceAvailableDurationAndIncludesZero()
        {
            var collector = new ForceStatsCollector();
            collector.Accumulate(Sample(new HandInput(true, 1, true, 8)), 1);
            collector.Accumulate(Sample(new HandInput(false, 0, true, 0)), 3);
            collector.Accumulate(Sample(new HandInput(true, 1, true, null)), 10);
            var stats = collector.Snapshot().Left;
            Assert.That(stats.HasForce, Is.True);
            Assert.That(stats.MeanForceN, Is.EqualTo(2).Within(0.001));
            Assert.That(stats.MaxForceN, Is.EqualTo(8));
            Assert.That(stats.ForceValidDurationSeconds, Is.EqualTo(4));
            Assert.That(stats.ValidDurationSeconds, Is.EqualTo(14));
        }

        [Test]
        public void InvalidHandDoesNotRemoveOtherHandsValidTime()
        {
            var collector = new ForceStatsCollector();
            collector.Accumulate(new InputSnapshot(new HandInput(true, 1, false, 99),
                new HandInput(true, 0.5f, true, 4), "Sensor"), 2);
            var stats = collector.Snapshot();
            Assert.That(stats.Left.HasStrength, Is.False);
            Assert.That(stats.Left.HasForce, Is.False);
            Assert.That(stats.Left.PressedDurationSeconds, Is.Zero);
            Assert.That(stats.Right.MeanStrength01, Is.EqualTo(0.5f));
            Assert.That(stats.Right.ValidDurationSeconds, Is.EqualTo(2));
            Assert.That(stats.BothPressedDurationSeconds, Is.Zero);
            Assert.That(stats.HasBalance, Is.True);
            Assert.That(stats.LeftBalance01, Is.Zero);
        }

        [Test]
        public void BalanceUsesPressedDurationAndCountsValidBothPressedTime()
        {
            var collector = new ForceStatsCollector();
            var pressed = new HandInput(true, 1, true, null);
            collector.Accumulate(new InputSnapshot(pressed, pressed, "Keyboard"), 2);
            collector.Accumulate(Sample(pressed), 4);
            var stats = collector.Snapshot();
            Assert.That(stats.BothPressedDurationSeconds, Is.EqualTo(2));
            Assert.That(stats.HasBalance, Is.True);
            Assert.That(stats.LeftBalance01, Is.EqualTo(0.75f));
        }

        [Test]
        public void ReleasedHandsHaveStrengthButNoBalance()
        {
            var collector = new ForceStatsCollector();
            var released = new HandInput(false, 0, true, null);
            collector.Accumulate(new InputSnapshot(released, released, "Keyboard"), 1);
            var stats = collector.Snapshot();
            Assert.That(stats.Left.HasStrength, Is.True);
            Assert.That(stats.Left.MeanStrength01, Is.Zero);
            Assert.That(stats.HasBalance, Is.False);
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void InvalidDurationDoesNotContribute(float dt)
        {
            var collector = new ForceStatsCollector();
            collector.Accumulate(Sample(new HandInput(true, 1, true, 10)), dt);
            var stats = collector.Snapshot();
            Assert.That(stats.Left.HasStrength, Is.False);
            Assert.That(stats.Left.HasForce, Is.False);
            Assert.That(stats.Left.PressedDurationSeconds, Is.Zero);
            Assert.That(stats.HasBalance, Is.False);
        }

        [Test]
        public void ResetAndSnapshotCopiesProtectCollectedState()
        {
            var collector = new ForceStatsCollector();
            collector.Accumulate(Sample(new HandInput(true, 1, true, 10)), 2);
            var first = collector.Snapshot();
            first.Left.MeanStrength01 = 99;
            first.Left.PressedDurationSeconds = 99;
            first.Right.HasStrength = true;
            first.BothPressedDurationSeconds = 99;
            var second = collector.Snapshot();
            Assert.That(second.Left.MeanStrength01, Is.EqualTo(1));
            Assert.That(second.Left.PressedDurationSeconds, Is.EqualTo(2));
            Assert.That(second.Right.HasStrength, Is.False);
            Assert.That(second.BothPressedDurationSeconds, Is.Zero);
            collector.Reset();
            var empty = collector.Snapshot();
            Assert.That(empty.Left.HasStrength, Is.False);
            Assert.That(empty.Left.HasForce, Is.False);
            Assert.That(empty.Left.PressedDurationSeconds, Is.Zero);
            Assert.That(empty.HasBalance, Is.False);
            Assert.That(second.Left.MeanForceN, Is.EqualTo(10));
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void NonfiniteStrengthDoesNotDiscardValidForceOrPressedTime(float strength)
        {
            var collector = new ForceStatsCollector();
            collector.Accumulate(Sample(new HandInput(true, strength, true, 5)), 2);
            var stats = collector.Snapshot().Left;
            Assert.That(stats.HasStrength, Is.False);
            Assert.That(stats.ValidDurationSeconds, Is.Zero);
            Assert.That(stats.HasForce, Is.True);
            Assert.That(stats.MeanForceN, Is.EqualTo(5));
            Assert.That(stats.ForceValidDurationSeconds, Is.EqualTo(2));
            Assert.That(stats.PressedDurationSeconds, Is.EqualTo(2));
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        [TestCase(-1f)]
        public void InvalidForceDoesNotContaminateValidStrength(float force)
        {
            var collector = new ForceStatsCollector();
            collector.Accumulate(Sample(new HandInput(true, 0.5f, true, force)), 2);
            collector.Accumulate(Sample(new HandInput(false, 0, true, 2)), 1);
            var stats = collector.Snapshot().Left;
            Assert.That(stats.MeanStrength01, Is.EqualTo(1f / 3).Within(0.001));
            Assert.That(stats.MeanForceN, Is.EqualTo(2));
            Assert.That(stats.ForceValidDurationSeconds, Is.EqualTo(1));
            Assert.That(stats.MaxForceN, Is.EqualTo(2));
        }

        [Test]
        public void FiniteStrengthOutsideNormalizedRangeIsClamped()
        {
            var collector = new ForceStatsCollector();
            collector.Accumulate(Sample(new HandInput(true, 2, true, null)), 1);
            collector.Accumulate(Sample(new HandInput(false, -1, true, null)), 1);
            var stats = collector.Snapshot().Left;
            Assert.That(stats.MeanStrength01, Is.EqualTo(0.5f));
            Assert.That(stats.MaxStrength01, Is.EqualTo(1));
        }

        [Test]
        public void LargeFiniteValuesDoNotOverflowWeightedMeans()
        {
            var collector = new ForceStatsCollector();
            collector.Accumulate(Sample(new HandInput(true, 1, true, float.MaxValue)), 2);
            Assert.That(collector.Snapshot().Left.MeanForceN, Is.EqualTo(float.MaxValue));
        }

        private static InputSnapshot Sample(HandInput left) =>
            new InputSnapshot(left, default, "Test");
    }
}
