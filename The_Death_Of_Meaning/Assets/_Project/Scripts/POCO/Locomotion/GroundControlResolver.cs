using TDOM.Contracts;
using UnityEngine;

namespace TDOM.Gameplay
{
    public sealed class GroundControlResolver
    {
        private readonly float _velocidadBase;
        private readonly float _velocidadCorrer;
        private readonly float _aceleracion;
        private readonly float _friccion;
        private readonly float _controlAereo;

        public GroundControlResolver(
            float velocidadBase,
            float velocidadCorrer,
            float aceleracion,
            float friccion,
            float controlAereo
        )
        {
            _velocidadBase = velocidadBase;
            _velocidadCorrer = velocidadCorrer;
            _aceleracion = aceleracion;
            _friccion = friccion;
            _controlAereo = controlAereo;
        }

        public void Tick(
            LocomotionState estado,
            Vector2 move,
            Quaternion yaw,
            bool corriendo,
            float dt
        )
        {
            Vector3 direction = yaw * new Vector3(move.x, 0f, move.y);

            float MaxVel = corriendo ? _velocidadCorrer : _velocidadBase;
            Vector3 objetivo = direction * MaxVel;
            float fuerzaActual = move.sqrMagnitude > 0.01f ? _aceleracion : _friccion;
            float accel = fuerzaActual * (estado.IsGrounded ? 1f : _controlAereo);

            Vector3 horizontal = new Vector3(estado.Velocity.x, 0f, estado.Velocity.z);
            horizontal = Vector3.MoveTowards(horizontal, objetivo, accel * dt);

            estado.Velocity.x = horizontal.x;
            estado.Velocity.z = horizontal.z;
        }
    }
}
