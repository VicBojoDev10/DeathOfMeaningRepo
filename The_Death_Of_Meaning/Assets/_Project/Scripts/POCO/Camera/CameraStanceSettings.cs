namespace TDOM.Gameplay.Camera
{
    public readonly struct CameraStanceSettings
    {
        public StanceFraming Exploration { get; }
        public StanceFraming Sprint { get; }
        public StanceFraming Combat { get; }
        public StanceFraming Aim { get; }
        public float TransitionSpeed { get; }
        public float CombatLingerSeconds { get; }
        public float ShoulderSwapSpeed { get; }
        public float StrafeSwingThreshold { get; }

        public CameraStanceSettings(
            StanceFraming exploration,
            StanceFraming sprint,
            StanceFraming combat,
            StanceFraming aim,
            float transitionSpeed,
            float combatLingerSeconds,
            float shoulderSwapSpeed,
            float strafeSwingThreshold
        )
        {
            Exploration = exploration;
            Sprint = sprint;
            Combat = combat;
            Aim = aim;
            TransitionSpeed = transitionSpeed;
            CombatLingerSeconds = combatLingerSeconds;
            ShoulderSwapSpeed = shoulderSwapSpeed;
            StrafeSwingThreshold = strafeSwingThreshold;
        }
    }
}
