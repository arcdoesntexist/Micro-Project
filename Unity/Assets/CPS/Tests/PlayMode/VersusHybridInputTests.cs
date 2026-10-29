using NUnit.Framework;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CPS.KitchenCatch.Tests
{
    public sealed class VersusHybridInputTests : InputTestFixture
    {
        private Keyboard keyboard;
        private GameObject root;
        private VersusControllerHub hub;
        private VersusKeyboardInput p1;
        private VersusKeyboardInput p2;
        [SetUp] public override void Setup()
        {
            base.Setup();
            if (VersusControllerHub.Instance != null) Object.DestroyImmediate(VersusControllerHub.Instance.gameObject);
            keyboard = InputSystem.AddDevice<Keyboard>();
            hub = VersusControllerHub.EnsureExists();
            root = new GameObject("Hybrid inputs");
            p1 = root.AddComponent<VersusKeyboardInput>(); p1.Configure(Key.A, Key.D, "Keyboard P1");
            p2 = root.AddComponent<VersusKeyboardInput>(); p2.Configure(Key.LeftArrow, Key.RightArrow, "Keyboard P2");
        }
        [TearDown] public override void TearDown()
        {
            Object.DestroyImmediate(root);
            if (hub != null) Object.DestroyImmediate(hub.gameObject);
            base.TearDown();
        }
        private void Feed(string line = "FSR raw L=0 R=100\n")
        {
            if (hub.Port(2) == null) Assert.That(hub.BeginSession(2, "TEST-COM2"), Is.True);
            int generation = hub.Generation(2);
            hub.TransportEvent(2, generation, "connected", null, SerialConnection.NowSeconds);
            hub.TransportEvent(2, generation, "chunk", line, SerialConnection.NowSeconds);
        }
        private void Keys(params Key[] keys) { InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys)); InputSystem.Update(); }

        [Test] public void KeyboardAndFsrWorkTogetherWithoutCrossingPlayersOrFakingForce()
        {
            Feed(); Keys(Key.A, Key.LeftArrow);
            Assert.That(p1.Read().Left.Pressed, Is.True);
            Assert.That(p1.Read().Right.Pressed, Is.False);
            var state = p2.Read();
            Assert.That(state.Left.Pressed && state.Right.Pressed, Is.True);
            Assert.That(state.Left.HasStrength, Is.False);
            Assert.That(state.Right.Strength01, Is.EqualTo(1));
            Assert.That(state.Right.ForceN, Is.Null);
            Assert.That(state.Source, Is.EqualTo("Keyboard + FSR P2"));
        }
        [Test] public void BrokenSensorCannotPressLeftAndKeyboardStillWorksWithoutBoards()
        {
            Feed("FSR raw L=0 R=4095\n"); Keys();
            Assert.That(p2.Read().Left.Pressed, Is.False);
            Assert.That(p2.Read().Right.Pressed, Is.False);
            hub.Disconnect(2); Keys(Key.LeftArrow, Key.RightArrow);
            Assert.That(p2.Read().Left.Pressed && p2.Read().Right.Pressed, Is.True);
            Assert.That(p2.Read().Right.HasStrength, Is.False);
            Assert.That(p2.Read().Source, Is.EqualTo("Keyboard P2"));
        }
        [Test] public void DuplicatePortsAndRetiredGenerationCannotReplaceTheNewSession()
        {
            Feed(); int old = hub.Generation(2);
            Assert.That(hub.BeginSession(1, "test-com2"), Is.False);
            Assert.That(hub.BeginSession(2, "TEST-COM3"), Is.True);
            hub.TransportEvent(2, old, "connected", null, 1);
            hub.TransportEvent(2, old, "chunk", "FSR raw L=0 R=100\n", 1);
            Assert.That(hub.IsFresh(2, 1), Is.False);
            Assert.That(hub.Port(2), Is.EqualTo("TEST-COM3"));
            Assert.That(hub.CanPlay(1, out _), Is.False);
        }
        [Test] public void LossReleasesSensorAndKeyboardContinuationDisarmsTheLoss()
        {
            Feed(); hub.WatchMatch();
            Assert.That(hub.HasStreamLoss(SerialConnection.NowSeconds), Is.False);
            Assert.That(hub.HasStreamLoss(SerialConnection.NowSeconds + 1), Is.True);
            Assert.That(hub.ReadAt(2, SerialConnection.NowSeconds + 1).Right.Pressed, Is.False);
            hub.ContinueWithKeyboard();
            Assert.That(hub.HasStreamLoss(SerialConnection.NowSeconds + 2), Is.False);
            Assert.That(hub.CanPlay(SerialConnection.NowSeconds, out _), Is.True);
            Keys(Key.RightArrow); Assert.That(p2.Read().Right.Pressed, Is.True);
        }
        [Test] public void OverflowFromRetiredConnectionCannotFaultReplacementConnection()
        {
            Feed();
            SetOverflow(2);
            Assert.That(hub.BeginSession(2, "TEST-COM3"), Is.True);
            Feed();
            hub.SendMessage("Update");
            Assert.That(hub.IsFresh(2, SerialConnection.NowSeconds), Is.True,
                "Overflow from the retired session must not clear the replacement connection.");
        }
        [Test] public void CurrentOverflowDoesNotInvalidateOtherPlayersHealthyStream()
        {
            Feed();
            hub.BeginSession(1, "TEST-COM1");
            hub.TransportEvent(1, hub.Generation(1), "connected", null, SerialConnection.NowSeconds);
            string json = System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath, "CPS/Tests/Fixtures/Usb/valid-v1.json"));
            hub.TransportEvent(1, hub.Generation(1), "chunk", json.Trim() + "\n", SerialConnection.NowSeconds);
            SetOverflow(2);
            hub.SendMessage("Update");
            Assert.That(hub.IsFresh(1, SerialConnection.NowSeconds), Is.True);
            Assert.That(hub.IsFresh(2, SerialConnection.NowSeconds), Is.False);
        }
        private void SetOverflow(int player)
        {
            var slots = (System.Array)typeof(VersusControllerHub).GetField("slots", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(hub);
            object slot = slots.GetValue(player - 1);
            object inbox = slot.GetType().GetField("Inbox").GetValue(slot);
            inbox.GetType().GetField("Overflow").SetValue(inbox, 1);
        }
        [Test] public void FocusLossAndControllerOverlaySuppressBothSources()
        {
            Feed(); Keys(Key.LeftArrow);
            hub.SuppressInput = true;
            Assert.That(p2.Read().Left.Pressed || p2.Read().Right.Pressed, Is.False);
            hub.SuppressInput = false;
            root.SendMessage("OnApplicationFocus", false);
            Assert.That(p2.Read().Left.Valid || p2.Read().Right.Valid, Is.False);
        }
        [Test] public void TypingProfileNameDoesNotDriveOrReadyMascots()
        {
            Feed(); Keys(Key.LeftArrow, Key.A, Key.D);
            var events = new GameObject("Typing events", typeof(EventSystem));
            var field = new GameObject("Nickname", typeof(RectTransform), typeof(InputField));
            try
            {
                EventSystem.current.SetSelectedGameObject(field);
                Assert.That(p1.Read().Left.Pressed || p1.Read().Right.Pressed, Is.False);
                Assert.That(p2.Read().Left.Pressed || p2.Read().Right.Pressed, Is.False);
            }
            finally { Object.DestroyImmediate(field); Object.DestroyImmediate(events); }
        }
    }
}
