using NUnit.Framework;

namespace CPS.KitchenCatch.Tests
{
    public sealed class VersusMatchModelTests
    {
        [Test]
        public void MissedFoodResetsComboWithoutDamagingHealth()
        {
            var model = Started();
            model.ApplyStep(1,
                new[] { Food(1, ObjectOutcome.Caught), Food(2, ObjectOutcome.Missed) }, null);

            Assert.That(model.Player1.Health.CurrentHp, Is.EqualTo(3));
            Assert.That(model.Player1.Score.FoodCaught, Is.EqualTo(1));
            Assert.That(model.Player1.Score.FoodMissed, Is.EqualTo(1));
            Assert.That(model.Player1.Score.Streak, Is.Zero);
            Assert.That(model.State, Is.EqualTo(VersusMatchState.Playing));
        }

        [Test]
        public void CaughtTrashDamagesOnlyThePlayerWhoCaughtIt()
        {
            var model = Started();
            model.ApplyStep(0.5f, new[] { Trash(1) }, null);

            Assert.That(model.Player1.Health.CurrentHp, Is.EqualTo(2));
            Assert.That(model.Player2.Health.CurrentHp, Is.EqualTo(3));
            Assert.That(model.Player1.Score.TrashEaten, Is.EqualTo(1));
        }

        [Test]
        public void PlayersScoreIndependentlyAndDeduplicatePerField()
        {
            var model = Started();
            model.ApplyStep(1,
                new[] { Food(7, ObjectOutcome.Caught), Food(7, ObjectOutcome.Caught) },
                new[] { Food(7, ObjectOutcome.Caught) });

            Assert.That(model.Player1.Score.Score, Is.EqualTo(10));
            Assert.That(model.Player2.Score.Score, Is.EqualTo(10));
            Assert.That(model.Player1.CaughtItems[0], Is.EqualTo(1));
            Assert.That(model.Player2.CaughtItems[0], Is.EqualTo(1));
        }

        [Test]
        public void ActiveTimeExcludesPauseAndDoesNotEndAtSixtySeconds()
        {
            var model = Started();
            model.Pause();
            model.ApplyStep(30, null, null);
            model.Resume();
            model.ApplyStep(61, null, null);

            Assert.That(model.PlayedSeconds, Is.EqualTo(61));
            Assert.That(model.State, Is.EqualTo(VersusMatchState.Playing));
            Assert.That(model.CurrentTier, Is.EqualTo(3));
        }

        [Test]
        public void FirstDeathMakesTheOtherPlayerWinner()
        {
            var model = Started();
            model.ApplyStep(1, new[] { Trash(1), Trash(2), Trash(3) }, null);

            Assert.That(model.State, Is.EqualTo(VersusMatchState.Finished));
            Assert.That(model.Result.Winner, Is.EqualTo(VersusWinner.P2));
            Assert.That(model.Result.Player1.RemainingHp, Is.Zero);
            Assert.That(model.Result.Player2.RemainingHp, Is.EqualTo(3));
        }

        [Test]
        public void SimultaneousDeathIsDrawRegardlessOfPlayerOrder()
        {
            var model = Started();
            model.ApplyStep(1,
                new[] { Trash(1), Trash(2), Trash(3) },
                new[] { Trash(1), Trash(2), Trash(3) });

            Assert.That(model.State, Is.EqualTo(VersusMatchState.Finished));
            Assert.That(model.Result.Winner, Is.EqualTo(VersusWinner.Draw));
            Assert.That(model.Result.Player1.RemainingHp, Is.Zero);
            Assert.That(model.Result.Player2.RemainingHp, Is.Zero);
        }

        [Test]
        public void FinishedResultIsStableAndStopsFurtherScoring()
        {
            var model = Started();
            model.ApplyStep(1, new[] { Trash(1), Trash(2), Trash(3) }, null);
            var result = model.Result;
            model.ApplyStep(5, null, new[] { Food(4, ObjectOutcome.Caught) });

            Assert.That(model.Result, Is.SameAs(result));
            Assert.That(model.PlayedSeconds, Is.EqualTo(1));
            Assert.That(model.Player2.Score.Score, Is.Zero);
        }

        private static VersusMatchModel Started()
        {
            var model = new VersusMatchModel(new VersusChallenge());
            model.Start();
            return model;
        }

        private static VersusObjectResolution Food(long id, ObjectOutcome outcome) =>
            new VersusObjectResolution(id, ObjectKind.Food, outcome, 10, 0);

        private static VersusObjectResolution Trash(long id) =>
            new VersusObjectResolution(id, ObjectKind.Trash, ObjectOutcome.Caught, 0, 3);
    }
}
