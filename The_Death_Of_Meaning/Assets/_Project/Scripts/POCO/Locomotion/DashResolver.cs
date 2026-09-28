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
        private readonly float _promedioCurva;
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

            if (_curva != null)
            {
                float suma = 0f;
                const int muestras = 100;
                for (int i = 0; i < muestras; i++)
                {
                    float t = (i + 0.5f) / muestras;
                    suma += _curva.Evaluate(t);
                }
                float promedio = suma / muestras;
                _promedioCurva = promedio <= 0.0001f ? 1f : promedio;
            }
            else
            {
                _promedioCurva = 1f;
            }
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

        public void Tick(LocomotionState estado, Vector3 direccionDeseada, float dt)
        {
            _cooldown.Tick(dt);
            if (!Activo)
                return;

            float tiempoRestante = _duracion - _transcurrido;
            float dtEfectivo = Mathf.Min(dt, tiempoRestante);

            if (_giroMaximo > 0f && direccionDeseada.sqrMagnitude > 0.01f)
            {
                float grados = _giroMaximo * dtEfectivo;
                _direccion = Vector3.RotateTowards(
                    _direccion,
                    direccionDeseada,
                    grados * Mathf.Deg2Rad,
                    0f
                );
            }

            float t = _transcurrido / _duracion;
            float evaluacion = _curva != null ? _curva.Evaluate(t) : 1f;
            float velocidad = (_distancia / _duracion) * evaluacion / _promedioCurva;

            float factorCompensacion = dtEfectivo / dt;
            estado.Velocity = _direccion * (velocidad * factorCompensacion);
            estado.Velocity.y = 0f;
            estado.Phase = LocomotionPhase.Dashing;

            _transcurrido += dtEfectivo;
            if (_transcurrido >= _duracion)
            {
                Activo = false;
                estado.Phase = estado.IsGrounded
                    ? LocomotionPhase.Grounded
                    : LocomotionPhase.Airborne;
            }
        }

        public void Cancelar() => Activo = false;
    }
}
