namespace CPS.KitchenCatch
{
    public enum VersusWinner { P1, P2, Draw }

    public sealed class VersusPlayerResult
    {
        public int RemainingHp { get; }
        public int Score { get; }
        public int FoodCaught { get; }
        public int FoodMissed { get; }
        public int TrashEaten { get; }
        public int MaxCombo { get; }
        public ComboRank HighestRank { get; }
        public int[] CaughtItems { get; }

        internal VersusPlayerResult(VersusPlayerModel player)
        {
            RemainingHp = player.Health.CurrentHp;
            Score = player.Score.Score;
            FoodCaught = player.Score.FoodCaught;
            FoodMissed = player.Score.FoodMissed;
            TrashEaten = player.Score.TrashEaten;
            MaxCombo = player.Score.MaxCombo;
            HighestRank = player.Score.HighestRank;
            CaughtItems = (int[])player.CaughtItems.Clone();
        }
    }

    public sealed class VersusMatchResult
    {
        public VersusWinner Winner { get; }
        public float PlayedSeconds { get; }
        public VersusPlayerResult Player1 { get; }
        public VersusPlayerResult Player2 { get; }

        internal VersusMatchResult(VersusWinner winner, float playedSeconds,
            VersusPlayerModel player1, VersusPlayerModel player2)
        {
            Winner = winner;
            PlayedSeconds = playedSeconds;
            Player1 = new VersusPlayerResult(player1);
            Player2 = new VersusPlayerResult(player2);
        }
    }
}
