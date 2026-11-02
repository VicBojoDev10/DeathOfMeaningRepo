using UnityEngine;

namespace TDOM.Gameplay.Camera
{
    public sealed class LookResolver
    {
        private float _sensibilidadX;
        private float _sensibilidadY;
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
            : this(sensibilidad, sensibilidad, pitchMin, pitchMax, yawInicial) { }

        public static LookResolver CrearConSensibilidades(
            float sensibilidadX,
            float sensibilidadY,
            float pitchMin = -85f,
            float pitchMax = 85f,
            float yawInicial = 0f
        )
        {
            return new LookResolver(sensibilidadX, sensibilidadY, pitchMin, pitchMax, yawInicial);
        }

        private LookResolver(
            float sensibilidadX,
            float sensibilidadY,
            float pitchMin,
            float pitchMax,
            float yawInicial
        )
        {
            EstablecerSensibilidad(sensibilidadX, sensibilidadY);
            _pitchMin = pitchMin;
            _pitchMax = pitchMax;
            Yaw = yawInicial;
        }

        public void EstablecerSensibilidad(float sensibilidadX, float sensibilidadY)
        {
            _sensibilidadX = Mathf.Max(0f, sensibilidadX);
            _sensibilidadY = Mathf.Max(0f, sensibilidadY);
        }

        public void EstablecerOrientacion(float yaw, float pitch)
        {
            Yaw = Mathf.Repeat(yaw, 360f);
            Pitch = pitch;
        }

        public void Tick(Vector2 look, float dt, float multiplicadorSensibilidad = 1f)
        {
            float multiplicador = Mathf.Max(0f, multiplicadorSensibilidad);
            Yaw += look.x * _sensibilidadX * multiplicador * dt;
            Yaw = Mathf.Repeat(Yaw, 360f);
            Pitch -= look.y * _sensibilidadY * multiplicador * dt;
            Pitch = Mathf.Clamp(Pitch, _pitchMin, _pitchMax);
        }
    }
}
