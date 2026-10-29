using System;
using System.Collections.Generic;

namespace CPS.KitchenCatch
{
    public sealed class GameSessionModel
    {
        private const float DurationSeconds = 60;
        private readonly HashSet<long> resolvedObjectIds = new HashSet<long>();

        public SessionState State { get; private set; } = SessionState.Ready;
        public float PlayedSeconds { get; private set; }
        public float RemainingSeconds => DurationSeconds - PlayedSeconds;
        public HealthSystem Health { get; } = new HealthSystem();
        public ScoreComboSystem Score { get; } = new ScoreComboSystem();
        public int[] CaughtItems { get; } = new int[5];
        public event Action<EndReason> Ended;

        public void Start()
        {
            if (State != SessionState.Ready)
                return;

            Health.Reset(3);
            Score.Reset();
            State = SessionState.Playing;
        }

        public void Resolve(long objectId, ObjectKind kind, ObjectOutcome outcome, int points, int itemIndex = -1)
        {
            if (State != SessionState.Playing || !resolvedObjectIds.Add(objectId))
                return;

            Score.Resolve(kind, outcome, points);
            if (outcome == ObjectOutcome.Caught && itemIndex >= 0 && itemIndex < CaughtItems.Length)
                CaughtItems[itemIndex]++;
            if ((kind == ObjectKind.Food && outcome == ObjectOutcome.Missed) ||
                (kind == ObjectKind.Trash && outcome == ObjectOutcome.Caught))
                Health.Damage(1);
        }

        public void CompleteStep(float dt)
        {
            if (State != SessionState.Playing)
                return;

            PlayedSeconds += Math.Min(RemainingSeconds, Math.Max(0, dt));
            if (Health.CurrentHp == 0)
            {
                State = SessionState.Dying;
                Ended?.Invoke(EndReason.HpDepleted);
            }
            else if (RemainingSeconds <= 0)
            {
                State = SessionState.Triumph;
                Ended?.Invoke(EndReason.Triumph);
            }
        }
    }
}
