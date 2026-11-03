namespace TDOM.Gameplay.Camera
{
    public readonly struct LockOnSettings
    {
        public float Range { get; }
        public float SmoothTime { get; }
        public float PitchMin { get; }
        public float PitchMax { get; }

        public LockOnSettings(float range, float smoothTime, float pitchMin, float pitchMax)
        {
            Range = range;
            SmoothTime = smoothTime;
            PitchMin = pitchMin;
            PitchMax = pitchMax;
        }
    }
}
