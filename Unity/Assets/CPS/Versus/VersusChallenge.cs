using System;
using UnityEngine;

namespace CPS.KitchenCatch
{
    public readonly struct VersusChallengeStep
    {
        public int Tier { get; }
        public float IntervalSeconds { get; }
        public float TrashProbability { get; }
        public float FallSpeed { get; }
        public int ObjectsPerWave { get; }
        public float MinGapSeconds { get; }
        public float MaxGapSeconds { get; }

        public VersusChallengeStep(int tier, float intervalSeconds, float trashProbability, float fallSpeed,
            int objectsPerWave = 1, float minGapSeconds = 0, float maxGapSeconds = 0)
        {
            Tier = tier;
            IntervalSeconds = intervalSeconds;
            TrashProbability = trashProbability;
            FallSpeed = fallSpeed;
            ObjectsPerWave = objectsPerWave;
            MinGapSeconds = minGapSeconds;
            MaxGapSeconds = maxGapSeconds;
        }
    }

    [Serializable]
    public sealed class VersusChallenge
    {
        public const int MaxObjectsPerWaveLimit = 9;

        [SerializeField, Min(1)] private float tierDurationSeconds = 20;
        [SerializeField, Min(0)] private int maxTier = 8;
        [SerializeField, Min(0.05f)] private float initialIntervalSeconds = 1.2f;
        [SerializeField, Min(0)] private float intervalDecreasePerTier = 0.08f;
        [SerializeField, Range(0, 1)] private float initialTrashProbability = 0.2f;
        [SerializeField, Range(0, 1)] private float trashIncreasePerTier = 0.05f;
        [SerializeField, Min(0.1f)] private float initialFallSpeed = 5.5f;
        [SerializeField, Min(0)] private float fallSpeedIncreasePerTier = 0.35f;

        [Header("Objects per wave")]
        [Tooltip("How many objects fall in one burst at tier 0.")]
        [SerializeField, Range(1, MaxObjectsPerWaveLimit)] private int initialObjectsPerWave = 1;
        [Tooltip("Add one more object per wave every N tiers. 0 = never add.")]
        [SerializeField, Min(0)] private int tiersPerExtraObject = 3;
        [Tooltip("Upper limit of objects in one burst.")]
        [SerializeField, Range(1, MaxObjectsPerWaveLimit)] private int maxObjectsPerWave = 3;
        [Tooltip("Shortest delay between objects inside one burst.")]
        [SerializeField, Min(0.02f)] private float minGapSeconds = 0.15f;
        [Tooltip("Longest delay between objects inside one burst.")]
        [SerializeField, Min(0.02f)] private float maxGapSeconds = 0.35f;

        public VersusChallengeStep At(float activeSeconds)
        {
            if (float.IsNaN(activeSeconds) || float.IsInfinity(activeSeconds) || activeSeconds < 0)
                throw new ArgumentException("Active time must be finite and nonnegative.", nameof(activeSeconds));
            int tier = Mathf.Min(maxTier, Mathf.FloorToInt(activeSeconds / tierDurationSeconds));
            float interval = Mathf.Max(0.05f, initialIntervalSeconds - intervalDecreasePerTier * tier);
            int count = ObjectsPerWaveAt(tier);
            // Keep the whole burst inside 80% of the interval so bursts never run into each other.
            float gapCap = count > 1 ? interval * 0.8f / (count - 1) : 0;
            float maxGap = Mathf.Min(Mathf.Max(minGapSeconds, maxGapSeconds), gapCap);
            float minGap = Mathf.Min(minGapSeconds, maxGap);
            return new VersusChallengeStep(tier, interval,
                Mathf.Clamp01(initialTrashProbability + trashIncreasePerTier * tier),
                initialFallSpeed + fallSpeedIncreasePerTier * tier,
                count, minGap, maxGap);
        }

        private int ObjectsPerWaveAt(int tier)
        {
            int extra = tiersPerExtraObject > 0 ? tier / tiersPerExtraObject : 0;
            int cap = Mathf.Clamp(maxObjectsPerWave, 1, MaxObjectsPerWaveLimit);
            return Mathf.Clamp(initialObjectsPerWave + extra, 1, cap);
        }
    }
}
