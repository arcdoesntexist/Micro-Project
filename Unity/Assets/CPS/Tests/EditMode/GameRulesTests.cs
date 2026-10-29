using NUnit.Framework;
using UnityEngine;

namespace CPS.KitchenCatch.Tests
{
    public sealed class GameRulesTests
    {
        [Test]
        public void FoodPointsAre10_15_20()
        {
            var score = new ScoreComboSystem();
            score.Reset();
            score.Resolve(ObjectKind.Food, ObjectOutcome.Caught, 10);
            score.Resolve(ObjectKind.Food, ObjectOutcome.Caught, 15);
            score.Resolve(ObjectKind.Food, ObjectOutcome.Caught, 20);
            int scoreAfterAppleBreadCake = score.Score;
            Assert.That(scoreAfterAppleBreadCake, Is.EqualTo(45));
            Assert.That(score.FoodCaught, Is.EqualTo(3));
        }

        [Test]
        public void TrashMissIsHarmless()
        {
            var model = StartedModel();
            model.Resolve(1, ObjectKind.Food, ObjectOutcome.Caught, 10);
            model.Resolve(2, ObjectKind.Trash, ObjectOutcome.Missed, 99);
            Assert.That(model.Health.CurrentHp, Is.EqualTo(3));
            Assert.That(model.Score.Score, Is.EqualTo(10));
            Assert.That(model.Score.Streak, Is.EqualTo(1));
            Assert.That(model.Score.TrashEaten, Is.Zero);
            Assert.That(model.Score.FoodMissed, Is.Zero);
        }

        [Test]
        public void DamageClampsAtZero()
        {
            var health = new HealthSystem();
            health.Reset(3);
            health.Damage(1);
            Assert.That(health.CurrentHp, Is.EqualTo(2));
            health.Damage(10);
            health.Damage(1);
            Assert.That(health.CurrentHp, Is.Zero);
            health.Reset(3);
            Assert.That(health.CurrentHp, Is.EqualTo(3));
        }

        [Test]
        public void DuplicateObjectIdIsIgnored()
        {
            var model = StartedModel();
            model.Resolve(42, ObjectKind.Food, ObjectOutcome.Caught, 15);
            model.CompleteStep(1);
            model.Resolve(42, ObjectKind.Food, ObjectOutcome.Missed, 15);
            Assert.That(model.Score.Score, Is.EqualTo(15));
            Assert.That(model.Score.FoodCaught, Is.EqualTo(1));
            Assert.That(model.Score.FoodMissed, Is.Zero);
            Assert.That(model.Health.CurrentHp, Is.EqualTo(3));
        }

        [Test]
        public void ComboThresholdsAndReset()
        {
            var score = new ScoreComboSystem();
            score.Reset();
            var ranksAtStreaks2_3_4_5_8_12 = new ComboRank[6];
            int rankIndex = 0;
            for (int streak = 1; streak <= 12; streak++)
            {
                score.Resolve(ObjectKind.Food, ObjectOutcome.Caught, 10);
                if (streak == 2 || streak == 3 || streak == 4 || streak == 5 || streak == 8 || streak == 12)
                    ranksAtStreaks2_3_4_5_8_12[rankIndex++] = score.CurrentRank;
            }
            Assert.That(ranksAtStreaks2_3_4_5_8_12, Is.EqualTo(new[] { ComboRank.C, ComboRank.B, ComboRank.A, ComboRank.S, ComboRank.SS, ComboRank.SSS }));
            score.Resolve(ObjectKind.Food, ObjectOutcome.Missed, 10);
            Assert.That(score.Streak, Is.Zero);
            Assert.That(score.CurrentRank, Is.EqualTo(ComboRank.D));
            Assert.That(score.MaxCombo, Is.EqualTo(12));
            Assert.That(score.HighestRank, Is.EqualTo(ComboRank.SSS));
            Assert.That(score.Score, Is.EqualTo(120));
            Assert.That(score.FoodMissed, Is.EqualTo(1));
            score.Reset();
            Assert.That(score.Score, Is.Zero);
            Assert.That(score.FoodCaught, Is.Zero);
            Assert.That(score.FoodMissed, Is.Zero);
            Assert.That(score.TrashEaten, Is.Zero);
            Assert.That(score.Streak, Is.Zero);
            Assert.That(score.MaxCombo, Is.Zero);
            Assert.That(score.HighestRank, Is.EqualTo(ComboRank.D));
        }

        [TestCase(0, ComboRank.D)]
        [TestCase(1, ComboRank.D)]
        [TestCase(2, ComboRank.C)]
        [TestCase(3, ComboRank.B)]
        [TestCase(4, ComboRank.A)]
        [TestCase(5, ComboRank.S)]
        [TestCase(7, ComboRank.S)]
        [TestCase(8, ComboRank.SS)]
        [TestCase(11, ComboRank.SS)]
        [TestCase(12, ComboRank.SSS)]
        [TestCase(13, ComboRank.SSS)]
        public void RankBoundary(int streak, ComboRank expected)
        {
            var score = new ScoreComboSystem();
            score.Reset();
            for (int i = 0; i < streak; i++)
                score.Resolve(ObjectKind.Food, ObjectOutcome.Caught, 10);
            Assert.That(score.CurrentRank, Is.EqualTo(expected));
            Assert.That(score.HighestRank, Is.EqualTo(expected));
        }

        [TestCase(ObjectKind.Food, ObjectOutcome.Missed)]
        [TestCase(ObjectKind.Trash, ObjectOutcome.Caught)]
        public void HarmfulOutcomeDamagesOnceAndResetsCombo(ObjectKind kind, ObjectOutcome outcome)
        {
            var model = StartedModel();
            model.Resolve(1, ObjectKind.Food, ObjectOutcome.Caught, 20);
            model.Resolve(2, ObjectKind.Food, ObjectOutcome.Caught, 20);
            model.Resolve(3, kind, outcome, 99);
            model.Resolve(3, kind, outcome, 99);
            Assert.That(model.Health.CurrentHp, Is.EqualTo(2));
            Assert.That(model.Score.Streak, Is.Zero);
            Assert.That(model.Score.Score, Is.EqualTo(40));
            Assert.That(model.Score.MaxCombo, Is.EqualTo(2));
            Assert.That(model.Score.HighestRank, Is.EqualTo(ComboRank.C));
            Assert.That(model.Score.FoodMissed, Is.EqualTo(kind == ObjectKind.Food ? 1 : 0));
            Assert.That(model.Score.TrashEaten, Is.EqualTo(kind == ObjectKind.Trash ? 1 : 0));
        }

        [Test]
        public void DeathWinsAtDeadline()
        {
            var model = StartedModel();
            EndReason? reason = null;
            model.Ended += value => reason = value;
            model.CompleteStep(59);
            model.Resolve(1, ObjectKind.Food, ObjectOutcome.Missed, 10);
            model.Resolve(2, ObjectKind.Trash, ObjectOutcome.Caught, 0);
            model.Resolve(3, ObjectKind.Food, ObjectOutcome.Missed, 10);
            Assert.That(model.State, Is.EqualTo(SessionState.Playing));
            model.CompleteStep(1);
            Assert.That(model.PlayedSeconds, Is.EqualTo(60).Within(0.001));
            Assert.That(model.State, Is.EqualTo(SessionState.Dying));
            Assert.That(reason, Is.EqualTo(EndReason.HpDepleted));
        }

        [Test]
        public void LargeDtStopsAt60()
        {
            var model = StartedModel();
            model.CompleteStep(59.5f);
            Assert.That(model.RemainingSeconds, Is.EqualTo(0.5f).Within(0.001));
            model.CompleteStep(5);
            Assert.That(model.PlayedSeconds, Is.EqualTo(60).Within(0.001));
            Assert.That(model.RemainingSeconds, Is.Zero);
            Assert.That(model.State, Is.EqualTo(SessionState.Triumph));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TerminalEventOnce(bool depleteHp)
        {
            var model = StartedModel();
            int endedCount = 0;
            EndReason? reason = null;
            model.Ended += value => { endedCount++; reason = value; };
            if (depleteHp)
                for (int i = 1; i <= 3; i++)
                    model.Resolve(i, ObjectKind.Trash, ObjectOutcome.Caught, 0);
            model.CompleteStep(60);
            model.CompleteStep(1);
            model.Start();
            model.CompleteStep(60);
            Assert.That(endedCount, Is.EqualTo(1));
            Assert.That(reason, Is.EqualTo(depleteHp ? EndReason.HpDepleted : EndReason.Triumph));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TerminalRejectsFurtherOutcomes(bool depleteHp)
        {
            var model = StartedModel();
            model.Resolve(1, ObjectKind.Food, ObjectOutcome.Caught, 10);
            if (depleteHp)
                for (int i = 2; i <= 4; i++)
                    model.Resolve(i, ObjectKind.Trash, ObjectOutcome.Caught, 0);
            model.CompleteStep(60);
            model.Resolve(100, ObjectKind.Food, ObjectOutcome.Caught, 20);
            model.Resolve(101, ObjectKind.Food, ObjectOutcome.Missed, 20);
            Assert.That(model.Score.Score, Is.EqualTo(10));
            Assert.That(model.Score.FoodCaught, Is.EqualTo(1));
            Assert.That(model.Score.FoodMissed, Is.Zero);
            Assert.That(model.Health.CurrentHp, Is.EqualTo(depleteHp ? 0 : 3));
            Assert.That(model.PlayedSeconds, Is.EqualTo(60));
        }

        [Test]
        public void ReadyAndRepeatedStartAreGuarded()
        {
            var model = new GameSessionModel();
            Assert.That(model.State, Is.EqualTo(SessionState.Ready));
            model.Resolve(1, ObjectKind.Food, ObjectOutcome.Caught, 10);
            model.CompleteStep(20);
            Assert.That(model.PlayedSeconds, Is.Zero);
            Assert.That(model.Score.Score, Is.Zero);
            model.Start();
            model.Resolve(1, ObjectKind.Food, ObjectOutcome.Caught, 10);
            model.CompleteStep(2);
            model.Start();
            Assert.That(model.State, Is.EqualTo(SessionState.Playing));
            Assert.That(model.PlayedSeconds, Is.EqualTo(2));
            Assert.That(model.Score.Score, Is.EqualTo(10));
        }

        [Test]
        public void ResultCopyDoesNotShareStatistics()
        {
            var original = ResultFixture();
            var copy = original.Copy();
            AssertResultFixture(copy);
            Assert.That(copy.InputStatistics, Is.Not.SameAs(original.InputStatistics));
            Assert.That(copy.InputStatistics.Left, Is.Not.SameAs(original.InputStatistics.Left));
            Assert.That(copy.InputStatistics.Right, Is.Not.SameAs(original.InputStatistics.Right));
            copy.Score = -1;
            copy.DisplayName = "Changed";
            copy.InputStatistics.Left.MaxStrength01 = 0;
            copy.InputStatistics.Right.MaxForceN = 0;
            copy.InputStatistics.BothPressedDurationSeconds = 0;
            AssertResultFixture(original);
        }

        [Test]
        public void ResultCopyPreservesUnavailableStatistics()
        {
            var original = new SessionResult();
            var copy = original.Copy();
            Assert.That(copy.SchemaVersion, Is.EqualTo(1));
            Assert.That(copy.InputStatistics, Is.Null);
            original.InputStatistics = new InputStatistics { Left = new HandStatistics() };
            copy = original.Copy();
            Assert.That(copy.InputStatistics.Right, Is.Null);
            Assert.That(copy.InputStatistics.Left.HasForce, Is.False);
            Assert.That(copy.InputStatistics.Left.MaxForceN, Is.Zero);
            Assert.That(copy.InputStatistics.Left.MeanForceN, Is.Zero);
        }

        [Test]
        public void ResultJsonRoundTripsAllFields()
        {
            var copy = ResultFixture().Copy();
            var restored = JsonUtility.FromJson<SessionResult>(JsonUtility.ToJson(copy));
            AssertResultFixture(restored);
        }

        private static GameSessionModel StartedModel()
        {
            var model = new GameSessionModel();
            model.Start();
            return model;
        }

        private static SessionResult ResultFixture()
        {
            return new SessionResult
            {
                SessionId = "session-1", PlayerId = "player-2", DisplayName = "Chef",
                EndedAtUtc = "2026-10-08T01:02:03Z", InputSource = "hardware",
                Difficulty = DifficultyId.Hard, EndReason = EndReason.HpDepleted,
                PlayedSeconds = 42.5f, Score = 145, RemainingHp = 0,
                FoodCaught = 10, FoodMissed = 2, TrashEaten = 1, MaxCombo = 8,
                HighestRank = ComboRank.SS,
                InputStatistics = new InputStatistics
                {
                    Left = new HandStatistics
                    {
                        HasStrength = true, HasForce = true, MaxStrength01 = 0.8f,
                        MeanStrength01 = 0.4f, MaxForceN = 12, MeanForceN = 6,
                        ValidDurationSeconds = 40, ForceValidDurationSeconds = 30,
                        PressedDurationSeconds = 20
                    },
                    Right = new HandStatistics
                    {
                        HasStrength = true, HasForce = true, MaxStrength01 = 0.9f,
                        MeanStrength01 = 0.5f, MaxForceN = 14, MeanForceN = 7,
                        ValidDurationSeconds = 39, ForceValidDurationSeconds = 29,
                        PressedDurationSeconds = 19
                    },
                    BothPressedDurationSeconds = 10, HasBalance = true, LeftBalance01 = 0.45f
                }
            };
        }

        private static void AssertResultFixture(SessionResult result)
        {
            Assert.That(result.SchemaVersion, Is.EqualTo(1));
            Assert.That(result.SessionId, Is.EqualTo("session-1"));
            Assert.That(result.PlayerId, Is.EqualTo("player-2"));
            Assert.That(result.DisplayName, Is.EqualTo("Chef"));
            Assert.That(result.EndedAtUtc, Is.EqualTo("2026-10-08T01:02:03Z"));
            Assert.That(result.InputSource, Is.EqualTo("hardware"));
            Assert.That(result.Difficulty, Is.EqualTo(DifficultyId.Hard));
            Assert.That(result.EndReason, Is.EqualTo(EndReason.HpDepleted));
            Assert.That(result.PlayedSeconds, Is.EqualTo(42.5f));
            Assert.That(result.Score, Is.EqualTo(145));
            Assert.That(result.RemainingHp, Is.Zero);
            Assert.That(result.FoodCaught, Is.EqualTo(10));
            Assert.That(result.FoodMissed, Is.EqualTo(2));
            Assert.That(result.TrashEaten, Is.EqualTo(1));
            Assert.That(result.MaxCombo, Is.EqualTo(8));
            Assert.That(result.HighestRank, Is.EqualTo(ComboRank.SS));
            Assert.That(result.InputStatistics.BothPressedDurationSeconds, Is.EqualTo(10));
            Assert.That(result.InputStatistics.HasBalance, Is.True);
            Assert.That(result.InputStatistics.LeftBalance01, Is.EqualTo(0.45f));
            AssertHandFixture(result.InputStatistics.Left, 0.8f, 0.4f, 12, 6, 40, 30, 20);
            AssertHandFixture(result.InputStatistics.Right, 0.9f, 0.5f, 14, 7, 39, 29, 19);
        }

        private static void AssertHandFixture(HandStatistics hand, float maxStrength, float meanStrength,
            float maxForce, float meanForce, float validDuration, float forceDuration, float pressedDuration)
        {
            Assert.That(hand.HasStrength, Is.True);
            Assert.That(hand.HasForce, Is.True);
            Assert.That(hand.MaxStrength01, Is.EqualTo(maxStrength));
            Assert.That(hand.MeanStrength01, Is.EqualTo(meanStrength));
            Assert.That(hand.MaxForceN, Is.EqualTo(maxForce));
            Assert.That(hand.MeanForceN, Is.EqualTo(meanForce));
            Assert.That(hand.ValidDurationSeconds, Is.EqualTo(validDuration));
            Assert.That(hand.ForceValidDurationSeconds, Is.EqualTo(forceDuration));
            Assert.That(hand.PressedDurationSeconds, Is.EqualTo(pressedDuration));
        }
    }
}
