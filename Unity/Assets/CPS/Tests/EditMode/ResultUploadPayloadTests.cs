using System;
using NUnit.Framework;

namespace CPS.KitchenCatch.Tests
{
    public sealed class ResultUploadPayloadTests
    {
        [Test]
        public void From_MapsCompleteSessionResult()
        {
            var result = new SessionResult
            {
                SchemaVersion = 1,
                SessionId = "session-1",
                PlayerId = "player-1",
                DisplayName = "ผู้เล่นหนึ่ง",
                Difficulty = DifficultyId.Hard,
                EndReason = EndReason.Triumph,
                EndedAtUtc = "2026-10-20T01:00:00Z",
                PlayedSeconds = 60f,
                Score = 1234,
                RemainingHp = 80,
                FoodCaught = 12,
                FoodMissed = 3,
                TrashEaten = 1,
                MaxCombo = 7,
                HighestRank = ComboRank.S,
                InputSource = "FSR",
                InputStatistics = new InputStatistics
                {
                    Left = Hand(true, true, 0.9f, 0.4f, 15f, 8f, 20f),
                    Right = Hand(true, true, 0.8f, 0.3f, 14f, 7f, 18f),
                    BothPressedDurationSeconds = 5f,
                    HasBalance = true,
                    LeftBalance01 = 0.55f
                }
            };

            ResultUploadPayload payload = ResultUploadPayload.From(result);

            Assert.That(payload.schemaVersion, Is.EqualTo(1));
                Assert.That(payload.sessionId, Is.EqualTo("session-1"));
                Assert.That(payload.playerId, Is.EqualTo("player-1"));
                Assert.That(payload.displayName, Is.EqualTo("ผู้เล่นหนึ่ง"));
                Assert.That(payload.difficulty, Is.EqualTo("Hard"));
                Assert.That(payload.endReason, Is.EqualTo("Triumph"));
                Assert.That(payload.endedAtUtc, Is.EqualTo("2026-10-20T01:00:00Z"));
                Assert.That(payload.playedSeconds, Is.EqualTo(60f));
                Assert.That(payload.score, Is.EqualTo(1234));
                Assert.That(payload.remainingHp, Is.EqualTo(80));
                Assert.That(payload.foodCaught, Is.EqualTo(12));
                Assert.That(payload.foodMissed, Is.EqualTo(3));
                Assert.That(payload.trashEaten, Is.EqualTo(1));
                Assert.That(payload.maxCombo, Is.EqualTo(7));
                Assert.That(payload.highestRank, Is.EqualTo("S"));
                Assert.That(payload.inputSource, Is.EqualTo("FSR"));
                Assert.That(payload.leftMaxStrength01, Is.EqualTo(0.9f));
                Assert.That(payload.leftMeanStrength01, Is.EqualTo(0.4f));
                Assert.That(payload.leftPressedSeconds, Is.EqualTo(20f));
                Assert.That(payload.leftMaxForceN, Is.EqualTo(15f));
                Assert.That(payload.leftMeanForceN, Is.EqualTo(8f));
                Assert.That(payload.rightMaxStrength01, Is.EqualTo(0.8f));
                Assert.That(payload.rightMeanStrength01, Is.EqualTo(0.3f));
                Assert.That(payload.rightPressedSeconds, Is.EqualTo(18f));
                Assert.That(payload.rightMaxForceN, Is.EqualTo(14f));
                Assert.That(payload.rightMeanForceN, Is.EqualTo(7f));
                Assert.That(payload.bothPressedSeconds, Is.EqualTo(5f));
            Assert.That(payload.leftBalance01, Is.EqualTo(0.55f));
            StringAssert.Contains("\"displayName\":\"ผู้เล่นหนึ่ง\"", payload.ToJson());
        }

        [Test]
        public void From_UsesNullForUnavailableStatistics()
        {
            var result = new SessionResult
            {
                SchemaVersion = 1,
                SessionId = "keyboard-session",
                InputStatistics = new InputStatistics
                {
                    Left = Hand(true, false, 0.7f, 0.2f, 0f, 0f, 9f),
                    Right = null,
                    HasBalance = false
                }
            };

            ResultUploadPayload payload = ResultUploadPayload.From(result);

            Assert.That(payload.leftMaxStrength01, Is.EqualTo(0.7f));
                Assert.That(payload.leftMaxForceN, Is.Null);
                Assert.That(payload.leftMeanForceN, Is.Null);
                Assert.That(payload.rightMaxStrength01, Is.Null);
                Assert.That(payload.rightPressedSeconds, Is.Null);
            Assert.That(payload.leftBalance01, Is.Null);
            StringAssert.Contains("\"leftMaxForceN\":null", payload.ToJson());
            StringAssert.Contains("\"rightMaxStrength01\":null", payload.ToJson());
        }

        [Test]
        public void From_RejectsNullResult()
        {
            Assert.Throws<ArgumentNullException>(() => ResultUploadPayload.From(null));
        }

        private static HandStatistics Hand(bool hasStrength, bool hasForce, float maxStrength, float meanStrength, float maxForce, float meanForce, float pressedSeconds)
        {
            return new HandStatistics
            {
                HasStrength = hasStrength,
                HasForce = hasForce,
                MaxStrength01 = maxStrength,
                MeanStrength01 = meanStrength,
                MaxForceN = maxForce,
                MeanForceN = meanForce,
                PressedDurationSeconds = pressedSeconds
            };
        }
    }
}
