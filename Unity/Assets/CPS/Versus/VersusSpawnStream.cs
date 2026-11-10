using System;
using System.Collections.Generic;

namespace CPS.KitchenCatch
{
    public sealed class VersusSpawnStream
    {
        private static readonly float[] Lanes = { -1, -.75f, -.5f, -.25f, 0, .25f, .5f, .75f, 1 };
        private readonly VersusChallenge challenge;
        private readonly Random random;
        private readonly List<int> freeLanes = new List<int>(Lanes.Length);
        private float nextWaveTime;
        private float nextObjectTime;
        private long nextId;
        private int remainingInWave;
        private VersusChallengeStep waveStep;

        public VersusSpawnStream(VersusChallenge challenge, int seed)
        {
            this.challenge = challenge ?? throw new ArgumentNullException(nameof(challenge));
            random = new Random(seed);
        }

        public bool TryNext(float activeSeconds, out VersusSpawnRecord record)
        {
            if (float.IsNaN(activeSeconds) || float.IsInfinity(activeSeconds) || activeSeconds < 0)
                throw new ArgumentException("Active time must be finite and nonnegative.", nameof(activeSeconds));

            float due = remainingInWave > 0 ? nextObjectTime : nextWaveTime;
            if (activeSeconds + 0.00001f < due)
            {
                record = default;
                return false;
            }
            if (remainingInWave == 0) StartWave();

            var kind = random.NextDouble() < waveStep.TrashProbability ? ObjectKind.Trash : ObjectKind.Food;
            int definition = random.Next(kind == ObjectKind.Food ? 3 : 2);
            record = new VersusSpawnRecord(++nextId, kind, definition, TakeFreeLane(),
                nextObjectTime, waveStep.FallSpeed);

            // The next object of this burst follows a short, random moment later.
            if (--remainingInWave > 0)
                nextObjectTime += waveStep.MinGapSeconds +
                    (float)random.NextDouble() * (waveStep.MaxGapSeconds - waveStep.MinGapSeconds);
            return true;
        }

        private void StartWave()
        {
            nextObjectTime = nextWaveTime;
            waveStep = challenge.At(nextWaveTime);
            remainingInWave = Math.Max(1, Math.Min(waveStep.ObjectsPerWave, Lanes.Length));
            freeLanes.Clear();
            for (int index = 0; index < Lanes.Length; index++) freeLanes.Add(index);
            nextWaveTime += waveStep.IntervalSeconds;
        }

        // Objects in one burst use different lanes, so the player has to move between them.
        private float TakeFreeLane()
        {
            int pick = random.Next(freeLanes.Count);
            int lane = freeLanes[pick];
            freeLanes.RemoveAt(pick);
            return Lanes[lane];
        }
    }
}
