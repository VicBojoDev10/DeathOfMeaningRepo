using UnityEngine;

namespace TDOM.Gameplay.Camera
{
    public sealed class CameraAutoRotationResolver
    {
        private readonly CameraAutoRotationSettings _ajustes;
        private float _tiempoSinMirar;
        private float _tiempoDesalineado;

        public CameraAutoRotationResolver(CameraAutoRotationSettings ajustes)
        {
            _ajustes = ajustes;
            _tiempoSinMirar = ajustes.LookCooldown;
        }

        public float Tick(
            float yawCamara,
            float yawPersonaje,
            float velocidadHorizontal,
            Vector2 look,
            bool apuntando,
            float dt
        )
        {
            _tiempoSinMirar = look.magnitude > _ajustes.LookDeadzone ? 0f : _tiempoSinMirar + dt;
            float diferencia = Mathf.DeltaAngle(yawCamara, yawPersonaje);

            if (!PuedeRotar(diferencia, velocidadHorizontal, apuntando))
            {
                _tiempoDesalineado = 0f;
                return 0f;
            }

            _tiempoDesalineado += dt;
            if (_tiempoDesalineado < _ajustes.GraceDelay)
                return 0f;

            return Paso(diferencia, dt);
        }

        private bool PuedeRotar(float diferencia, float velocidadHorizontal, bool apuntando)
        {
            float angulo = Mathf.Abs(diferencia);
            return !apuntando
                && velocidadHorizontal > _ajustes.MinSpeed
                && _tiempoSinMirar >= _ajustes.LookCooldown
                && angulo > _ajustes.MinAngle
                && angulo <= _ajustes.MaxAngle;
        }

        private float Paso(float diferencia, float dt)
        {
            float angulo = Mathf.Abs(diferencia);
            float suavizado = Mathf.Clamp01(angulo / _ajustes.EaseAngle);
            float paso = Mathf.Min(angulo, _ajustes.RotationSpeed * suavizado * dt);
            return Mathf.Sign(diferencia) * paso;
        }
    }
}
