using NUnit.Framework;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CPS.KitchenCatch.Tests
{
    public sealed class KeyboardPlayerTests : InputTestFixture
    {
        private GameObject playerObject;
        private KeyboardInput keyboardInput;
        private PlayerController player;
        private Keyboard keyboard;

        [SetUp]
        public override void Setup()
        {
            base.Setup();
            keyboard = InputSystem.AddDevice<Keyboard>();
            playerObject = new GameObject("Player");
            keyboardInput = playerObject.AddComponent<KeyboardInput>();
            player = playerObject.AddComponent<PlayerController>();
        }

        [TearDown]
        public override void TearDown()
        {
            Object.DestroyImmediate(playerObject);
            base.TearDown();
        }

        [TestCase(true, false, -1)]
        [TestCase(false, true, 1)]
        [TestCase(true, true, 0)]
        [TestCase(false, false, 0)]
        public void KeyboardStatesMoveLeftRightBothOrNeither(bool left, bool right, int direction)
        {
            QueueKeys(left, right);
            var input = keyboardInput.Read();
            player.Step(input, 0.5f);
            Assert.That(player.MovementDirection, Is.EqualTo(direction));
            Assert.That(player.transform.position.x, Is.EqualTo(direction * 3.5f).Within(0.001));
            Assert.That(input.Source, Is.EqualTo("Keyboard"));
            Assert.That(input.Left.Pressed, Is.EqualTo(left));
            Assert.That(input.Right.Pressed, Is.EqualTo(right));
            Assert.That(input.Left.Strength01, Is.EqualTo(left ? 1 : 0));
            Assert.That(input.Right.Strength01, Is.EqualTo(right ? 1 : 0));
            Assert.That(input.Left.Valid, Is.True);
            Assert.That(input.Right.Valid, Is.True);
            Assert.That(input.Left.ForceN, Is.Null);
            Assert.That(input.Right.ForceN, Is.Null);
        }

        [TestCase(Key.A, Key.LeftArrow, -1)]
        [TestCase(Key.D, Key.RightArrow, 1)]
        public void ReleasingOneAliasKeepsOtherAliasPressed(Key letter, Key arrow, int direction)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(letter, arrow));
            InputSystem.Update();
            player.Step(keyboardInput.Read(), 0.1f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(arrow));
            InputSystem.Update();
            player.Step(keyboardInput.Read(), 0.1f);
            Assert.That(player.MovementDirection, Is.EqualTo(direction));
            Assert.That(player.transform.position.x, Is.EqualTo(direction * 1.4f).Within(0.001));
        }

        [Test]
        public void MissingKeyboardReturnsInvalidEmptyInput()
        {
            InputSystem.RemoveDevice(keyboard);
            var input = keyboardInput.Read();
            AssertInvalidEmpty(input);
            player.Step(input, 1);
            Assert.That(player.transform.position.x, Is.Zero);
        }

        [Test]
        public void FocusLossReturnsInvalidInputAndDoesNotAccumulateTime()
        {
            QueueKeys(true, false);
            playerObject.SendMessage("OnApplicationFocus", false, SendMessageOptions.DontRequireReceiver);
            var input = keyboardInput.Read();
            AssertInvalidEmpty(input);
            var collector = new ForceStatsCollector();
            collector.Accumulate(input, 3);
            Assert.That(collector.Snapshot().Left.HasStrength, Is.False);
            playerObject.SendMessage("OnApplicationFocus", true, SendMessageOptions.DontRequireReceiver);
            QueueKeys(false, true);
            Assert.That(keyboardInput.Read().Right.Pressed, Is.True);
        }

        [TestCase(30)]
        [TestCase(60)]
        [TestCase(120)]
        public void MovementDistanceIsIndependentOfStepRate(int steps)
        {
            QueueKeys(false, true);
            for (int i = 0; i < steps; i++)
                player.Step(keyboardInput.Read(), 1f / steps);
            Assert.That(player.transform.position.x, Is.EqualTo(7).Within(0.001));
        }

        [Test]
        public void MovementClampsVisibleWidthAndResetPreservesOtherAxes()
        {
            player.transform.position = new Vector3(0, -3, 2);
            QueueKeys(false, true);
            player.Step(keyboardInput.Read(), 10);
            Assert.That(player.transform.position.x, Is.EqualTo(7.5f));
            QueueKeys(true, false);
            player.Step(keyboardInput.Read(), 10);
            Assert.That(player.transform.position.x, Is.EqualTo(-7.5f));
            player.ResetPosition();
            Assert.That(player.transform.position, Is.EqualTo(new Vector3(0, -3, 2)));
            Assert.That(player.MovementDirection, Is.Zero);
        }

        [Test]
        public void CatchZoneTracksPositionWithoutFacingOrScaleChanges()
        {
            player.transform.position = new Vector3(2, -3, 0);
            var before = player.CatchZone;
            Assert.That(before.center, Is.EqualTo(new Vector3(2, -2.5f, 0)));
            Assert.That(before.size, Is.EqualTo(new Vector3(2f, 0.8f, 0.2f)));
            player.transform.localScale = new Vector3(-2, 3, 1);
            player.transform.rotation = Quaternion.Euler(0, 0, 20);
            Assert.That(player.CatchZone, Is.EqualTo(before));
            player.transform.position += Vector3.right;
            Assert.That(player.CatchZone.center.x, Is.EqualTo(3));
        }

        [TestCase(0.5f, 7.5f)] [TestCase(3f, 5f)] [TestCase(9f, 0f)] [TestCase(-1f, 8f)]
        public void ReachableCatcherCenterRangeMatchesConfiguredClampsAndOffset(float width, float limit)
        {
            typeof(PlayerController).GetField("mascotHalfWidth", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(player, width);
            typeof(PlayerController).GetField("catchZoneOffset", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(player, new Vector3(1, 0.5f, 0));
            Assert.That(player.ReachableCatchCenterRange, Is.EqualTo(new Vector2(1 - limit, 1 + limit)));
            QueueKeys(false, true);
            player.Step(keyboardInput.Read(), 10);
            Assert.That(player.CatchZone.center.x, Is.EqualTo(1 + limit));
            QueueKeys(true, false);
            player.Step(keyboardInput.Read(), 10);
            Assert.That(player.CatchZone.center.x, Is.EqualTo(1 - limit));
        }

        [Test]
        public void InvalidHandAndInvalidDurationDoNotMovePlayer()
        {
            var invalid = new HandInput(true, 1, false, null);
            player.Step(new InputSnapshot(invalid, default, "Test"), 1);
            QueueKeys(false, true);
            foreach (float dt in new[] { -1f, 0f, float.NaN, float.PositiveInfinity })
                player.Step(keyboardInput.Read(), dt);
            Assert.That(player.transform.position.x, Is.Zero);
        }

        private void QueueKeys(bool left, bool right)
        {
            var state = new KeyboardState();
            if (left) state.Set(Key.A, true);
            if (right) state.Set(Key.D, true);
            InputSystem.QueueStateEvent(keyboard, state);
            InputSystem.Update();
        }

        private static void AssertInvalidEmpty(InputSnapshot input)
        {
            Assert.That(input.Left.Valid, Is.False);
            Assert.That(input.Right.Valid, Is.False);
            Assert.That(input.Left.Pressed, Is.False);
            Assert.That(input.Right.Pressed, Is.False);
            Assert.That(input.Left.Strength01, Is.Zero);
            Assert.That(input.Right.Strength01, Is.Zero);
            Assert.That(input.Left.ForceN, Is.Null);
            Assert.That(input.Right.ForceN, Is.Null);
        }
    }
}
