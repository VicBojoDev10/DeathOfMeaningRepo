namespace TDOM.Gameplay.Camera
{
    public readonly struct CameraAutoRotationSettings
    {
        public float MinSpeed { get; }
        public float LookCooldown { get; }
        public float LookDeadzone { get; }
        public float GraceDelay { get; }
        public float RotationSpeed { get; }
        public float EaseAngle { get; }
        public float MinAngle { get; }
        public float MaxAngle { get; }

        public CameraAutoRotationSettings(
            float minSpeed,
            float lookCooldown,
            float lookDeadzone,
            float graceDelay,
            float rotationSpeed,
            float easeAngle,
            float minAngle,
            float maxAngle
        )
        {
            MinSpeed = minSpeed;
            LookCooldown = lookCooldown;
            LookDeadzone = lookDeadzone;
            GraceDelay = graceDelay;
            RotationSpeed = rotationSpeed;
            EaseAngle = easeAngle;
            MinAngle = minAngle;
            MaxAngle = maxAngle;
        }
    }
}
