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
        private readonly GrappleResolver _grapple;

        public LocomotionState State { get; } = new();

        public const float UmbralAterrizajeFuerte = -12f;
        public bool DashIniciado { get; private set; }
        public bool SaltoAereo { get; private set; }
        public bool Aterrizaje { get; private set; }

        private bool _groundedPrevio = true;
        private float _velYPrevia;

        public bool GanchoActivo => _grapple != null && _grapple.Activo;
        public Vector3 PuntoGancho => _grapple != null ? _grapple.Punto : Vector3.zero;

        public PlayerLocomotion(
            GravityModel gravity,
            JumpResolver jump,
            GroundControlResolver ground,
            SprintResolver run,
            DashResolver dash,
            GrappleResolver grapple = null
        )
        {
            this._gravity = gravity;
            this._jump = jump;
            this._ground = ground;
            this._run = run;
            this._dash = dash;
            this._grapple = grapple;
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
            _grapple = definition.Grapple != null ? new GrappleResolver(definition.Grapple) : null;
        }

        private Vector3 DireccionDeDash(InputSnapshot input, Quaternion yaw)
        {
            if (input.Move.sqrMagnitude < 0.01f)
                return yaw * Vector3.forward;

            Vector3 direction = yaw * new Vector3(input.Move.x, 0f, input.Move.y);
            return direction.normalized;
        }

        public bool IntentarGancho(Vector3 origen, Vector3 punto)
        {
            if (_grapple == null || State.Phase == LocomotionPhase.Attacking)
                return false;

            if (_grapple.TryIniciar(origen, punto))
            {
                _dash?.Cancelar();
                State.Phase = LocomotionPhase.Grappling;
                return true;
            }

            return false;
        }

        public MotionIntent Tick(
            InputSnapshot input,
            Quaternion yaw,
            float dt,
            bool blockMove = false,
            Vector3 posicion = default
        )
        {
            DashIniciado = false;
            SaltoAereo = false;
            Aterrizaje = false;

            if (State.IsGrounded && !_groundedPrevio && _velYPrevia < UmbralAterrizajeFuerte)
            {
                Aterrizaje = true;
            }

            var intent = TickInterno(input, yaw, dt, blockMove, posicion);

            _groundedPrevio = State.IsGrounded;
            _velYPrevia = State.Velocity.y;

            return intent;
        }

        private MotionIntent TickInterno(
            InputSnapshot input,
            Quaternion yaw,
            float dt,
            bool blockMove,
            Vector3 posicion
        )
        {
            _run.Tick(input);

            if (_grapple != null && _grapple.Activo)
            {
                if (input.JumpPressed)
                {
                    _grapple.Cancelar();
                    _grapple.Tick(State, posicion, dt);
                }
                else
                {
                    _grapple.Tick(State, posicion, dt);
                    if (_grapple.Activo)
                        return new MotionIntent(State.Velocity, ignoreGravity: true);
                }
            }
            else
            {
                _grapple?.Tick(State, posicion, dt);
            }

            if (!blockMove && input.DashPressed)
            {
                Vector3 dir = DireccionDeDash(input, yaw);
                if (_dash.TryIniciar(dir))
                {
                    DashIniciado = true;
                }
            }

            Vector3 direccionDeseada = yaw * new Vector3(input.Move.x, 0f, input.Move.y);
            _dash.Tick(State, direccionDeseada, dt);

            if (_dash.Activo)
                return new MotionIntent(State.Velocity, ignoreGravity: true);

            if (!blockMove)
            {
                if (
                    State.Phase == LocomotionPhase.Attacking
                    || State.Phase == LocomotionPhase.Grappling
                )
                {
                    State.Phase = State.IsGrounded
                        ? LocomotionPhase.Grounded
                        : LocomotionPhase.Airborne;
                }
                int saltosAntes = State.JumpsUsed;
                _jump.Tick(State, input, dt);
                if (State.JumpsUsed > saltosAntes && State.JumpsUsed > 1)
                    SaltoAereo = true;
                _ground.Tick(State, input.Move, yaw, _run.Corriendo, dt);
            }
            else
            {
                State.Phase = LocomotionPhase.Attacking;
                _ground.Tick(State, Vector2.zero, yaw, false, dt);
            }

            _gravity.Aplicar(State, input, dt);

            return new MotionIntent(State.Velocity, ignoreGravity: false);
        }
    }
}
