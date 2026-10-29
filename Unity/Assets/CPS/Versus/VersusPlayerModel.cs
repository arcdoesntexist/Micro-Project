using System;
using System.Collections.Generic;

namespace CPS.KitchenCatch
{
    public readonly struct VersusObjectResolution
    {
        public long ObjectId { get; }
        public ObjectKind Kind { get; }
        public ObjectOutcome Outcome { get; }
        public int Points { get; }
        public int ItemIndex { get; }

        public VersusObjectResolution(long objectId, ObjectKind kind, ObjectOutcome outcome,
            int points, int itemIndex = -1)
        {
            ObjectId = objectId;
            Kind = kind;
            Outcome = outcome;
            Points = points;
            ItemIndex = itemIndex;
        }
    }

    public sealed class VersusPlayerModel
    {
        private readonly HashSet<long> resolvedObjectIds = new HashSet<long>();
        public HealthSystem Health { get; } = new HealthSystem();
        public ScoreComboSystem Score { get; } = new ScoreComboSystem();
        public int[] CaughtItems { get; } = new int[5];

        public void Reset()
        {
            Health.Reset(3);
            Score.Reset();
            resolvedObjectIds.Clear();
            Array.Clear(CaughtItems, 0, CaughtItems.Length);
        }

        public void Resolve(VersusObjectResolution resolution)
        {
            if (!resolvedObjectIds.Add(resolution.ObjectId)) return;
            Score.Resolve(resolution.Kind, resolution.Outcome, resolution.Points);
            if (resolution.Outcome == ObjectOutcome.Caught &&
                resolution.ItemIndex >= 0 && resolution.ItemIndex < CaughtItems.Length)
                CaughtItems[resolution.ItemIndex]++;
            if (resolution.Kind == ObjectKind.Trash && resolution.Outcome == ObjectOutcome.Caught)
                Health.Damage(1);
        }
    }
}
