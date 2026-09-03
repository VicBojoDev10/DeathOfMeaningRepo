using TDOM.Contracts;
using UnityEngine;

namespace TDOM.Gameplay.Locomotion
{
    public sealed class GravityModel
    {
        private readonly float _gravedad;
        private readonly float _velocidadTerminal;
        private readonly float _multiplicadorSaltoCorto;

        public GravityModel(float gravedad, float velocidadTerminal, float multiplicadorSaltoCorto)
        {
            _gravedad = gravedad;
            _velocidadTerminal = velocidadTerminal;
            _multiplicadorSaltoCorto = multiplicadorSaltoCorto;
        }

        public void Aplicar(LocomotionState estado, InputSnapshot input, float dt)
        {
            if (estado.IsGrounded && estado.Velocity.y < 0f)
            {
                estado.Velocity.y = -2f;
                return;
            }

            float g = _gravedad;

            if (estado.Velocity.y > 0f && !input.JumpHeld)
            {
                g *= _multiplicadorSaltoCorto;
            }

            estado.Velocity.y += g * dt;
            estado.Velocity.y = Mathf.Max(estado.Velocity.y, _velocidadTerminal);
        }
    }
}
