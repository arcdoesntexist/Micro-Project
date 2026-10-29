using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

namespace CPS.KitchenCatch.Tests
{
    public sealed class VersusKeyboardInputTests : InputTestFixture
    {
        private Keyboard keyboard;
        private GameObject p1Object;
        private GameObject p2Object;
        private VersusKeyboardInput p1;
        private VersusKeyboardInput p2;

        [SetUp]
        public override void Setup()
        {
            base.Setup();
            keyboard = InputSystem.AddDevice<Keyboard>();
            p1Object = new GameObject("P1 Input");
            p2Object = new GameObject("P2 Input");
            p1 = p1Object.AddComponent<VersusKeyboardInput>();
            p2 = p2Object.AddComponent<VersusKeyboardInput>();
            p1.Configure(Key.A, Key.D, "Keyboard P1");
            p2.Configure(Key.LeftArrow, Key.RightArrow, "Keyboard P2");
        }

        [TearDown]
        public override void TearDown()
        {
            Object.DestroyImmediate(p1Object);
            Object.DestroyImmediate(p2Object);
            base.TearDown();
        }

        [Test]
        public void LetterAndArrowKeysStayOnTheirConfiguredPlayer()
        {
            Queue(Key.A, Key.RightArrow);

            var first = p1.Read();
            var second = p2.Read();

            Assert.That(first.Left.Pressed, Is.True);
            Assert.That(first.Right.Pressed, Is.False);
            Assert.That(first.Source, Is.EqualTo("Keyboard P1"));
            Assert.That(second.Left.Pressed, Is.False);
            Assert.That(second.Right.Pressed, Is.True);
            Assert.That(second.Source, Is.EqualTo("Keyboard P2"));
        }

        [Test]
        public void BothPlayersAndOpposingDirectionsCanBePressedTogether()
        {
            Queue(Key.A, Key.D, Key.LeftArrow, Key.RightArrow);

            var first = p1.Read();
            var second = p2.Read();

            Assert.That(first.Left.Pressed, Is.True);
            Assert.That(first.Right.Pressed, Is.True);
            Assert.That(second.Left.Pressed, Is.True);
            Assert.That(second.Right.Pressed, Is.True);
            Assert.That(first.Left.Strength01, Is.EqualTo(1));
            Assert.That(second.Right.Strength01, Is.EqualTo(1));
        }

        [Test]
        public void MissingKeyboardAndFocusLossReturnInvalidReleasedHands()
        {
            Queue(Key.A, Key.RightArrow);
            p1Object.SendMessage("OnApplicationFocus", false, SendMessageOptions.DontRequireReceiver);

            AssertInvalid(p1.Read());
            Assert.That(p2.Read().Right.Pressed, Is.True);

            InputSystem.RemoveDevice(keyboard);
            AssertInvalid(p2.Read());
        }

        [Test]
        public void InvalidConfigurationDoesNotReplaceWorkingKeys()
        {
            Assert.Throws<ArgumentException>(() => p1.Configure(Key.None, Key.D, "Keyboard P1"));
            Assert.Throws<ArgumentException>(() => p1.Configure(Key.A, Key.A, "Keyboard P1"));
            Assert.Throws<ArgumentException>(() => p1.Configure(Key.A, Key.D, " "));

            Queue(Key.D);
            Assert.That(p1.Read().Right.Pressed, Is.True);
        }

        private void Queue(params Key[] keys)
        {
            var state = new KeyboardState(keys);
            InputSystem.QueueStateEvent(keyboard, state);
            InputSystem.Update();
        }

        private static void AssertInvalid(InputSnapshot snapshot)
        {
            Assert.That(snapshot.Left.Valid, Is.False);
            Assert.That(snapshot.Right.Valid, Is.False);
            Assert.That(snapshot.Left.Pressed, Is.False);
            Assert.That(snapshot.Right.Pressed, Is.False);
            Assert.That(snapshot.Left.Strength01, Is.Zero);
            Assert.That(snapshot.Right.Strength01, Is.Zero);
        }
    }
}
