using UnityEngine;

namespace TDOM.Gameplay.Camera
{
    public sealed class CameraStanceResolver
    {
        public const float LadoIzquierdo = 0f;
        public const float LadoDerecho = 1f;
        private const float MovimientoMinimoCuadrado = 0.0001f;

        private readonly CameraStanceSettings _ajustes;
        private float _tiempoDesdeCombate;
        private float _ladoObjetivo = LadoDerecho;

        public CameraStance Stance { get; private set; } = CameraStance.Exploration;
        public StanceFraming Framing { get; private set; }
        public float CameraSide { get; private set; } = LadoDerecho;
        public bool EnCombate => _tiempoDesdeCombate < _ajustes.CombatLingerSeconds;

        public CameraStanceResolver(CameraStanceSettings ajustes)
        {
            _ajustes = ajustes;
            _tiempoDesdeCombate = ajustes.CombatLingerSeconds;
            Framing = ajustes.Exploration;
        }

        public void Tick(bool apuntando, bool atacando, bool corriendo, float dt)
        {
            _tiempoDesdeCombate = atacando ? 0f : _tiempoDesdeCombate + dt;
            Stance = ResolverStance(apuntando, corriendo);
            Framing = StanceFraming.Lerp(
                Framing,
                EncuadreDe(Stance),
                FactorSuavizado(_ajustes.TransitionSpeed, dt)
            );

            if (!EnCombate)
                _ladoObjetivo = LadoDerecho;

            CameraSide = Mathf.Lerp(
                CameraSide,
                _ladoObjetivo,
                FactorSuavizado(_ajustes.ShoulderSwapSpeed, dt)
            );
        }

        public void RegistrarEsquiva(Vector2 movimientoRelativoACamara)
        {
            if (!EnCombate || movimientoRelativoACamara.sqrMagnitude < MovimientoMinimoCuadrado)
                return;

            float lateral = movimientoRelativoACamara.normalized.x;
            if (Mathf.Abs(lateral) < _ajustes.StrafeSwingThreshold)
                return;

            _ladoObjetivo = lateral < 0f ? LadoIzquierdo : LadoDerecho;
        }

        private CameraStance ResolverStance(bool apuntando, bool corriendo)
        {
            return (apuntando, EnCombate, corriendo) switch
            {
                (true, _, _) => CameraStance.Aim,
                (_, true, _) => CameraStance.Combat,
                (_, _, true) => CameraStance.Sprint,
                _ => CameraStance.Exploration,
            };
        }

        private StanceFraming EncuadreDe(CameraStance stance)
        {
            return stance switch
            {
                CameraStance.Aim => _ajustes.Aim,
                CameraStance.Combat => _ajustes.Combat,
                CameraStance.Sprint => _ajustes.Sprint,
                _ => _ajustes.Exploration,
            };
        }

        private static float FactorSuavizado(float velocidad, float dt)
        {
            return 1f - Mathf.Exp(-velocidad * dt);
        }
    }
}
