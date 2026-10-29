using System;

namespace CPS.KitchenCatch
{
    [Serializable]
    public sealed class SessionResult
    {
        public int SchemaVersion = 1;
        public string SessionId;
        public string PlayerId;
        public string DisplayName;
        public string EndedAtUtc;
        public string InputSource;
        public string CalibrationVersion;
        public DifficultyId Difficulty;
        public EndReason EndReason;
        public float PlayedSeconds;
        public int Score;
        public int RemainingHp;
        public int FoodCaught;
        public int FoodMissed;
        public int TrashEaten;
        public int MaxCombo;
        public ComboRank HighestRank;
        public InputStatistics InputStatistics;
        public int[] CaughtItems;

        public SessionResult Copy()
        {
            var copy = (SessionResult)MemberwiseClone();
            copy.CaughtItems = CaughtItems == null ? null : (int[])CaughtItems.Clone();
            if (InputStatistics != null)
            {
                copy.InputStatistics = new InputStatistics
                {
                    Left = CopyHand(InputStatistics.Left),
                    Right = CopyHand(InputStatistics.Right),
                    BothPressedDurationSeconds = InputStatistics.BothPressedDurationSeconds,
                    HasBalance = InputStatistics.HasBalance,
                    LeftBalance01 = InputStatistics.LeftBalance01
                };
            }
            return copy;
        }

        private static HandStatistics CopyHand(HandStatistics hand)
        {
            if (hand == null)
                return null;

            return new HandStatistics
            {
                HasStrength = hand.HasStrength,
                HasForce = hand.HasForce,
                MaxStrength01 = hand.MaxStrength01,
                MeanStrength01 = hand.MeanStrength01,
                MaxForceN = hand.MaxForceN,
                MeanForceN = hand.MeanForceN,
                ValidDurationSeconds = hand.ValidDurationSeconds,
                ForceValidDurationSeconds = hand.ForceValidDurationSeconds,
                PressedDurationSeconds = hand.PressedDurationSeconds
            };
        }
    }
}
