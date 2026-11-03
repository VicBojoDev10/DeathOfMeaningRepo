namespace TDOM.Gameplay.Camera
{
    public readonly struct LockOnSwitchSettings
    {
        public float Range { get; }
        public float SwitchThreshold { get; }
        public float ResetThreshold { get; }
        public float MaxAngle { get; }

        public LockOnSwitchSettings(
            float range,
            float switchThreshold,
            float resetThreshold,
            float maxAngle
        )
        {
            Range = range;
            SwitchThreshold = switchThreshold;
            ResetThreshold = resetThreshold;
            MaxAngle = maxAngle;
        }
    }
}
