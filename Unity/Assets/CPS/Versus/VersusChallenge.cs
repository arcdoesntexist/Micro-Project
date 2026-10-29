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

        public VersusChallengeStep(int tier, float intervalSeconds, float trashProbability, float fallSpeed)
        {
            Tier = tier;
            IntervalSeconds = intervalSeconds;
            TrashProbability = trashProbability;
            FallSpeed = fallSpeed;
        }
    }

    [Serializable]
    public sealed class VersusChallenge
    {
        [SerializeField, Min(1)] private float tierDurationSeconds = 20;
        [SerializeField, Min(0)] private int maxTier = 8;
        [SerializeField, Min(0.05f)] private float initialIntervalSeconds = 1.2f;
        [SerializeField, Min(0)] private float intervalDecreasePerTier = 0.08f;
        [SerializeField, Range(0, 1)] private float initialTrashProbability = 0.2f;
        [SerializeField, Range(0, 1)] private float trashIncreasePerTier = 0.05f;
        [SerializeField, Min(0.1f)] private float initialFallSpeed = 5.5f;
        [SerializeField, Min(0)] private float fallSpeedIncreasePerTier = 0.35f;

        public VersusChallengeStep At(float activeSeconds)
        {
            if (float.IsNaN(activeSeconds) || float.IsInfinity(activeSeconds) || activeSeconds < 0)
                throw new ArgumentException("Active time must be finite and nonnegative.", nameof(activeSeconds));
            int tier = Mathf.Min(maxTier, Mathf.FloorToInt(activeSeconds / tierDurationSeconds));
            return new VersusChallengeStep(tier,
                Mathf.Max(0.05f, initialIntervalSeconds - intervalDecreasePerTier * tier),
                Mathf.Clamp01(initialTrashProbability + trashIncreasePerTier * tier),
                initialFallSpeed + fallSpeedIncreasePerTier * tier);
        }
    }
}
