using TDOM.Contracts;
using UnityEngine;

namespace TDOM.Gameplay
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
            Debug.Log($"_gravedad: {g}");
            Debug.Log($"_velocidadTerminal{_velocidadTerminal}");
            Debug.Log($"_multiplicadorSaltoCorto{_multiplicadorSaltoCorto}");
            Debug.Log($"estado is grouded: {estado.IsGrounded.ToString()}");
            Debug.Log($"velocity: {estado.Velocity.ToString()}");
            Debug.Log($"input JumpPressed: {input.JumpPressed}");
            Debug.Log($"input JumpHeld: {input.JumpHeld}");

            if (estado.Velocity.y > 0f && !input.JumpHeld)
            {
                g *= _multiplicadorSaltoCorto;
                Debug.Log($"Entro al multiplicador{_multiplicadorSaltoCorto}");
            }

            estado.Velocity.y += g * dt;
            estado.Velocity.y = Mathf.Max(estado.Velocity.y, _velocidadTerminal);
        }
    }
}
