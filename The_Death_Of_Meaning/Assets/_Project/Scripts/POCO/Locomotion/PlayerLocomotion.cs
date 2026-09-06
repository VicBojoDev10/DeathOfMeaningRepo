using UnityEngine;
using TDOM.Contracts;

namespace TDOM.Gameplay.Locomotion
{
    public sealed class PlayerLocomotion
    {
        private readonly GravityModel _gravedad;
        private readonly JumpResolver _salto;
        private readonly GroundControlResolver _suelo;
        private readonly SprintResolver _correr;
        private readonly DashResolver _dash;
        public LocomotionState State { get; } = new();
        public MotionIntent Tick(InputSnapshot input, Quaternion yaw, float dt)
        {
            _correr.Tick(input);
            if (input.DashPressed)
            {
                Vector3 dir = DireccionDeDash(input, yaw);
                _dash.TryIniciar(dir);
            }
            _dash.Tick(State, input.Move, dt);
            if (_dash.Activo)
                return new MotionIntent(State.Velocity, ignoreGravity: true);
            _salto.Tick(State, input, dt);
            _suelo.Tick(State, input.Move, yaw, _correr.Corriendo, dt);
            _gravedad.Aplicar(State, input, dt);
            return new MotionIntent(State.Velocity, ignoreGravity: false);
        }
    }
}
