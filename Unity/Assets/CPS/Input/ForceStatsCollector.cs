using System;

namespace CPS.KitchenCatch
{
    public sealed class ForceStatsCollector
    {
        private readonly HandTotals left = new HandTotals();
        private readonly HandTotals right = new HandTotals();
        private double bothPressedDuration;

        public void Accumulate(InputSnapshot input, float dt)
        {
            if (!IsFinite(dt) || dt <= 0)
                return;

            left.Accumulate(input.Left, dt);
            right.Accumulate(input.Right, dt);
            if (input.Left.Valid && input.Left.Pressed && input.Right.Valid && input.Right.Pressed)
                bothPressedDuration += dt;
        }

        public void Reset()
        {
            left.Reset();
            right.Reset();
            bothPressedDuration = 0;
        }

        public InputStatistics Snapshot()
        {
            double pressedTotal = left.PressedDuration + right.PressedDuration;
            return new InputStatistics
            {
                Left = left.Snapshot(),
                Right = right.Snapshot(),
                BothPressedDurationSeconds = ToFloat(bothPressedDuration),
                HasBalance = pressedTotal > 0,
                LeftBalance01 = pressedTotal > 0 ? (float)(left.PressedDuration / pressedTotal) : 0
            };
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static float ToFloat(double value) => (float)Math.Min(value, float.MaxValue);

        private sealed class HandTotals
        {
            public double PressedDuration;
            private double strengthDuration;
            private double strengthIntegral;
            private float maxStrength;
            private double forceDuration;
            private double forceIntegral;
            private float maxForce;

            public void Accumulate(HandInput input, float dt)
            {
                if (!input.Valid)
                    return;

                if (input.Pressed)
                    PressedDuration += dt;
                if (input.HasStrength && IsFinite(input.Strength01))
                {
                    float strength = Math.Clamp(input.Strength01, 0, 1);
                    strengthDuration += dt;
                    strengthIntegral += (double)strength * dt;
                    maxStrength = Math.Max(maxStrength, strength);
                }
                if (input.ForceN.HasValue && IsFinite(input.ForceN.Value) && input.ForceN.Value >= 0)
                {
                    float force = input.ForceN.Value;
                    forceDuration += dt;
                    forceIntegral += (double)force * dt;
                    maxForce = Math.Max(maxForce, force);
                }
            }

            public HandStatistics Snapshot() => new HandStatistics
            {
                HasStrength = strengthDuration > 0,
                HasForce = forceDuration > 0,
                MaxStrength01 = maxStrength,
                MeanStrength01 = strengthDuration > 0 ? ToFloat(strengthIntegral / strengthDuration) : 0,
                MaxForceN = maxForce,
                MeanForceN = forceDuration > 0 ? ToFloat(forceIntegral / forceDuration) : 0,
                ValidDurationSeconds = ToFloat(strengthDuration),
                ForceValidDurationSeconds = ToFloat(forceDuration),
                PressedDurationSeconds = ToFloat(PressedDuration)
            };

            public void Reset()
            {
                PressedDuration = 0;
                strengthDuration = 0;
                strengthIntegral = 0;
                maxStrength = 0;
                forceDuration = 0;
                forceIntegral = 0;
                maxForce = 0;
            }
        }
    }
}
