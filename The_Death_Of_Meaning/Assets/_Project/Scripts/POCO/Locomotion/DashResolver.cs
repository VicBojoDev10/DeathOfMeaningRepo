using UnityEngine;
using TDOM.Gameplay.Core;
using TDOM.Contracts;
namespace TDOM.Gameplay
{
    public class DashResolver
    {
        private readonly float _distancia;
        private readonly float _duracion;
        private readonly AnimationCurve _curva;
        private readonly float _giroMaximo;
        private readonly CooldownTimer _cooldown;
        private float   _transcurrido;
        private Vector3 _direccion;
        public bool Activo { get; private set; }
        public bool TryIniciar(Vector3 direccion)
        {
            if (Activo || !_cooldown.Listo) return false;
            _direccion    = direccion.normalized;
            _transcurrido = 0f;
            Activo        = true;
            _cooldown.Disparar();
            return true;
        }
        public void Tick(LocomotionState estado, Vector2 move, float dt)
        {
            _cooldown.Tick(dt);
            if (!Activo) return;
            _transcurrido += dt;
            if (_transcurrido >= _duracion)
            {
                Activo = false;
                return;
            }

            if (_giroMaximo > 0f && move.sqrMagnitude > 0.01f)
            {
                float grados = _giroMaximo * dt;
                _direccion = Vector3.RotateTowards(
                    _direccion, new Vector3(move.x, 0f, move.y),
                    grados * Mathf.Deg2Rad, 0f);
            }

            float t         = _transcurrido / _duracion;
            float velocidad = (_distancia / _duracion) * _curva.Evaluate(t);
            estado.Velocity   = _direccion * velocidad;
            estado.Velocity.y = 0f;
            estado.Phase      = LocomotionPhase.Dashing;
        }
        public void Cancelar() => Activo = false;
    }
}
