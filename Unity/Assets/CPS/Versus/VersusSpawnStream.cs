using System;

namespace CPS.KitchenCatch
{
    public sealed class VersusSpawnStream
    {
        private static readonly float[] Lanes = { -1, -.75f, -.5f, -.25f, 0, .25f, .5f, .75f, 1 };
        private readonly VersusChallenge challenge;
        private readonly Random random;
        private float nextSpawnTime;
        private long nextId;

        public VersusSpawnStream(VersusChallenge challenge, int seed)
        {
            this.challenge = challenge ?? throw new ArgumentNullException(nameof(challenge));
            random = new Random(seed);
        }

        public bool TryNext(float activeSeconds, out VersusSpawnRecord record)
        {
            if (float.IsNaN(activeSeconds) || float.IsInfinity(activeSeconds) || activeSeconds < 0)
                throw new ArgumentException("Active time must be finite and nonnegative.", nameof(activeSeconds));
            if (activeSeconds + 0.00001f < nextSpawnTime)
            {
                record = default;
                return false;
            }

            var step = challenge.At(nextSpawnTime);
            var kind = random.NextDouble() < step.TrashProbability ? ObjectKind.Trash : ObjectKind.Food;
            record = new VersusSpawnRecord(++nextId, kind,
                random.Next(kind == ObjectKind.Food ? 3 : 2),
                Lanes[random.Next(Lanes.Length)], nextSpawnTime, step.FallSpeed);
            nextSpawnTime += step.IntervalSeconds;
            return true;
        }
    }
}
