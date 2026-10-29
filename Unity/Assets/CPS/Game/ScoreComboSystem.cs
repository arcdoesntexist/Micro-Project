using System;

namespace CPS.KitchenCatch
{
    public sealed class ScoreComboSystem
    {
        public int Score { get; private set; }
        public int Streak { get; private set; }
        public int MaxCombo { get; private set; }
        public ComboRank HighestRank { get; private set; }
        public int FoodCaught { get; private set; }
        public int FoodMissed { get; private set; }
        public int TrashEaten { get; private set; }

        public ComboRank CurrentRank
        {
            get
            {
                if (Streak >= 12) return ComboRank.SSS;
                if (Streak >= 8) return ComboRank.SS;
                if (Streak >= 5) return ComboRank.S;
                if (Streak >= 4) return ComboRank.A;
                if (Streak >= 3) return ComboRank.B;
                if (Streak >= 2) return ComboRank.C;
                return ComboRank.D;
            }
        }

        public void Reset()
        {
            Score = 0;
            Streak = 0;
            MaxCombo = 0;
            HighestRank = ComboRank.D;
            FoodCaught = 0;
            FoodMissed = 0;
            TrashEaten = 0;
        }

        public void Resolve(ObjectKind kind, ObjectOutcome outcome, int points)
        {
            if (kind == ObjectKind.Food && outcome == ObjectOutcome.Caught)
            {
                Score += points;
                FoodCaught++;
                Streak++;
                MaxCombo = Math.Max(MaxCombo, Streak);
                if (CurrentRank > HighestRank)
                    HighestRank = CurrentRank;
            }
            else if (kind == ObjectKind.Food && outcome == ObjectOutcome.Missed)
            {
                FoodMissed++;
                Streak = 0;
            }
            else if (kind == ObjectKind.Trash && outcome == ObjectOutcome.Caught)
            {
                TrashEaten++;
                Streak = 0;
            }
        }
    }
}
