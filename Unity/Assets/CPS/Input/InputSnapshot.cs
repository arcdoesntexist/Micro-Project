namespace CPS.KitchenCatch
{
    public readonly struct HandInput
    {
        public bool Pressed { get; }
        public float Strength01 { get; }
        public bool Valid { get; }
        public float? ForceN { get; }
        public bool HasStrength { get; }

        public HandInput(bool pressed, float strength01, bool valid, float? forceN, bool hasStrength = true)
        {
            Pressed = pressed;
            Strength01 = strength01;
            Valid = valid;
            ForceN = forceN;
            HasStrength = hasStrength;
        }
    }

    public readonly struct InputSnapshot
    {
        public HandInput Left { get; }
        public HandInput Right { get; }
        public string Source { get; }
        public string CalibrationVersion { get; }

        public InputSnapshot(HandInput left, HandInput right, string source, string calibrationVersion = null)
        {
            Left = left;
            Right = right;
            Source = source;
            CalibrationVersion = calibrationVersion;
        }
    }
}
