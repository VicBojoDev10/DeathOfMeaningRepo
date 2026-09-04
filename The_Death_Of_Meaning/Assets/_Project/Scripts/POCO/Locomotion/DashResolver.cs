using TDOM.Contracts;
using TDOM.Data;
using TDOM.Gameplay.Core;
using UnityEngine;

namespace TDOM.Gameplay
{
    public class DashResolver
    {
        private readonly float _distancia;
        private readonly float _duracion;
        private readonly AnimationCurve _curva;
        private readonly float _giroMaximo;
        private readonly CooldownTimer _cooldown;
        private float _transcurrido;
        private Vector3 _direccion;
        public bool Activo { get; private set; }

        public DashResolver(DashProfile profile)
        {
            _distancia = profile.Distance;
            _duracion = profile.Duration;
            _curva = profile.Easing;
            _giroMaximo = profile.MaxTurnRate;
            _cooldown = new CooldownTimer(profile.Cooldown);
        }

        public bool TryIniciar(Vector3 direccion)
        {
            if (Activo || !_cooldown.Listo)
                return false;
            _direccion = direccion.normalized;
            _transcurrido = 0f;
            Activo = true;
            _cooldown.Disparar();
            return true;
        }

        public void Tick(LocomotionState estado, Vector2 move, float dt)
        {
            _cooldown.Tick(dt);
            if (!Activo)
                return;

            float tiempoRestante = _duracion - _transcurrido;
            float dtEfectivo = Mathf.Min(dt, tiempoRestante);

            if (_giroMaximo > 0f && move.sqrMagnitude > 0.01f)
            {
                float grados = _giroMaximo * dtEfectivo;
                _direccion = Vector3.RotateTowards(
                    _direccion,
                    new Vector3(move.x, 0f, move.y),
                    grados * Mathf.Deg2Rad,
                    0f
                );
            }

            float t = _transcurrido / _duracion;
            float velocidad = (_distancia / _duracion) * _curva.Evaluate(t);

            float factorCompensacion = dtEfectivo / dt;
            estado.Velocity = _direccion * (velocidad * factorCompensacion);
            estado.Velocity.y = 0f;
            estado.Phase = LocomotionPhase.Dashing;

            _transcurrido += dtEfectivo;
            if (_transcurrido >= _duracion)
            {
                Activo = false;
            }
        }

        public void Cancelar() => Activo = false;
    }
}
