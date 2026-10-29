using System;

namespace CPS.KitchenCatch
{
    [Serializable]
    public sealed class HandStatistics
    {
        public bool HasStrength;
        public bool HasForce;
        public float MaxStrength01;
        public float MeanStrength01;
        public float MaxForceN;
        public float MeanForceN;
        public float ValidDurationSeconds;
        public float ForceValidDurationSeconds;
        public float PressedDurationSeconds;
    }

    [Serializable]
    public sealed class InputStatistics
    {
        public HandStatistics Left;
        public HandStatistics Right;
        public float BothPressedDurationSeconds;
        public bool HasBalance;
        public float LeftBalance01;
    }
}
