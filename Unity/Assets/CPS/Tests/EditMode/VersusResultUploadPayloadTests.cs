using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace CPS.KitchenCatch.Tests
{
    public sealed class VersusResultUploadPayloadTests
    {
        private GameObject root;
        private AppSession app;

        [SetUp]
        public void Setup()
        {
            root = new GameObject("Versus payload test");
            root.SetActive(false);
            app = root.AddComponent<AppSession>();
            app.Initialize(new ProfileStore(_ => null, (_, _) => { }, () => { }));
            var first = app.Store.Create("Same name");
            var second = app.Store.Create("Same name");
            app.ConfigureVersus(first.PlayerId, second.PlayerId, "#F57C32", "#68A832", "#3C78D8", "#D94A64");
        }

        [TearDown]
        public void Cleanup() => Object.DestroyImmediate(root);

        [Test]
        public void CompletedMatchKeepsBothStableProfileIdsAndP1P2Order()
        {
            var data = Payload().Data;
            Assert.That((string)data["mode"], Is.EqualTo("multiplayer"));
            Assert.That((int)data["schemaVersion"], Is.EqualTo(1));
            Assert.That((string)data["winnerSlot"], Is.EqualTo("P1"));
            Assert.That((string)data["endReason"], Is.EqualTo("PlayerDefeated"));
            var players = (JArray)data["players"];
            Assert.That(players.Count, Is.EqualTo(2));
            Assert.That((string)players[0]["playerSlot"], Is.EqualTo("P1"));
            Assert.That((string)players[1]["playerSlot"], Is.EqualTo("P2"));
            Assert.That((string)players[0]["playerId"], Is.EqualTo(app.VersusSetup.Player1.PlayerId));
            Assert.That((string)players[1]["playerId"], Is.EqualTo(app.VersusSetup.Player2.PlayerId));
            Assert.That((int)players[0]["remainingHp"], Is.EqualTo(3));
            Assert.That((int)players[1]["remainingHp"], Is.Zero);
        }

        [Test]
        public void MissingInputMeasurementsAreNullInsteadOfInventedNewtons()
        {
            var player = Payload().Data["players"][0];
            Assert.That(player["leftMaxForceN"].Type, Is.EqualTo(JTokenType.Null));
            Assert.That(player["rightMeanForceN"].Type, Is.EqualTo(JTokenType.Null));
            Assert.That(player["leftMeanStrength01"].Type, Is.EqualTo(JTokenType.Null));
            Assert.That((string)player["inputSource"], Is.EqualTo("Keyboard P1"));
        }

        [Test]
        public void PayloadRetainsChosenColorsAndCaughtItemBreakdown()
        {
            var player = Payload().Data["players"][0];
            Assert.That((string)player["hatColorHex"], Is.EqualTo("#F57C32"));
            Assert.That((string)player["shoeColorHex"], Is.EqualTo("#68A832"));
            Assert.That((int)player["caughtItems"][0], Is.EqualTo(1));
            Assert.That((int)player["foodCaught"], Is.EqualTo(1));
            Assert.That((int)player["score"], Is.EqualTo(10));
        }

        [Test]
        public void BothPlayersDefeatedInOneStepProducesADrawPayload()
        {
            var match = new VersusMatchModel(new VersusChallenge());
            match.Start();
            var trash = new[] {
                new VersusObjectResolution(1, ObjectKind.Trash, ObjectOutcome.Caught, 0, 3),
                new VersusObjectResolution(2, ObjectKind.Trash, ObjectOutcome.Caught, 0, 3),
                new VersusObjectResolution(3, ObjectKind.Trash, ObjectOutcome.Caught, 0, 4) };
            match.ApplyStep(1, trash, trash);
            var data = VersusResultUploadPayload.Create("draw", app.VersusSetup, match.Result, null, null).Data;
            Assert.That((string)data["winnerSlot"], Is.EqualTo("Draw"));
            Assert.That((int)data["players"][0]["remainingHp"], Is.Zero);
            Assert.That((int)data["players"][1]["remainingHp"], Is.Zero);
        }

        private VersusResultUploadPayload Payload()
        {
            var match = new VersusMatchModel(new VersusChallenge());
            match.Start();
            match.ApplyStep(83.25f, new[] { new VersusObjectResolution(1, ObjectKind.Food, ObjectOutcome.Caught, 10, 0) },
                new[] {
                    new VersusObjectResolution(1, ObjectKind.Trash, ObjectOutcome.Caught, 0, 3),
                    new VersusObjectResolution(2, ObjectKind.Trash, ObjectOutcome.Caught, 0, 3),
                    new VersusObjectResolution(3, ObjectKind.Trash, ObjectOutcome.Caught, 0, 4) });
            return VersusResultUploadPayload.Create("unit-match", app.VersusSetup, match.Result, null, null);
        }
    }
}
