using System;
using System.Collections.Generic;
using TDOM.Contracts;
using TDOM.Data;
using TDOM.Gameplay.Camera;
using TDOM.Gameplay.Core;
using TDOM.Gameplay.Locomotion;
using TDOM.Unity;
using TDOM.Unity.Camera;
using TDOM.Unity.Combat;
using TDOM.Unity.Input;
using TDOM.Unity.Level;
using TDOM.Unity.Locomotion;
using TDOM.Unity.UI;
using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity.Player
{
    public class PlayerRoot : NetworkBehaviour, IDamageable
    {
        public readonly struct CooldownValues
        {
            public float Dash { get; }
            public float Gancho { get; }
            public float Ancla { get; }

            public CooldownValues(float dash, float gancho, float ancla)
            {
                Dash = dash;
                Gancho = gancho;
                Ancla = ancla;
            }
        }

        [SerializeField]
        private CharacterDefinition _definition;

        [SerializeField]
        private PlayerMotor _motor;
        private PlayerLocomotion _locomocion;
        private ZendrePlayerLocomotionAnimator _zendreLocomotionAnimator;

        [SerializeField]
        private PlayerCameraRig _camera;
        private LookResolver _look;
        private FacingResolver _facing;
        private LockOnResolver _fijacion;
        private LockOnSwitcher _cambioDeFijacion;
        private readonly List<LockOnCandidate> _candidatosFijacion = new List<LockOnCandidate>();

        [SerializeField]
        private PlayerInputReader _inputReader;

        private AnchorLauncher _anchorLauncher;

        private PlayerCombat _combat;

        private AylaPlayerLocomotionAnimator _aylaAnimator;

        [SerializeField]
        private GrappleVisual _grappleVisual;

        [SerializeField]
        private float _sensitivity = 200f;

        private bool _ganchoActivoPrevio;

        private BossController _jefe;

        [SerializeField]
        private float _alturaMaxima = 80f;
        private bool _respawnPorAlturaSolicitado;

        public NetworkVariable<float> Energia = new NetworkVariable<float>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );
        private EnergyPool _energyPool;

        public NetworkVariable<float> Vida = new NetworkVariable<float>(
            100f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<PlayerLifeState> EstadoVida = new NetworkVariable<PlayerLifeState>(
            PlayerLifeState.Vivo,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<float> TiempoAbatidoRestante = new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<float> ProgresoRevivir = new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private HealthPool _healthPool;
        private readonly List<HealthPool> _vidasJugadores = new List<HealthPool>();

        private FeedbackDirector _feedback;
        private CharacterIds _characterId = CharacterIds.None;
        private bool _initialSpawnApplied;
        private bool _originResetLogged;
        private Vector3 _initialSpawnPosition;
        private Vector3 _lastObservedPosition;
        public bool AtaqueActivo => MovimientoBloqueado || ModoPistola;
        private bool ModoPistola => _combat != null && _combat.DisparoActivo;
        private bool PuedeApuntar => _definition != null && _definition.Ranged != null;
        private bool MovimientoBloqueado =>
            (_combat != null && _combat.BloqueaMovimiento)
            || (_anchorLauncher != null && _anchorLauncher.AnclaActiva);

        public CharacterIds CharacterId => _characterId;
        public float EnergiaMaxima => _definition?.Energy != null ? _definition.Energy.Max : 0f;
        public float VidaMaxima => _definition.MaxHealth;
        public float DuracionAbatido => _definition.DownedDuration;
        public float VidaAlRevivir => _definition.ReviveHealth;
        public float DuracionRevivir => _definition.ReviveDuration;
        public float RadioRevivir => _definition.ReviveRadius;
        public float TiempoAbatidoNormalizado =>
            DuracionAbatido > 0f
                ? Mathf.Clamp01(TiempoAbatidoRestante.Value / DuracionAbatido)
                : 0f;
        public bool EstaAbatidoOMuerto =>
            EstadoVida.Value == PlayerLifeState.Abatido
            || EstadoVida.Value == PlayerLifeState.Muerto;
        public event Action DashEmbestidaIniciada;
        public CooldownValues Cooldowns =>
            new CooldownValues(
                _locomocion != null ? _locomocion.CooldownDashNormalizado : 0f,
                _locomocion != null ? _locomocion.CooldownGanchoNormalizado : 0f,
                _anchorLauncher != null ? _anchorLauncher.CooldownNormalizado : 0f
            );
        public bool SpawnInicialAplicado => _initialSpawnApplied;
        public Transform CameraTransform => _camera != null ? _camera.CamaraTransform : null;
        public PlayerInputReader InputReader => _inputReader;
        public float GrappleRange => _definition?.Grapple != null ? _definition.Grapple.Range : 30f;
        public float AnchorRange =>
            _anchorLauncher != null && _anchorLauncher.Perfil != null
                ? _anchorLauncher.Perfil.Range
                : 20f;

        public override void OnNetworkSpawn()
        {
            _zendreLocomotionAnimator = GetComponent<ZendrePlayerLocomotionAnimator>();
            _anchorLauncher = GetComponentInChildren<AnchorLauncher>();
            _combat = GetComponentInChildren<PlayerCombat>();
            _aylaAnimator = GetComponentInChildren<AylaPlayerLocomotionAnimator>();
            _look = new LookResolver(
                _sensitivity,
                _camera.Config.PitchMin,
                _camera.Config.PitchMax,
                transform.eulerAngles.y
            );
            _locomocion = new PlayerLocomotion(_definition);
            _facing = new FacingResolver(_definition.Movement.TurnSpeed, transform.eulerAngles.y);
            _fijacion = new LockOnResolver(CrearAjustesFijacion(_camera.Config));
            _cambioDeFijacion = new LockOnSwitcher(CrearAjustesCambioDeFijacion(_camera.Config));
            _camera.gameObject.SetActive(IsOwner);
            _inputReader.enabled = false;

            if (_grappleVisual == null)
                _grappleVisual = GetComponentInChildren<GrappleVisual>();

            if (IsServer && _definition != null && _definition.Energy != null)
            {
                _energyPool = new EnergyPool(
                    _definition.Energy.Max,
                    _definition.Energy.RegenPerSecond,
                    _definition.Energy.DrainPerSecondDowned
                );
                Energia.Value = _energyPool.Current;
            }

            if (IsServer)
            {
                _healthPool = new HealthPool(VidaMaxima, DuracionAbatido);
                SincronizarVidaEnRed();
                BossController.ObservarJefe(NetworkManager, OnBossSpawned);
            }

            EstadoVida.OnValueChanged += OnEstadoVidaChanged;
            AplicarEstadoVida();

            _characterId = GetCharacterId();

            if (IsOwner)
            {
                _inputReader.ActivarPersonaje(_characterId);
                if (_camera != null)
                    _feedback = _camera.GetComponent<FeedbackDirector>();
            }
        }

        private void OnBossSpawned(BossController jefe)
        {
            _jefe = jefe;
        }

        private void OnEstadoVidaChanged(PlayerLifeState anterior, PlayerLifeState actual)
        {
            AplicarEstadoVida();
        }

        private void AplicarEstadoVida()
        {
            _combat?.EstablecerIncapacitado(EstaAbatidoOMuerto);
            _anchorLauncher?.EstablecerIncapacitado(EstaAbatidoOMuerto);
        }

        public override void OnNetworkDespawn()
        {
            EstadoVida.OnValueChanged -= OnEstadoVidaChanged;
            _healthPool = null;
            _vidasJugadores.Clear();
            if (IsServer)
            {
                BossController.DejarDeObservarJefe(OnBossSpawned);
                _jefe = null;
            }
        }

        public override void OnDestroy()
        {
            EstadoVida.OnValueChanged -= OnEstadoVidaChanged;
            if (IsServer)
            {
                BossController.DejarDeObservarJefe(OnBossSpawned);
                _jefe = null;
            }
            base.OnDestroy();
        }

        public void EnviarSpawnInicial(Vector3 position, Quaternion rotation)
        {
            if (IsServer)
                AplicarSpawnInicialRpc(position, rotation);
        }

        [Rpc(SendTo.Owner)]
        private void AplicarSpawnInicialRpc(Vector3 position, Quaternion rotation)
        {
            if (!IsOwner)
                return;

            var characterController = GetComponent<CharacterController>();
            bool controllerWasEnabled = characterController != null && characterController.enabled;
            if (controllerWasEnabled)
                characterController.enabled = false;

            try
            {
                var networkTransform = GetComponent<ClientNetworkTransform>();
                if (networkTransform != null)
                {
                    networkTransform.HabilitarAutoridadDelDueño();
                    networkTransform.Teleport(position, rotation, transform.localScale);
                }
                else
                {
                    transform.SetPositionAndRotation(position, rotation);
                }
            }
            finally
            {
                if (controllerWasEnabled)
                    characterController.enabled = true;
            }

            _look = new LookResolver(_sensitivity, yawInicial: transform.eulerAngles.y);
            _initialSpawnApplied = true;
            _inputReader.enabled = true;
            _initialSpawnPosition = transform.position;
            _lastObservedPosition = transform.position;
            Debug.Log(
                $"[SPAWN][OWNER] client={NetworkManager.LocalClientId} "
                    + $"position={transform.position} rotation={transform.eulerAngles}"
            );
        }

        public void SetControlCinematica(bool activa)
        {
            if (!IsOwner)
                return;

            _inputReader.SetEntradaHabilitada(!activa);
        }

        public AimSolution ResolverApuntado(Vector3 origen) =>
            _camera.ResolverApuntado(origen, out _);

        public AimSolution ResolverApuntado(Vector3 origen, out Collider colliderImpactado) =>
            _camera.ResolverApuntado(origen, out colliderImpactado);

        public void RecibirGolpe()
        {
            if (IsOwner && _feedback != null)
                _feedback.OnGolpeRecibido();
        }

        public void Respawn(Vector3 position, Quaternion rotation)
        {
            if (!IsOwner)
                return;

            _locomocion?.Reiniciar();

            var characterController = GetComponent<CharacterController>();

            bool controllerWasEnabled = characterController != null && characterController.enabled;

            if (controllerWasEnabled)
                characterController.enabled = false;

            try
            {
                var networkTransform = GetComponent<ClientNetworkTransform>();

                if (networkTransform != null)
                {
                    networkTransform.HabilitarAutoridadDelDueño();
                    networkTransform.Teleport(position, rotation, transform.localScale);
                }
                else
                {
                    transform.SetPositionAndRotation(position, rotation);
                }
            }
            finally
            {
                if (controllerWasEnabled)
                    characterController.enabled = true;
            }
        }

        public void SetCamaraCedida(bool cedida)
        {
            if (IsOwner && _camera != null)
                _camera.CederPrioridad(cedida);
        }

        private CharacterIds GetCharacterId()
        {
            if (_definition == null || string.IsNullOrEmpty(_definition.DisplayName))
                return CharacterIds.None;
            if (_definition.DisplayName.Equals("Ayla", System.StringComparison.OrdinalIgnoreCase))
                return CharacterIds.Ayla;
            if (_definition.DisplayName.Equals("Zendre", System.StringComparison.OrdinalIgnoreCase))
                return CharacterIds.Zendre;
            return CharacterIds.None;
        }

        // TW-116: el jefe lo llama en el servidor al entrar a una fase nueva.
        public void RestaurarEnergiaPorCambioDeFase()
        {
            if (!IsServer || _energyPool == null)
                return;

            _energyPool.RestoreMaxOnPhaseChange();
            Energia.Value = _energyPool.Current;
        }

        // TW-116: sacudida local por el cambio de fase del jefe.
        public void SacudidaCambioDeFase()
        {
            if (IsOwner && _feedback != null)
                _feedback.OnCambioDeFase();
        }

        public void ConsumirEnergiaEnServidor(float cantidad)
        {
            if (!IsServer || _energyPool == null)
                return;
            if (_energyPool.TryConsume(cantidad))
            {
                Energia.Value = _energyPool.Current;
            }
        }

        private void DispararFeedbackDeLocomocion()
        {
            if (_feedback == null || _locomocion == null)
                return;

            if (_locomocion.DashIniciado)
            {
                switch (_characterId)
                {
                    case CharacterIds.Ayla:
                        _feedback.OnDashAyla();
                        break;
                    case CharacterIds.Zendre:
                        _feedback.OnEmbestidaZendre();
                        break;
                }
            }
            if (_locomocion.SaltoAereo)
                _feedback.OnDobleSalto();
            if (_locomocion.Aterrizaje)
                _feedback.OnAterrizajeFuerte();
        }

        private void AplicarMiradaACamara()
        {
            if (_camera != null)
                _camera.ApplyLook(_look.Yaw, _look.Pitch);
        }

        private void ActualizarMirada(InputSnapshot input, float dt)
        {
            ActualizarFijacion(input.LockOnPressed, input.Look, dt);

            if (_fijacion.Activo)
            {
                _look.EstablecerOrientacion(_fijacion.Yaw, _fijacion.Pitch);
                return;
            }

            _look.Tick(input.Look, dt);
        }

        private void ActualizarFijacion(bool alternar, Vector2 stick, float dt)
        {
            _candidatosFijacion.Clear();
            foreach (var objetivo in LockOnTarget.Activos)
                _candidatosFijacion.Add(new LockOnCandidate(objetivo.Id, objetivo.PuntoDeMira));

            if (
                _fijacion.Activo
                && _cambioDeFijacion.TryCambiar(
                    stick,
                    _camera.transform.position,
                    _fijacion.PuntoObjetivo,
                    _fijacion.ObjetivoId,
                    _candidatosFijacion,
                    out LockOnCandidate siguiente
                )
            )
                _fijacion.FijarA(siguiente);

            _fijacion.Tick(
                alternar,
                _camera.transform.position,
                _camera.CamaraTransform.forward,
                _look.Yaw,
                _look.Pitch,
                _candidatosFijacion,
                dt
            );
        }

        private static LockOnSettings CrearAjustesFijacion(ThirdPersonCameraConfig config)
        {
            return new LockOnSettings(
                config.LockOnRange,
                config.LockOnSmoothTime,
                config.LockOnPitchMin,
                config.LockOnPitchMax
            );
        }

        private static LockOnSwitchSettings CrearAjustesCambioDeFijacion(
            ThirdPersonCameraConfig config
        )
        {
            return new LockOnSwitchSettings(
                config.LockOnRange,
                config.LockOnSwitchThreshold,
                config.LockOnSwitchResetThreshold,
                config.LockOnSwitchMaxAngle
            );
        }

        private void AlinearConCamaraAlAtacar(InputSnapshot input, bool apuntando)
        {
            bool iniciaAtaque = input.AttackPressed || input.FirePressed;
            if (!_definition.Movement.SnapToCameraOnAttack || apuntando || !iniciaAtaque)
                return;

            float yawObjetivo = _fijacion.Activo
                ? _fijacion.YawHacia(transform.position)
                : _look.Yaw;
            _facing.AlinearCon(yawObjetivo);
            transform.rotation = _facing.Rotacion;
        }

        private void RegistrarDesplazamientoLateralEnCamara(Vector2 movimiento)
        {
            if (_camera == null || !_locomocion.DashIniciado)
                return;

            _camera.RegistrarDesplazamientoLateral(movimiento);
        }

        private void IntentarGanchoApuntado()
        {
            var apuntado = ResolverApuntado(transform.position, out Collider impactado);
            if (!apuntado.HasHit || impactado.GetComponentInParent<PlayerRoot>() != null)
                return;

            if (_locomocion.IntentarGancho(transform.position, apuntado.TargetPoint))
                _camera.PunchFov(8f, 0.2f);
        }

        private void Update()
        {
            if (
                PauseManager.PausadoGlobal
                || (PauseManager.ResultadoGlobal != ResultadoPartida.EnCurso)
            )
                return;

            float dt = Time.deltaTime;

            if (IsServer && _energyPool != null)
            {
                _energyPool.Tick(dt, isDowned: EstadoVida.Value == PlayerLifeState.Abatido);
                Energia.Value = _energyPool.Current;
            }

            if (IsServer && _healthPool != null)
            {
                if (EstadoVida.Value == PlayerLifeState.Abatido)
                    ActualizarEstadoAbatidoEnServidor(dt);
            }

            if (!IsOwner || !_initialSpawnApplied)
                return;

            var input = _inputReader.Read();
            bool apuntando = input.AimHeld && PuedeApuntar && !EstaAbatidoOMuerto;
            ActualizarMirada(input, dt);
            _zendreLocomotionAnimator?.ActualizarApuntado(apuntando, _fijacion.Activo, input.Move);
            if (_aylaAnimator != null)
                _aylaAnimator.SetAimState(_fijacion != null && _fijacion.Activo);

            if (_camera != null)
                _camera.ActualizarEncuadre(
                    apuntando,
                    (AtaqueActivo || _fijacion.Activo) && !EstaAbatidoOMuerto,
                    _locomocion.Corriendo && !EstaAbatidoOMuerto,
                    dt
                );

            _motor.ProbeGround(_locomocion.State);

            if (!EstaAbatidoOMuerto)
            {
                AlinearConCamaraAlAtacar(input, apuntando);

                // Dash cancela el combo en cualquier punto (solo si el dash está disponible).
                if (
                    _combat != null
                    && input.DashPressed
                    && _combat.PuedeCancelarConDash
                    && _locomocion.DashDisponible
                    && !(_anchorLauncher != null && _anchorLauncher.AnclaActiva)
                )
                    _combat.CancelarMeleePorDash();

                // Salto también cancela el combo (si hay salto disponible).
                if (
                    _combat != null
                    && input.JumpPressed
                    && _combat.PuedeCancelarConDash
                    && _locomocion.SaltoDisponible
                    && !(_anchorLauncher != null && _anchorLauncher.AnclaActiva)
                )
                    _combat.CancelarMeleePorSalto();

                if (_combat != null)
                    _combat.Tick(input, dt);

                if (
                    input.GrapplePressed
                    && !AtaqueActivo
                    && _definition != null
                    && _definition.Grapple != null
                    && _camera != null
                )
                    IntentarGanchoApuntado();
            }

            var intent = EstaAbatidoOMuerto
                ? _locomocion.TickIncapacitado(dt)
                : _locomocion.Tick(
                    input,
                    _look.YawRotation,
                    dt,
                    MovimientoBloqueado,
                    transform.position,
                    ModoPistola
                );
            if (_aylaAnimator != null)
                _aylaAnimator.SetSprintState(_locomocion.Corriendo);
            if (_characterId == CharacterIds.Zendre)
                _zendreLocomotionAnimator?.ActualizarSalto(
                    _locomocion.SaltoEnCurso,
                    _locomocion.CayendoEnSalto
                );
            if (
                _anchorLauncher != null
                && _anchorLauncher.AnclaActiva
                && _anchorLauncher.AnclaActual != null
                && _anchorLauncher.AnclaActual.Pegada.Value
            )
            {
                intent = new MotionIntent(Vector3.zero, ignoreGravity: true);
            }
            _motor.Apply(intent, dt);
            _facing.Tick(_locomocion.State.Velocity, _look.Yaw, apuntando || ModoPistola, dt);
            transform.rotation = _facing.Rotacion;
            AplicarMiradaACamara();

            DispararFeedbackDeLocomocion();
            if (_locomocion.DashIniciado)
                DashEmbestidaIniciada?.Invoke();
            RegistrarDesplazamientoLateralEnCamara(input.Move);

            bool ganchoActivoActual = _locomocion != null && _locomocion.GanchoActivo;
            if (ganchoActivoActual != _ganchoActivoPrevio)
            {
                _ganchoActivoPrevio = ganchoActivoActual;
                if (_grappleVisual != null)
                {
                    if (ganchoActivoActual)
                        _grappleVisual.Mostrar(_locomocion.PuntoGancho);
                    else
                        _grappleVisual.Ocultar();
                }
            }
        }

        private void LateUpdate()
        {
            if (!IsOwner || !_initialSpawnApplied)
                return;

            if (transform.position.y > _alturaMaxima)
            {
                if (!_respawnPorAlturaSolicitado)
                {
                    var killPlane = FindAnyObjectByType<KillPlane>();
                    if (killPlane != null)
                        _respawnPorAlturaSolicitado = killPlane.RespawnJugador(this);
                }

                return;
            }

            _respawnPorAlturaSolicitado = false;

            Vector3 currentPosition = transform.position;
            Vector2 currentPlanarPosition = new Vector2(currentPosition.x, currentPosition.z);
            Vector2 initialPlanarPosition = new Vector2(
                _initialSpawnPosition.x,
                _initialSpawnPosition.z
            );
            if (
                !_originResetLogged
                && currentPlanarPosition.sqrMagnitude < 9f
                && initialPlanarPosition.sqrMagnitude > 100f
            )
            {
                _originResetLogged = true;
                Debug.LogError(
                    $"[SPAWN][ORIGIN-RESET] client={NetworkManager.LocalClientId} "
                        + $"owner={OwnerClientId} previous={_lastObservedPosition} "
                        + $"current={currentPosition} frame={Time.frameCount}"
                );
            }

            _lastObservedPosition = currentPosition;
        }

        public void RecibirDanio(float cantidad, bool esInstaKill = false)
        {
            if (!IsServer || _healthPool == null)
                return;

            if (esInstaKill)
            {
                _healthPool.TakeDamage(
                    _healthPool.Current > 0f ? _healthPool.Current : _healthPool.Max
                );
            }
            else
            {
                _healthPool.TakeDamage(cantidad);
            }

            SincronizarVidaEnRed();
            VerificarGameOverEnServidor();
        }

        public void AplicarDanioEnServidor(float cantidad, bool esInstaKill = false) =>
            RecibirDanio(cantidad, esInstaKill);

        private void ActualizarEstadoAbatidoEnServidor(float dt)
        {
            PlayerRoot companeroVivo = ObtenerCompaneroVivoCercano(RadioRevivir);
            _healthPool.TickDowned(dt, companeroVivo != null, DuracionRevivir, VidaAlRevivir);
            SincronizarVidaEnRed();

            if (_healthPool.State == PlayerLifeState.Muerto)
                VerificarGameOverEnServidor();
        }

        private void SincronizarVidaEnRed()
        {
            Vida.Value = _healthPool.Current;
            TiempoAbatidoRestante.Value = _healthPool.DownedRemainingTime;
            ProgresoRevivir.Value = _healthPool.ReviveProgress;
            EstadoVida.Value = _healthPool.State;
        }

        private PlayerRoot ObtenerCompaneroVivoCercano(float distanciaMaxima)
        {
            if (NetworkManager == null || NetworkManager.SpawnManager == null)
                return null;

            PlayerRoot companeroCercano = null;
            float menorDistancia = distanciaMaxima;

            foreach (var objeto in NetworkManager.SpawnManager.SpawnedObjectsList)
            {
                if (objeto == null || objeto == NetworkObject)
                    continue;

                if (objeto.TryGetComponent(out PlayerRoot otroJugador))
                {
                    if (
                        otroJugador.IsSpawned
                        && otroJugador.EstadoVida.Value == PlayerLifeState.Vivo
                    )
                    {
                        float d = Vector3.Distance(
                            transform.position,
                            otroJugador.transform.position
                        );
                        if (d <= menorDistancia)
                        {
                            menorDistancia = d;
                            companeroCercano = otroJugador;
                        }
                    }
                }
            }

            return companeroCercano;
        }

        private void VerificarGameOverEnServidor()
        {
            if (!IsServer)
                return;

            if (PauseManager.ResultadoGlobal != ResultadoPartida.EnCurso)
                return;

            if (NetworkManager == null || NetworkManager.SpawnManager == null)
                return;

            _vidasJugadores.Clear();
            foreach (var objeto in NetworkManager.SpawnManager.SpawnedObjectsList)
            {
                if (
                    objeto != null
                    && objeto.IsSpawned
                    && objeto.TryGetComponent(out PlayerRoot jugador)
                    && jugador._healthPool != null
                )
                {
                    _vidasJugadores.Add(jugador._healthPool);
                }
            }

            if (HealthPool.CheckGameOver(_vidasJugadores))
                DispararGameOverEnServidor();
        }

        private void DispararGameOverEnServidor()
        {
            if (!IsServer)
                return;

            int fase = _jefe != null ? _jefe.Fase.Value : 1;

            var sessionManager = ObtenerGameSessionManager();
            if (sessionManager != null)
            {
                sessionManager.FinalizarPartidaPorDerrotaDesdeServidor(fase);
                return;
            }

            foreach (var objeto in NetworkManager.SpawnManager.SpawnedObjectsList)
            {
                if (objeto != null && objeto.TryGetComponent(out PauseManager pauseManager))
                {
                    pauseManager.EstablecerResultadoDesdeServidor(ResultadoPartida.Derrota);
                    return;
                }
            }
        }

        private GameSessionManager ObtenerGameSessionManager()
        {
            if (NetworkManager != null && NetworkManager.SpawnManager != null)
            {
                foreach (var objeto in NetworkManager.SpawnManager.SpawnedObjectsList)
                {
                    if (objeto != null && objeto.TryGetComponent(out GameSessionManager manager))
                        return manager;
                }
            }

            return null;
        }
    }
}
