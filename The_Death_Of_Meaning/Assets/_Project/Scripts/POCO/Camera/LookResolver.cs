using UnityEngine;

namespace TDOM.Gameplay.Camera
{
    public sealed class LookResolver
    {
        private readonly float _sensibilidad;
        private readonly float _pitchMin;
        private readonly float _pitchMax;

        public float Yaw { get; private set; }
        public float Pitch { get; private set; }

        public Quaternion YawRotation => Quaternion.Euler(0f, Yaw, 0f);

        public LookResolver(
            float sensibilidad,
            float pitchMin = -85f,
            float pitchMax = 85f,
            float yawInicial = 0f
        )
        {
            _sensibilidad = sensibilidad;
            _pitchMin = pitchMin;
            _pitchMax = pitchMax;
            Yaw = yawInicial;
        }

        public void Tick(Vector2 look, float dt)
        {
            Yaw += look.x * _sensibilidad * dt;
            Yaw = Mathf.Repeat(Yaw, 360f);
            Pitch -= look.y * _sensibilidad * dt;
            Pitch = Mathf.Clamp(Pitch, _pitchMin, _pitchMax);
        }
    }
}
