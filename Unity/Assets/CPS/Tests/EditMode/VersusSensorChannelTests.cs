using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace CPS.KitchenCatch.Tests
{
    public sealed class VersusSensorChannelTests
    {
        private VersusControllerSettings settings;
        [SetUp] public void Setup() => settings = ScriptableObject.CreateInstance<VersusControllerSettings>();
        [TearDown] public void Cleanup() => UnityEngine.Object.DestroyImmediate(settings);

        [Test] public void BrokenLeftIsIgnoredAndRightAcceptsSplitLines()
        {
            var channel = new VersusSensorChannel(2, settings);
            channel.Receive("FSR raw L=0 R=", 1);
            Assert.That(channel.IsFresh(1), Is.False);
            channel.Receive("100\r\n", 1.1);
            var state = channel.Read(1.1);
            Assert.That(state.Left.Valid, Is.False);
            Assert.That(state.Left.Pressed, Is.False);
            Assert.That(state.Right.Pressed, Is.True);
            Assert.That(state.Right.Strength01, Is.EqualTo(1));
            Assert.That(state.Right.ForceN, Is.Null);
        }

        [TestCase("FSR raw L=4096 R=0\n")]
        [TestCase("FSR raw L=0 R=-1\n")]
        [TestCase("FSR raw L=0 R=100 trailing\n")]
        [TestCase("FSRFSR raw L=0 R=100\n")]
        [TestCase("FSR ADC_ERROR\n")]
        public void MalformedLinesCannotRefreshOrLatchInput(string line)
        {
            var channel = new VersusSensorChannel(2, settings);
            channel.Receive("FSR raw L=0 R=100\n", 1);
            channel.Receive(line, 1.7);
            Assert.That(channel.IsFresh(1.75), Is.False);
            Assert.That(channel.Read(1.75).Right.Pressed, Is.False);
        }

        [Test] public void HysteresisReleasesAndDoesNotRelatchInMiddleBandAfterStale()
        {
            var channel = new VersusSensorChannel(2, settings);
            channel.Receive("FSR raw L=0 R=3200\n", 1);
            Assert.That(channel.Read(1).Right.Pressed, Is.True);
            channel.Receive("FSR raw L=0 R=3400\n", 1.1);
            Assert.That(channel.Read(1.1).Right.Pressed, Is.True);
            channel.Receive("FSR raw L=0 R=3600\n", 1.2);
            Assert.That(channel.Read(1.2).Right.Pressed, Is.False);
            channel.Receive("FSR raw L=0 R=3200\n", 1.3);
            channel.Read(2.1);
            channel.Receive("FSR raw L=0 R=3400\n", 2.2);
            Assert.That(channel.Read(2.2).Right.Pressed, Is.False);
        }

        [Test] public void EspJsonUsesExistingValidationAndDuplicateCannotExtendFreshness()
        {
            var line = File.ReadAllText(Path.Combine(Application.dataPath, "CPS/Tests/Fixtures/Usb/valid-v1.json")).Trim();
            var channel = new VersusSensorChannel(1, settings);
            channel.Receive(line + "\n", 1);
            Assert.That(channel.Read(1).Left.Pressed, Is.True);
            Assert.That(channel.Read(1).Left.Strength01, Is.EqualTo(.5f));
            Assert.That(channel.Read(1).Left.ForceN, Is.Null);
            channel.Receive(line + "\n", 1.7);
            Assert.That(channel.IsFresh(1.75), Is.False);
        }

        [Test] public void InvalidSettingsAndTimestampsCannotBecomeFresh()
        {
            var channel = new VersusSensorChannel(2, settings);
            settings.stm32ReferenceRaw = settings.stm32ReleasedRaw;
            Assert.That(settings.ValidationError, Is.Not.Null);
            channel.Receive("FSR raw L=0 R=100\n", 1);
            Assert.That(channel.IsFresh(1), Is.False);
            settings.stm32ReferenceRaw = 100;
            channel.Receive("FSR raw L=0 R=100\n", double.NaN);
            Assert.That(channel.IsFresh(1), Is.False);
            settings.pressPercent = settings.releasePercent;
            Assert.That(settings.ValidationError, Is.Not.Null);
            Assert.Throws<ArgumentOutOfRangeException>(() => new VersusSensorChannel(3, settings));
        }
    }
}
