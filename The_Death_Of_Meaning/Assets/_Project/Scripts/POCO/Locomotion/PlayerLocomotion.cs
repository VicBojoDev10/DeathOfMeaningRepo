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
        private readonly float _multiplicadorVelocidadPistola;
        private readonly InputSnapshot _inputSinControl = new InputSnapshot();

        private const float VelocidadCompleta = 1f;

        public LocomotionState State { get; } = new();

        public const float UmbralAterrizajeFuerte = -12f;
        public bool DashIniciado { get; private set; }
        public bool SaltoAereo { get; private set; }
        public bool Aterrizaje { get; private set; }
        public float VelocidadImpacto { get; private set; }
        public float VelocidadMinimaAterrizaje { get; set; } = -UmbralAterrizajeFuerte;
        public bool SaltoEnCurso { get; private set; }
        public bool CayendoEnSalto { get; private set; }

        private bool _groundedPrevio = true;
        private float _velYPrevia;

        public bool Corriendo => _run != null && _run.Corriendo;
        public bool GanchoActivo => _grapple != null && _grapple.Activo;
        public bool DashActivo => _dash != null && _dash.Activo;
        public Vector3 PuntoGancho => _grapple != null ? _grapple.Punto : Vector3.zero;
        private float _escalaGravedadAtaqueAereo = 1f;
        public bool SaltoDisponible => _jump != null && _jump.PuedeSaltar(State);
        public bool DashDisponible => _dash != null && _dash.Disponible;
        public float CooldownDashNormalizado => _dash != null ? _dash.CooldownNormalizado : 0f;
        public float CooldownGanchoNormalizado =>
            _grapple != null ? _grapple.CooldownNormalizado : 0f;

        public PlayerLocomotion(
            GravityModel gravity,
            JumpResolver jump,
            GroundControlResolver ground,
            SprintResolver run,
            DashResolver dash,
            GrappleResolver grapple = null,
            float multiplicadorVelocidadPistola = VelocidadCompleta
        )
        {
            this._gravity = gravity;
            this._jump = jump;
            this._ground = ground;
            this._run = run;
            this._dash = dash;
            this._grapple = grapple;
            this._multiplicadorVelocidadPistola = multiplicadorVelocidadPistola;
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
            _escalaGravedadAtaqueAereo = m.AirAttackGravityScale;
            _dash = new DashResolver(definition.Dash);
            _grapple = definition.Grapple != null ? new GrappleResolver(definition.Grapple) : null;
            _multiplicadorVelocidadPistola = m.ShootingSpeedMultiplier;
        }

        private Vector3 DireccionDeDash(InputSnapshot input, Quaternion yaw)
        {
            if (input.Move.sqrMagnitude < 0.01f)
                return yaw * Vector3.forward;

            Vector3 direction = yaw * new Vector3(input.Move.x, 0f, input.Move.y);
            return direction.normalized;
        }

        public bool IntentarGancho(Vector3 origen, Vector3 punto, bool haciaJefe = false)
        {
            if (_grapple == null || State.Phase == LocomotionPhase.Attacking)
                return false;

            if (_grapple.TryIniciar(origen, punto, haciaJefe))
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
            Vector3 posicion = default,
            bool modoPistola = false,
            float multiplicadorAtaque = 1f,
            bool gravedadReducida = false
        )
        {
            DashIniciado = false;
            SaltoAereo = false;
            Aterrizaje = false;
            VelocidadImpacto = 0f;

            if (State.IsGrounded && !_groundedPrevio && -_velYPrevia >= VelocidadMinimaAterrizaje)
            {
                Aterrizaje = true;
                VelocidadImpacto = -_velYPrevia;
            }

            // Mantener el bool de salto sincronizado con el contacto real con el
            // suelo. El reset ocurre antes de procesar un posible salto bufferizado.
            if (State.IsGrounded && State.Velocity.y <= 0f)
                SaltoEnCurso = false;

            var intent = TickInterno(
                input,
                yaw,
                dt,
                blockMove,
                posicion,
                modoPistola,
                multiplicadorAtaque,
                gravedadReducida
            );

            CayendoEnSalto = SaltoEnCurso && State.Velocity.y <= 0f;

            _groundedPrevio = State.IsGrounded;
            _velYPrevia = State.Velocity.y;

            return intent;
        }

        private MotionIntent TickInterno(
            InputSnapshot input,
            Quaternion yaw,
            float dt,
            bool blockMove,
            Vector3 posicion,
            bool modoPistola,
            float multiplicadorAtaque,
            bool gravedadReducida
        )
        {
            ActualizarCarrera(input, modoPistola);

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

            bool puedeEmbestir = !blockMove && !modoPistola;
            if (puedeEmbestir && input.DashPressed)
            {
                Vector3 dir = DireccionDeDash(input, yaw);
                if (_dash.TryIniciar(dir))
                {
                    DashIniciado = true;
                    // En el suelo, el dash termina directamente en carrera (no vuelve a caminar).
                    // Si no hay direccion de movimiento, ActualizarCarrera lo apaga solo.
                    if (State.IsGrounded)
                        _run.Corriendo = true;
                }
            }

            Vector3 direccionDeseada = yaw * new Vector3(input.Move.x, 0f, input.Move.y);
            _dash.Tick(State, direccionDeseada, dt);

            if (_dash.Activo)
                return new MotionIntent(State.Velocity, ignoreGravity: true);

            float escalaGravedad = 1f;
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
                // Cola de la animacion de un ataque aereo: el movimiento ya no esta bloqueado
                // pero la gravedad sigue reducida hasta que la animacion termine.
                if (gravedadReducida && !State.IsGrounded)
                    escalaGravedad = _escalaGravedadAtaqueAereo;
                if (State.JumpsUsed > saltosAntes && State.Velocity.y > 0f)
                    SaltoEnCurso = true;
                if (State.JumpsUsed > saltosAntes && State.JumpsUsed > 1)
                    SaltoAereo = true;
                _ground.Tick(
                    State,
                    input.Move,
                    yaw,
                    _run.Corriendo && multiplicadorAtaque >= 1f,
                    dt,
                    MultiplicadorDeVelocidad(modoPistola) * multiplicadorAtaque
                );
            }
            else
            {
                bool yaAtacando = State.Phase == LocomotionPhase.Attacking;
                State.Phase = LocomotionPhase.Attacking;
                _ground.Tick(State, Vector2.zero, yaw, false, dt);

                // Ataque en el aire: al empezar se congela la caída/subida y luego
                // la gravedad actúa muy reducida mientras dure el combo.
                if (!State.IsGrounded)
                {
                    if (!yaAtacando)
                        State.Velocity.y = 0f;
                    escalaGravedad = _escalaGravedadAtaqueAereo;
                }
            }

            _gravity.Aplicar(State, input, dt, escalaGravedad);

            return new MotionIntent(State.Velocity, ignoreGravity: false);
        }

        private void ActualizarCarrera(InputSnapshot input, bool modoPistola)
        {
            if (modoPistola)
            {
                _run.Corriendo = false;
                return;
            }

            _run.Tick(input);
        }

        public MotionIntent TickIncapacitado(float dt)
        {
            _dash?.Cancelar();
            _grapple?.Cancelar();
            _run.Corriendo = false;
            State.Velocity = new Vector3(0f, Mathf.Min(0f, State.Velocity.y), 0f);
            State.BufferTimer = 0f;
            State.CoyoteTimer = 0f;
            State.Phase = State.IsGrounded ? LocomotionPhase.Grounded : LocomotionPhase.Airborne;
            SaltoEnCurso = false;
            return Tick(_inputSinControl, Quaternion.identity, dt);
        }

        public void Reiniciar()
        {
            State.Velocity = Vector3.zero;
            State.JumpsUsed = 0;
            State.CoyoteTimer = 0f;
            State.BufferTimer = 0f;

            _dash?.Cancelar();
            _grapple?.Cancelar();

            DashIniciado = false;
            SaltoAereo = false;
            Aterrizaje = false;
        }

        private float MultiplicadorDeVelocidad(bool modoPistola) =>
            modoPistola ? _multiplicadorVelocidadPistola : VelocidadCompleta;
    }
}
