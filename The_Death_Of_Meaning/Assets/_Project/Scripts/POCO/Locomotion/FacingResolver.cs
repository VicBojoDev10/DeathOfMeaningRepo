using UnityEngine;

namespace TDOM.Gameplay.Locomotion
{
    public sealed class FacingResolver
    {
        public const float VelocidadMinimaParaGirar = 0.1f;

        private readonly float _velocidadGiroGradosPorSeg;

        public float YawActual { get; private set; }

        public Quaternion Rotacion => Quaternion.Euler(0f, YawActual, 0f);

        public FacingResolver(float velocidadGiroGradosPorSeg, float yawInicial = 0f)
        {
            _velocidadGiroGradosPorSeg = velocidadGiroGradosPorSeg;
            YawActual = yawInicial;
        }

        public float Tick(Vector3 velocidadHorizontal, float yawCamara, bool apuntando, float dt)
        {
            float objetivo = YawObjetivo(velocidadHorizontal, yawCamara, apuntando);
            YawActual = Mathf.MoveTowardsAngle(
                YawActual,
                objetivo,
                _velocidadGiroGradosPorSeg * dt
            );
            return YawActual;
        }

        public void AlinearCon(float yaw)
        {
            YawActual = yaw;
        }

        private float YawObjetivo(Vector3 velocidadHorizontal, float yawCamara, bool apuntando)
        {
            if (apuntando)
                return yawCamara;

            Vector2 plano = new Vector2(velocidadHorizontal.x, velocidadHorizontal.z);
            if (plano.magnitude <= VelocidadMinimaParaGirar)
                return YawActual;

            return Mathf.Atan2(plano.x, plano.y) * Mathf.Rad2Deg;
        }
    }
}
