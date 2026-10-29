using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace CPS.KitchenCatch.Tests
{
    public sealed class SensorPacketTests
    {
        private static string Fixture(string name = "valid-v1") =>
            File.ReadAllText(Path.Combine(Application.dataPath, "CPS/Tests/Fixtures/Usb/" + name + ".json")).Trim();

        [Test] public void ValidForceAndRelativeLevelsKeepTheirUnitsAndSigns()
        {
            var reader = new SensorPacketReader();
            Assert.That(reader.TryAccept(Fixture(), 2, out var error), Is.True, error);
            Assert.That(reader.Latest.FsrLeftPercent, Is.EqualTo(50));
            Assert.That(reader.Latest.LoadLeftN, Is.EqualTo(.980665f).Within(.00001));
            Assert.That(reader.Latest.LoadRightN, Is.EqualTo(-.01f));
            Assert.That(reader.Latest.CalibrationVersion, Is.EqualTo("cps-2026-10-26-v1"));
        }
        [Test] public void UncalibratedHandsRemainUnavailableNotZeroForce()
        {
            var reader = new SensorPacketReader();
            Assert.That(reader.TryAccept(Fixture("released-v1"), 0, out _), Is.True);
            Assert.That(reader.Latest.LoadLeftN, Is.Null);
            Assert.That(reader.Latest.LoadRightAgeMs, Is.Null);
            Assert.That(reader.Latest.LoadLeftTared, Is.False);
        }
        [Test] public void FiniteJsonIntegerForceWithinFloatRangeDoesNotThrow()
        {
            var reader = new SensorPacketReader();
            Assert.That(reader.TryAccept(Fixture().Replace("\"loadLeftN\":0.980665", "\"loadLeftN\":100000000000000000000"), 1, out _), Is.True);
            Assert.That(reader.Latest.LoadLeftN, Is.EqualTo(1e20f));
        }
        [TestCase("\"version\":1", "\"version\":2")]
        [TestCase("\"bootId\":17", "\"bootId\":-1")]
        [TestCase("\"bootId\":17", "\"bootId\":999999999999999999999999999999999")]
        [TestCase("\"sequence\":12", "\"sequence\":4294967296")]
        [TestCase("\"tareId\":1", "\"tareId\":1.5")]
        [TestCase("\"fsrLeftPercent\":50", "\"fsrLeftPercent\":101")]
        [TestCase("\"fsrLeftPercent\":50", "\"fsrLeftPercent\":-1")]
        [TestCase("\"fsrLeftPercent\":50", "\"fsrLeftPercent\":NaN")]
        [TestCase("\"loadLeftN\":0.980665", "\"loadLeftN\":Infinity")]
        [TestCase("\"loadLeftN\":0.980665", "\"loadLeftN\":null")]
        [TestCase("\"loadLeftTared\":true", "\"loadLeftTared\":false")]
        [TestCase("\"loadLeftValid\":true", "\"loadLeftValid\":\"true\"")]
        [TestCase("\"loadLeftAgeMs\":20", "\"loadLeftAgeMs\":null")]
        [TestCase("cps-2026-10-26-v1", "unknown")]
        [TestCase("\"fsrLeftPercent\":50,", "")]
        [TestCase("\"version\":1", "\"version\":1,\"version\":1")]
        public void InvalidPacketDoesNotReplaceGoodState(string before, string after)
        {
            var reader = new SensorPacketReader();
            Assert.That(reader.TryAccept(Fixture(), 1, out _), Is.True);
            Assert.That(reader.TryAccept(Fixture().Replace(before, after), 3, out _), Is.False);
            Assert.That(reader.ReceivedAtSeconds, Is.EqualTo(1));
        }
        [Test] public void DiagnosticOversizeAndTruncatedLinesDoNotBecomeTelemetry()
        {
            var reader = new SensorPacketReader();
            foreach (var line in new[] { "Starting ESP32", Fixture().Substring(0, 100), new string(' ', 1025) + Fixture(), Fixture() + "{}" })
                Assert.That(reader.TryAccept(line, 1, out _), Is.False);
            Assert.That(reader.Latest, Is.Null);
        }
        [Test] public void DuplicateAndOldSequenceCannotRefreshTheWatchdog()
        {
            var reader = new SensorPacketReader();
            Assert.That(reader.TryAccept(Fixture(), 1, out _), Is.True);
            Assert.That(reader.TryAccept(Fixture(), 2, out _), Is.False);
            Assert.That(reader.TryAccept(Fixture().Replace("\"sequence\":12", "\"sequence\":11"), 2, out _), Is.False);
            Assert.That(reader.ReceivedAtSeconds, Is.EqualTo(1));
        }
        [Test] public void UintSequenceAndUptimeRolloverAreAccepted()
        {
            var reader = new SensorPacketReader();
            string first = Fixture().Replace("\"sequence\":12", "\"sequence\":4294967295").Replace("\"uptimeMs\":700", "\"uptimeMs\":4294967290");
            Assert.That(reader.TryAccept(first, 1, out _), Is.True);
            Assert.That(reader.TryAccept(Fixture().Replace("\"sequence\":12", "\"sequence\":0").Replace("\"uptimeMs\":700", "\"uptimeMs\":10"), 2, out _), Is.True);
        }
        [Test] public void NewBootRetiresOldBootUntilExplicitPortReset()
        {
            var reader = new SensorPacketReader();
            Assert.That(reader.TryAccept(Fixture(), 1, out _), Is.True);
            Assert.That(reader.TryAccept(Fixture("released-v1").Replace("\"bootId\":17", "\"bootId\":18"), 2, out _), Is.True);
            Assert.That(reader.Latest.LoadLeftN, Is.Null);
            Assert.That(reader.TryAccept(Fixture(), 3, out _), Is.False);
            reader.Reset();
            Assert.That(reader.TryAccept(Fixture(), 3, out _), Is.True);
        }
        [Test] public void OneUnavailableHandDoesNotInventOrDiscardTheOtherForce()
        {
            string line = Fixture().Replace("\"loadLeftN\":0.980665", "\"loadLeftN\":null")
                .Replace("\"loadLeftAgeMs\":20", "\"loadLeftAgeMs\":null")
                .Replace("\"loadLeftValid\":true", "\"loadLeftValid\":false");
            var reader = new SensorPacketReader();
            Assert.That(reader.TryAccept(line, 1, out _), Is.True);
            Assert.That(reader.Latest.LoadLeftN, Is.Null);
            Assert.That(reader.Latest.LoadRightN, Is.EqualTo(-.01f));
        }
        [Test] public void FramingHandlesSplitCrLfAndDiscardsOversizeUntilNextNewline()
        {
            var buffer = new SerialLineBuffer(); var lines = new List<string>();
            buffer.Push("ab", lines.Add); buffer.Push("c\r\nsecond\n", lines.Add);
            buffer.Push(new string('x', 1025), lines.Add); buffer.Push("tail\nthird\n", lines.Add);
            Assert.That(lines, Is.EqualTo(new[] { "abc", "second", "third" }));
            buffer.Push("old", lines.Add); buffer.Reset(); buffer.Push("new\n", lines.Add);
            Assert.That(lines[3], Is.EqualTo("new"));
        }
    }
}
