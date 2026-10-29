using System;
using System.Collections.Generic;

namespace CPS.KitchenCatch
{
    public enum VersusMatchState { Ready, Playing, Finished }

    public sealed class VersusMatchModel
    {
        private readonly VersusChallenge challenge;
        public VersusMatchState State { get; private set; } = VersusMatchState.Ready;
        public VersusPlayerModel Player1 { get; } = new VersusPlayerModel();
        public VersusPlayerModel Player2 { get; } = new VersusPlayerModel();
        public float PlayedSeconds { get; private set; }
        public int CurrentTier => challenge.At(PlayedSeconds).Tier;
        public bool IsPaused { get; private set; }
        public VersusMatchResult Result { get; private set; }

        public VersusMatchModel(VersusChallenge challenge)
        {
            this.challenge = challenge ?? throw new ArgumentNullException(nameof(challenge));
        }

        public void Start()
        {
            if (State != VersusMatchState.Ready) return;
            Player1.Reset();
            Player2.Reset();
            PlayedSeconds = 0;
            IsPaused = false;
            Result = null;
            State = VersusMatchState.Playing;
        }

        public void Pause()
        {
            if (State == VersusMatchState.Playing) IsPaused = true;
        }

        public void Resume()
        {
            if (State == VersusMatchState.Playing) IsPaused = false;
        }

        public void ApplyStep(float dt, IReadOnlyList<VersusObjectResolution> player1Resolutions,
            IReadOnlyList<VersusObjectResolution> player2Resolutions)
        {
            if (State != VersusMatchState.Playing || IsPaused) return;
            if (float.IsNaN(dt) || float.IsInfinity(dt) || dt < 0)
                throw new ArgumentException("Step duration must be finite and nonnegative.", nameof(dt));

            Resolve(Player1, player1Resolutions);
            Resolve(Player2, player2Resolutions);
            PlayedSeconds += dt;

            bool player1Dead = Player1.Health.CurrentHp == 0;
            bool player2Dead = Player2.Health.CurrentHp == 0;
            if (!player1Dead && !player2Dead) return;

            var winner = player1Dead && player2Dead ? VersusWinner.Draw :
                player1Dead ? VersusWinner.P2 : VersusWinner.P1;
            Result = new VersusMatchResult(winner, PlayedSeconds, Player1, Player2);
            State = VersusMatchState.Finished;
            IsPaused = false;
        }

        private static void Resolve(VersusPlayerModel player,
            IReadOnlyList<VersusObjectResolution> resolutions)
        {
            if (resolutions == null) return;
            for (int index = 0; index < resolutions.Count; index++) player.Resolve(resolutions[index]);
        }
    }
}
