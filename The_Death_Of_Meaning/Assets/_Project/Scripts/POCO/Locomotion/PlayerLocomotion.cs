using TDOM.Contracts;
using TDOM.Data;
using UnityEngine;

namespace TDOM.Gameplay.Locomotion
{
    public sealed class PlayerLocomotion
    {
        private readonly GravityModel _gravity;
        private readonly JumpResolver _jump;
        private readonly GroundControlResolver _ground;
        private readonly SprintResolver _run;
        private readonly DashResolver _dash;

        public LocomotionState State { get; } = new();

        public PlayerLocomotion(
            GravityModel gravity,
            JumpResolver jump,
            GroundControlResolver ground,
            SprintResolver run,
            DashResolver dash
        )
        {
            this._gravity = gravity;
            this._jump = jump;
            this._ground = ground;
            this._run = run;
            this._dash = dash;
        }

        public PlayerLocomotion(CharacterDefinition definition)
        {
            var m = definition.Movement;
            _gravity = new GravityModel(m.Gravity, m.TerminalVelocity, m.LowJumpMultiplier);
            _jump = new JumpResolver(m.MaxJumps, m.JumpVelocity, m.CoyoteTime, m.BufferTime);
            _ground = new GroundControlResolver(
                m.BaseSpeed,
                m.SprintSpeed,
                m.Acceleration,
                m.Friction,
                m.AirControl
            );
            _run = new SprintResolver();
            _dash = new DashResolver(definition.Dash);
        }

        private Vector3 DireccionDeDash(InputSnapshot input, Quaternion yaw)
        {
            Vector3 direction = yaw * new Vector3(input.Move.x, 0f, input.Move.y);
            return direction.normalized;
        }

        public MotionIntent Tick(InputSnapshot input, Quaternion yaw, float dt)
        {
            _run.Tick(input);
            if (input.DashPressed)
            {
                Vector3 dir = DireccionDeDash(input, yaw);
                _dash.TryIniciar(dir);
            }
            _dash.Tick(State, input.Move, dt);
            if (_dash.Activo)
                return new MotionIntent(State.Velocity, ignoreGravity: true);
            _jump.Tick(State, input, dt);
            _ground.Tick(State, input.Move, yaw, _run.Corriendo, dt);
            _gravity.Aplicar(State, input, dt);
            return new MotionIntent(State.Velocity, ignoreGravity: false);
        }
    }
}
