using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CPS.KitchenCatch.Tests
{
    public sealed class VersusSetupFlowTests
    {
        private readonly List<GameObject> objects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var item in objects)
                if (item != null) Object.DestroyImmediate(item);
            objects.Clear();
        }

        [Test]
        public void StartRequiresDistinctProfilesAndBothPlayersReady()
        {
            var session = CreateSession(out var store);
            var arc = store.Create("Arc");
            var tet = store.Create("Tet");
            var setup = CreateSetup(session);

            setup.SelectProfile(1, arc.PlayerId);
            setup.SelectProfile(2, arc.PlayerId);
            setup.SetReady(1, true);
            setup.SetReady(2, true);
            Assert.That(setup.CanStart, Is.False);

            setup.SelectProfile(2, tet.PlayerId);
            Assert.That(setup.CanStart, Is.True);
            Assert.That(setup.TryPrepareMatch(), Is.True);
            Assert.That(session.VersusSetup.Player1.PlayerId, Is.EqualTo(arc.PlayerId));
            Assert.That(session.VersusSetup.Player2.PlayerId, Is.EqualTo(tet.PlayerId));
        }

        [Test]
        public void CreatingProfileSelectsOnlyTheInitiatingSlot()
        {
            var session = CreateSession(out var store);
            var arc = store.Create("Arc");
            var tet = store.Create("Tet");
            var setup = CreateSetup(session);
            setup.SelectProfile(1, arc.PlayerId);
            setup.SelectProfile(2, tet.PlayerId);

            var created = setup.CreateProfile(2, "New Player");

            Assert.That(setup.Player1ProfileId, Is.EqualTo(arc.PlayerId));
            Assert.That(setup.Player2ProfileId, Is.EqualTo(created.PlayerId));
            Assert.That(store.Profiles.Count, Is.EqualTo(3));
        }

        [Test]
        public void PaletteChoicesPersistIntoPreparedSetup()
        {
            var session = CreateSession(out var store);
            var arc = store.Create("Arc");
            var tet = store.Create("Tet");
            var setup = CreateSetup(session);
            setup.SelectProfile(1, arc.PlayerId);
            setup.SelectProfile(2, tet.PlayerId);
            setup.SetColors(1, "#AABBCC", "#112233");
            setup.SetColors(2, "#DDEEFF", "#445566");
            setup.SetReady(1, true);
            setup.SetReady(2, true);

            Assert.That(setup.TryPrepareMatch(), Is.True);
            Assert.That(session.VersusSetup.Player1.HatColorHex, Is.EqualTo("#AABBCC"));
            Assert.That(session.VersusSetup.Player1.ShoeColorHex, Is.EqualTo("#AABBCC"));
            Assert.That(session.VersusSetup.Player2.HatColorHex, Is.EqualTo("#DDEEFF"));
            Assert.That(session.VersusSetup.Player2.ShoeColorHex, Is.EqualTo("#DDEEFF"));
        }

        [Test]
        public void PracticePlayersMoveIndependentlyAndLatchReadyOnBothDirections()
        {
            var first = CreatePractice("P1");
            var second = CreatePractice("P2");

            first.StepPractice(Snapshot(left: false, right: true), 0.5f);
            second.StepPractice(Snapshot(left: true, right: false), 0.25f);

            Assert.That(first.PositionX, Is.GreaterThan(0));
            Assert.That(second.PositionX, Is.LessThan(0));
            Assert.That(first.IsReady, Is.False);
            first.StepPractice(Snapshot(left: true, right: true), 0.1f);
            Assert.That(first.IsReady, Is.True);
            Assert.That(second.IsReady, Is.False);
        }

        [Test]
        public void PencilRenameKeepsProfileIdAndDoesNotCreateAnotherProfile()
        {
            var session = CreateSession(out var store);
            var profile = store.Create("Before");
            var setup = CreateSetup(session);
            var input = NewObject("Rename", typeof(RectTransform), typeof(InputField)).GetComponent<InputField>();
            typeof(VersusSetupUI).GetField("player1Name", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(setup, input);
            setup.EditPlayer1();
            Assert.That(input.text, Is.EqualTo("Before"));
            input.text = "After";
            setup.CreatePlayer1();
            Assert.That(store.Profiles.Count, Is.EqualTo(1));
            Assert.That(store.Profiles[0].PlayerId, Is.EqualTo(profile.PlayerId));
            Assert.That(store.Profiles[0].DisplayName, Is.EqualTo("After"));
        }

        [Test]
        public void GeneratedHatSpritesKeepWhiteTintAndSwapWithFacing()
        {
            var practice = CreatePractice("Hat preview");
            var body = NewObject("Body", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            var hat = NewObject("Hat", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            hat.rectTransform.anchoredPosition = new Vector2(-16.8f, 75.33f);
            var right = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * .5f);
            var left = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * .5f);
            try
            {
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                typeof(VersusPracticePlayer).GetField("hat", flags).SetValue(practice, hat);
                typeof(VersusPracticePlayer).GetField("hatVariants", flags).SetValue(practice,
                    new[] { new VersusPracticePlayer.HatVariant { Color = Color.blue, Right = right, Left = left } });
                practice.Initialize(null, body.rectTransform, body, -100, 100, 100);
                practice.SetColors(Color.blue, Color.red);
                Assert.That(hat.color, Is.EqualTo(Color.white));
                Assert.That(hat.sprite, Is.SameAs(right));
                practice.StepPractice(Snapshot(true, false), .1f);
                Assert.That(hat.sprite, Is.SameAs(left));
                Assert.That(hat.rectTransform.anchoredPosition.x, Is.EqualTo(16.8f));
                Assert.That(hat.color, Is.EqualTo(Color.white));
                practice.StepPractice(Snapshot(false, true), .1f);
                Assert.That(hat.sprite, Is.SameAs(right));
                Assert.That(hat.rectTransform.anchoredPosition.x, Is.EqualTo(-16.8f));
            }
            finally { Object.DestroyImmediate(right); Object.DestroyImmediate(left); }
        }

        private AppSession CreateSession(out ProfileStore store)
        {
            var values = new Dictionary<string, string>();
            store = new ProfileStore(key => values.TryGetValue(key, out var raw) ? raw : null,
                (key, raw) => values[key] = raw, () => { });
            var go = NewObject("Session");
            go.SetActive(false);
            var session = go.AddComponent<AppSession>();
            session.Initialize(store);
            return session;
        }

        private VersusSetupUI CreateSetup(AppSession session)
        {
            var setup = NewObject("Setup").AddComponent<VersusSetupUI>();
            setup.Initialize(session, null);
            return setup;
        }

        private VersusPracticePlayer CreatePractice(string name)
        {
            var root = NewObject(name, typeof(RectTransform));
            var mascot = NewObject(name + " Mascot", typeof(RectTransform), typeof(Image));
            mascot.transform.SetParent(root.transform, false);
            var practice = root.AddComponent<VersusPracticePlayer>();
            practice.Initialize(null, mascot.GetComponent<RectTransform>(), null, -100, 100, 100);
            return practice;
        }

        private GameObject NewObject(string name, params System.Type[] components)
        {
            var go = new GameObject(name, components);
            objects.Add(go);
            return go;
        }

        private static InputSnapshot Snapshot(bool left, bool right) => new InputSnapshot(
            new HandInput(left, left ? 1 : 0, true, null),
            new HandInput(right, right ? 1 : 0, true, null), "Test");
    }
}
