using System.Globalization;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace CPS.KitchenCatch.Tests
{
    public sealed class ControllerInputTests
    {
        private GameObject root;
        private ControllerInput input;
        private SerialConnection connection;
        private ControllerSettings settings;
        private int generation;
        private uint sequence;
        private bool hadPreference;
        private int preference;
        private const string Preference = "CPS.ControllerMode.v1";
        [SetUp] public void SetUp()
        {
            hadPreference = PlayerPrefs.HasKey(Preference); preference = PlayerPrefs.GetInt(Preference);
            PlayerPrefs.DeleteKey(Preference);
            root = new GameObject("Controller input test"); connection = root.AddComponent<SerialConnection>();
            input = root.AddComponent<ControllerInput>(); settings = ScriptableObject.CreateInstance<ControllerSettings>();
            Set("serial", connection); Set("settings", settings);
            generation = connection.BeginSession(); connection.TransportOpened(generation);
        }
        [TearDown] public void TearDown()
        {
            Object.DestroyImmediate(root); Object.DestroyImmediate(settings);
            if (hadPreference) PlayerPrefs.SetInt(Preference, preference); else PlayerPrefs.DeleteKey(Preference);
        }
        private void Set(string name, object value) => typeof(ControllerInput).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(input, value);
        private void Publish(float level = 0, float force = 0, uint tare = 1, uint age = 20)
        {
            string line = File.ReadAllText(Path.Combine(Application.dataPath, "CPS/Tests/Fixtures/Usb/valid-v1.json")).Trim()
                .Replace("\"sequence\":12", "\"sequence\":" + ++sequence)
                .Replace("\"fsrLeftPercent\":50", "\"fsrLeftPercent\":" + level.ToString(CultureInfo.InvariantCulture))
                .Replace("\"loadLeftN\":0.980665", "\"loadLeftN\":" + force.ToString(CultureInfo.InvariantCulture))
                .Replace("\"loadRightN\":-0.01", "\"loadRightN\":0")
                .Replace("\"tareId\":1", "\"tareId\":" + tare)
                .Replace("\"loadLeftAgeMs\":20", "\"loadLeftAgeMs\":" + age);
            connection.Receive(line + "\n", generation, 1);
        }
        private void AuthorizeLoad()
        {
            Publish(); connection.SendCommand += _ => { };
            Assert.That(connection.RequestTare(), Is.True); Publish(tare: 2);
        }
        [Test] public void FsrHysteresisKeepsIntermediateStateWithoutInventingForce()
        {
            input.SelectMode(ControllerMode.FSR);
            float[] levels = { 19, 20, 17, 15 }; bool[] pressed = { false, true, true, false };
            for (int i = 0; i < levels.Length; i++)
            {
                Publish(levels[i]); var frame = input.ReadAt(1.1);
                Assert.That(frame.Source, Is.EqualTo("FSR"));
                Assert.That(frame.Left.Valid, Is.True); Assert.That(frame.Left.Pressed, Is.EqualTo(pressed[i]));
                Assert.That(frame.Left.Strength01, Is.EqualTo(levels[i] / 100).Within(.001));
                Assert.That(frame.Left.ForceN, Is.Null);
            }
        }
        [Test] public void LoadHysteresisUsesNewtonsAndNoPercentage()
        {
            input.SelectMode(ControllerMode.LoadCell); AuthorizeLoad();
            float[] forces = { .49f, .5f, .4f, .3f }; bool[] pressed = { false, true, true, false };
            for (int i = 0; i < forces.Length; i++)
            {
                Publish(force: forces[i], tare: 2); var frame = input.ReadAt(1.1);
                Assert.That(frame.Left.Pressed, Is.EqualTo(pressed[i]));
                Assert.That(frame.Left.ForceN, Is.EqualTo(forces[i]));
                Assert.That(frame.Left.HasStrength, Is.False);
            }
            Publish(force: -2, tare: 2); Assert.That(input.ReadAt(1.1).Left.ForceN, Is.Zero);
        }
        [Test] public void HeartbeatDoesNotRefreshAnOldLoadSampleOrHeldPress()
        {
            input.SelectMode(ControllerMode.LoadCell); AuthorizeLoad();
            Publish(force: 2, tare: 2); Assert.That(input.ReadAt(1.1).Left.Pressed, Is.True);
            Publish(force: 2, tare: 2, age: 700);
            Assert.That(input.CanPlayAt(1.1, out _), Is.False);
            Assert.That(input.ReadAt(1.1).Left.Valid, Is.False);
            Assert.That(input.ReadAt(1.1).Left.Pressed, Is.False);
        }
        [Test] public void FsrDoesNotNeedLoadTareAndOldPacketsBecomeUnavailable()
        {
            input.SelectMode(ControllerMode.FSR); Publish(50);
            Assert.That(input.CanPlayAt(1.1, out _), Is.True);
            Assert.That(input.ReadAt(1.75).Left.Valid, Is.False);
            connection.Disconnect(); Assert.That(input.ReadAt(2).Left.Pressed, Is.False);
        }
        [Test] public void ModeAndThresholdsStayFrozenForTheRound()
        {
            input.SelectMode(ControllerMode.FSR); Publish(25); input.FreezeForRound();
            settings.fsrLeftPress = 90; input.SelectMode(ControllerMode.LoadCell);
            Assert.That(input.SelectedMode, Is.EqualTo(ControllerMode.FSR));
            Assert.That(input.ReadAt(1.1).Left.Pressed, Is.True);
            input.ReleaseRound(); input.SelectMode(ControllerMode.LoadCell);
            Assert.That(input.SelectedMode, Is.EqualTo(ControllerMode.LoadCell));
        }
        [Test] public void ForceStatisticsDoNotFabricateStrengthButKeepReleaseAndPressedTime()
        {
            var stats = new ForceStatsCollector();
            stats.Accumulate(new InputSnapshot(new HandInput(true, 0, true, 2, false), default, "LoadCell"), 1);
            stats.Accumulate(new InputSnapshot(new HandInput(false, 0, true, 0, false), default, "LoadCell"), 3);
            var left = stats.Snapshot().Left;
            Assert.That(left.HasStrength, Is.False); Assert.That(left.HasForce, Is.True);
            Assert.That(left.MeanForceN, Is.EqualTo(.5)); Assert.That(left.PressedDurationSeconds, Is.EqualTo(1));
        }
        [Test] public void InvalidThresholdsCannotStartSensorPlay()
        {
            settings.fsrLeftRelease = 30; input.SelectMode(ControllerMode.FSR); Publish(50);
            Assert.That(input.CanPlayAt(1.1, out var reason), Is.False);
            Assert.That(reason, Does.Contain("threshold"));
        }
    }
}
