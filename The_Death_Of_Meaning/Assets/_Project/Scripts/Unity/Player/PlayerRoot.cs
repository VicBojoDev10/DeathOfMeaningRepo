using System;
using System.Collections;
using System.Collections.Generic;
using TDOM.Contracts;
using TDOM.Data;
using TDOM.Gameplay.Camera;
using TDOM.Gameplay.Core;
using TDOM.Gameplay.Locomotion;
using TDOM.Gameplay.Vfx;
using TDOM.Unity;
using TDOM.Unity.Camera;
using TDOM.Unity.Combat;
using TDOM.Unity.Input;
using TDOM.Unity.Level;
using TDOM.Unity.Locomotion;
using TDOM.Unity.Settings;
using TDOM.Unity.Vfx;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

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
        private readonly InputSnapshot _inputSinControlDuranteEscalada = new InputSnapshot();

        private AylaPlayerLocomotionAnimator _aylaAnimator;

        [SerializeField]
        private GrappleVisual _grappleVisual;

        [SerializeField]
        private VfxDash _vfxDash;

        [SerializeField]
        private VfxEmbestidaLanzador _vfxEmbestida;

        [SerializeField]
        private DestelloGolpe _destelloGolpe;

        [SerializeField]
        private VfxRed _vfxRed;

        [SerializeField]
        private VfxBase _vfxGancho;

        [SerializeField]
        private Transform _origenVfxGancho;

        [SerializeField]
        private VfxBase _vfxSalto;

        [SerializeField]
        private VfxBase _vfxAterrizaje;

        [SerializeField]
        private float _velocidadMinimaVfxAterrizaje = 6f;

        [SerializeField]
        private float _distanciaVfxAlSuelo = 2f;

        [SerializeField]
        private float _alturaRayoVfxSuelo = 1f;

        [SerializeField]
        private float _separacionVfxDelSuelo = 0.05f;

        private DetectorSaltoAterrizaje _detectorVfxSalto;

        [SerializeField]
        private VfxBase _vfxDobleSalto;

        [SerializeField]
        private Transform _origenVfxDobleSalto;

        [SerializeField]
        private float _sensitivity = 120f;

        private const float MultiplicadorSensibilidadGancho = 0.35f;
        private bool _ganchoActivoPrevio;
        private float _mantenerOrientacionGanchoHasta;
        private bool _hayYawObjetivoGancho;
        private float _yawObjetivoGancho;
        private float _velocidadYawGancho;

        private BossController _jefe;

        [SerializeField]
        private float _alturaMaxima = 80f;
        private bool _respawnPorAlturaSolicitado;
        private bool _respawnEnCurso;
        private GameObject _respawnFadeCanvas;
        private CanvasGroup _respawnFade;

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
        private ArenaEncounterNetwork _arenaEncounter;
        private CharacterIds _characterId = CharacterIds.None;
        private bool _initialSpawnApplied;
        private bool _originResetLogged;
        private Vector3 _initialSpawnPosition;
        private Vector3 _lastObservedPosition;
        public bool AtaqueActivo =>
            MovimientoBloqueado || ModoPistola || (_combat != null && _combat.AtaqueActivo);
        private bool ModoPistola => _combat != null && _combat.DisparoActivo;
        private bool PuedeApuntar => _definition != null && _definition.Ranged != null;

        // En el aire el combo congela la caida: solo en tierra deja caminar.
        private bool BloqueoPorAtaqueAereo =>
            _combat != null && _combat.MovimientoLimitado && !_locomocion.State.IsGrounded;

        private bool BloqueoAnimacionMovimiento =>
            (
                _characterId == CharacterIds.Zendre
                && _zendreLocomotionAnimator != null
                && _zendreLocomotionAnimator.AnimationMovementLocked
            )
            || (
                _characterId == CharacterIds.Ayla
                && _aylaAnimator != null
                && _aylaAnimator.AnimationMovementLocked
            );

        private bool MovimientoBloqueado =>
            (_combat != null && _combat.BloqueaMovimiento)
            || (_anchorLauncher != null && _anchorLauncher.AnclaActiva)
            || BloqueoAnimacionMovimiento;

        public CharacterIds CharacterId => _characterId;
        public float CostoEnergiaPendiente => _combat != null ? _combat.CostoCargaPendiente : 0f;

        /// <summary>
        /// Empujon corto hacia el frente con cada golpe de combo. El saltito vertical solo se
        /// da en el aire: en el suelo solo avanza.
        /// </summary>
        public void EmpujarPorCombo(float adelante, float arriba, float duracion)
        {
            if (_motor == null)
                return;

            bool enElAire = _locomocion != null && !_locomocion.State.IsGrounded;
            _motor.Empujar(transform.forward, adelante, enElAire ? arriba : 0f, duracion);
        }

        /// <summary>
        /// Solo servidor: empuja al jugador en la dirección horizontal dada. El motor lo mueve su
        /// dueño, así que el servidor se lo pide por RPC.
        /// </summary>
        public void EmpujarDesdeServidor(
            Vector3 direccion,
            float distancia,
            float altura,
            float duracion
        )
        {
            if (!IsServer)
                return;

            EmpujarRpc(direccion, distancia, altura, duracion);
        }

        [Rpc(SendTo.Owner)]
        private void EmpujarRpc(Vector3 direccion, float distancia, float altura, float duracion)
        {
            if (_motor != null)
                _motor.Empujar(direccion, distancia, altura, duracion);
        }

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
            _arenaEncounter = BuscarEncuentroArena();
            _zendreLocomotionAnimator = GetComponent<ZendrePlayerLocomotionAnimator>();
            _anchorLauncher = GetComponentInChildren<AnchorLauncher>();
            _combat = GetComponentInChildren<PlayerCombat>();
            _aylaAnimator = GetComponentInChildren<AylaPlayerLocomotionAnimator>();
            _look = CrearLookResolver(transform.eulerAngles.y);
            if (IsOwner)
                CameraSettingsService.SensibilidadCambiada += OnSensibilidadCambiada;
            _locomocion = new PlayerLocomotion(_definition);
            _facing = new FacingResolver(_definition.Movement.TurnSpeed, transform.eulerAngles.y);
            _fijacion = new LockOnResolver(CrearAjustesFijacion(_camera.Config));
            _cambioDeFijacion = new LockOnSwitcher(CrearAjustesCambioDeFijacion(_camera.Config));
            _camera.gameObject.SetActive(IsOwner);
            _inputReader.enabled = false;

            if (_grappleVisual == null)
                _grappleVisual = GetComponentInChildren<GrappleVisual>();

            if (_vfxDash == null)
                _vfxDash = GetComponentInChildren<VfxDash>();

            if (_vfxEmbestida == null)
                _vfxEmbestida = GetComponentInChildren<VfxEmbestidaLanzador>();

            if (_destelloGolpe == null)
                _destelloGolpe = GetComponentInChildren<DestelloGolpe>();

            if (_vfxRed == null)
                _vfxRed = GetComponentInChildren<VfxRed>();

            _detectorVfxSalto = new DetectorSaltoAterrizaje(_velocidadMinimaVfxAterrizaje);

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
            Vida.OnValueChanged += OnVidaChanged;
            AplicarEstadoVida();
            if (IsServer)
                BuscarEncuentroArena()?.NotificarEstadoVida(this, EstadoVida.Value);

            _characterId = GetCharacterId();

            if (IsOwner)
            {
                CrearOverlayRespawn();
                _inputReader.ActivarPersonaje(_characterId);
                if (_camera != null)
                    _feedback = _camera.GetComponent<FeedbackDirector>();
                if (_feedback != null && _feedback.Config != null)
                    _locomocion.VelocidadMinimaAterrizaje = _feedback
                        .Config
                        .aterrizajeVelocidadMinima;
            }
        }

        private void OnBossSpawned(BossController jefe)
        {
            _jefe = jefe;
        }

        private bool IntentarEscaladaEnBorde(InputSnapshot input)
        {
            if (
                _definition == null
                || !_definition.CanClimb
                || !_locomocion.PuedeEscalar
                || !input.JumpPressed
                || input.Move.sqrMagnitude <= 0f
                || AtaqueActivo
                || _locomocion.DashActivo
                || _locomocion.GanchoActivo
                || (_anchorLauncher != null && _anchorLauncher.AnclaActiva)
            )
                return false;

            CharacterController controller = _motor != null ? _motor.Controller : null;
            if (controller == null)
                return false;

            Vector3 direccion = _look.YawRotation * new Vector3(input.Move.x, 0f, input.Move.y);
            if (direccion.sqrMagnitude <= 0f)
                return false;

            float distanciaMaxima = _definition.Movement.ClimbApproachDistance;
            if (
                !Physics.Raycast(
                    controller.bounds.center,
                    direccion.normalized,
                    out RaycastHit hit,
                    distanciaMaxima,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore
                )
            )
                return false;

            LedgeClimbAnchor borde = hit.collider.GetComponentInParent<LedgeClimbAnchor>();
            if (borde == null || borde.TopPoint == null || borde.transform.IsChildOf(transform))
                return false;

            return _locomocion.IntentarEscalar(
                hit.distance,
                transform.position,
                borde.TopPoint.position,
                acercandose: true,
                bordeMarcado: true
            );
        }

        private void OnEstadoVidaChanged(PlayerLifeState anterior, PlayerLifeState actual)
        {
            AplicarEstadoVida();
            BuscarEncuentroArena()?.NotificarEstadoVida(this, actual);
        }

        private ArenaEncounterNetwork BuscarEncuentroArena()
        {
            if (_arenaEncounter != null && _arenaEncounter.IsSpawned)
                return _arenaEncounter;

            if (NetworkManager == null || NetworkManager.SpawnManager == null)
                return null;

            foreach (NetworkObject objeto in NetworkManager.SpawnManager.SpawnedObjectsList)
            {
                if (
                    objeto != null
                    && objeto.IsSpawned
                    && objeto.TryGetComponent(out ArenaEncounterNetwork encuentro)
                )
                {
                    _arenaEncounter = encuentro;
                    return _arenaEncounter;
                }
            }

            _arenaEncounter = null;
            return null;
        }

        private void OnVidaChanged(float anterior, float actual)
        {
            if (_destelloGolpe != null && CurvaDestelloGolpe.EsGolpe(anterior, actual))
                _destelloGolpe.Disparar();
        }

        private void AplicarEstadoVida()
        {
            _combat?.EstablecerIncapacitado(EstaAbatidoOMuerto);
            _anchorLauncher?.EstablecerIncapacitado(EstaAbatidoOMuerto);
        }

        public override void OnNetworkDespawn()
        {
            CameraSettingsService.SensibilidadCambiada -= OnSensibilidadCambiada;
            DestruirOverlayRespawn();
            EstadoVida.OnValueChanged -= OnEstadoVidaChanged;
            Vida.OnValueChanged -= OnVidaChanged;
            _arenaEncounter = null;
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
            CameraSettingsService.SensibilidadCambiada -= OnSensibilidadCambiada;
            DestruirOverlayRespawn();
            EstadoVida.OnValueChanged -= OnEstadoVidaChanged;
            Vida.OnValueChanged -= OnVidaChanged;
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

            _look = CrearLookResolver(transform.eulerAngles.y);
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

        public void RespawnConFundido(Vector3 position, Quaternion rotation)
        {
            if (!IsOwner || _respawnEnCurso)
                return;

            StartCoroutine(RespawnConFundidoRutina(position, rotation));
        }

        private IEnumerator RespawnConFundidoRutina(Vector3 position, Quaternion rotation)
        {
            _respawnEnCurso = true;
            yield return FundirOverlayRespawn(1f, 0.25f);
            Respawn(position, rotation);
            yield return new WaitForSecondsRealtime(0.1f);
            yield return FundirOverlayRespawn(0f, 0.35f);
            _respawnEnCurso = false;
        }

        private IEnumerator FundirOverlayRespawn(float alphaObjetivo, float duracion)
        {
            if (_respawnFade == null)
                yield break;

            float alphaInicial = _respawnFade.alpha;
            float transcurrido = 0f;
            while (transcurrido < duracion && _respawnFade != null)
            {
                transcurrido += Time.unscaledDeltaTime;
                _respawnFade.alpha = Mathf.Lerp(
                    alphaInicial,
                    alphaObjetivo,
                    transcurrido / duracion
                );
                yield return null;
            }

            if (_respawnFade != null)
                _respawnFade.alpha = alphaObjetivo;
        }

        private void CrearOverlayRespawn()
        {
            if (_respawnFadeCanvas != null)
                return;

            _respawnFadeCanvas = new GameObject("RespawnFadeLocal");
            var canvas = _respawnFadeCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;

            _respawnFade = _respawnFadeCanvas.AddComponent<CanvasGroup>();
            _respawnFade.alpha = 0f;
            _respawnFade.interactable = false;
            _respawnFade.blocksRaycasts = false;

            var imagenObjeto = new GameObject(
                "PantallaNegra",
                typeof(RectTransform),
                typeof(Image)
            );
            imagenObjeto.transform.SetParent(_respawnFadeCanvas.transform, false);
            var rect = imagenObjeto.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var imagen = imagenObjeto.GetComponent<Image>();
            imagen.color = Color.black;
            imagen.raycastTarget = false;
        }

        private void DestruirOverlayRespawn()
        {
            if (_respawnFadeCanvas != null)
                Destroy(_respawnFadeCanvas);

            _respawnFadeCanvas = null;
            _respawnFade = null;
        }

        public void Respawn(Vector3 position, Quaternion rotation)
        {
            if (!IsOwner)
                return;

            BuscarEncuentroArena()?.DesregistrarJugador(this);

            _zendreLocomotionAnimator?.RestablecerBloqueoDanio();

            _locomocion?.Reiniciar();
            _motor?.ResetPlataforma();

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
            {
                bool hayOtraAccionDeCamara =
                    _locomocion.DashActivo
                    || _locomocion.SaltoAereo
                    || _locomocion.GanchoActivo
                    || _ganchoActivoPrevio;
                _feedback.OnAterrizajeFuerte(_locomocion.VelocidadImpacto, hayOtraAccionDeCamara);
            }
        }

        private void CapturarContextoVfxDash(float dt)
        {
            if (_vfxDash == null)
                return;

            _vfxDash.Capturar(
                _locomocion.State.IsGrounded,
                _locomocion.Corriendo,
                _combat != null && (_combat.AtaqueActivo || _combat.PuedeCancelarConDash),
                _locomocion.GanchoActivo,
                dt
            );
        }

        private void LanzarVfxDash()
        {
            if (_vfxDash != null)
                _vfxDash.Lanzar(transform.position, _locomocion.State.Velocity, transform.rotation);
        }

        private void ActualizarVfxEmbestida()
        {
            if (_vfxEmbestida == null || _characterId != CharacterIds.Zendre)
                return;

            if (_locomocion.DashIniciado)
                _vfxEmbestida.Lanzar(
                    transform.position,
                    _locomocion.State.Velocity,
                    transform.rotation
                );

            _vfxEmbestida.Actualizar(
                _locomocion.DashActivo,
                transform.position,
                _locomocion.State.Velocity
            );
        }

        private void ActualizarVfxSaltoAterrizaje()
        {
            if (_detectorVfxSalto == null)
                return;

            _detectorVfxSalto.Actualizar(
                _locomocion.State.IsGrounded,
                _locomocion.SaltoEnCurso,
                _locomocion.SaltoAereo,
                _locomocion.State.Velocity.y
            );

            if (_detectorVfxSalto.SaltoDesdeSuelo)
                LanzarVfxEnPies(_vfxSalto);

            if (_detectorVfxSalto.Aterrizaje)
                LanzarVfxAterrizaje();
        }

        private void LanzarVfxAterrizaje()
        {
            if (_vfxRed == null || _vfxAterrizaje == null)
                return;

            _vfxRed.Lanzar(_vfxAterrizaje, BaseDelControlador(), transform.rotation);
        }

        private Vector3 BaseDelControlador()
        {
            CharacterController controlador = _motor != null ? _motor.Controller : null;
            if (controlador == null)
                return transform.position;

            Vector3 pies = transform.position;
            pies.y = controlador.bounds.min.y + _separacionVfxDelSuelo;
            return pies;
        }

        private void LanzarVfxEnPies(VfxBase efecto)
        {
            if (_vfxRed == null || efecto == null)
                return;

            if (!BuscarSueloBajoLosPies(out RaycastHit impacto))
            {
                _vfxRed.Lanzar(efecto, transform.position, transform.rotation);
                return;
            }

            Vector3 posicion = impacto.point + impacto.normal * _separacionVfxDelSuelo;
            Quaternion rotacion =
                Quaternion.FromToRotation(Vector3.up, impacto.normal) * transform.rotation;
            _vfxRed.Lanzar(efecto, posicion, rotacion);
        }

        private bool BuscarSueloBajoLosPies(out RaycastHit impacto)
        {
            Vector3 origen = transform.position + Vector3.up * _alturaRayoVfxSuelo;
            return Physics.Raycast(
                origen,
                Vector3.down,
                out impacto,
                _alturaRayoVfxSuelo + _distanciaVfxAlSuelo,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore
            );
        }

        private void LanzarVfxDobleSalto()
        {
            if (_vfxRed == null || _vfxDobleSalto == null)
                return;

            Transform origen = _origenVfxDobleSalto != null ? _origenVfxDobleSalto : transform;
            _vfxRed.Lanzar(_vfxDobleSalto, origen.position, transform.rotation);
        }

        private void AplicarMiradaACamara()
        {
            if (_camera != null)
                _camera.ApplyLook(_look.Yaw, _look.Pitch);
        }

        private void ActualizarMirada(InputSnapshot input, float dt)
        {
            bool ganchoActivo = _locomocion != null && _locomocion.GanchoActivo;
            if (ganchoActivo)
            {
                // La fijacion automatica puede arrastrar la camara mientras Ayla es jalada.
                // Durante el gancho se conserva solo el control horizontal y el pitch vuelve
                // gradualmente al horizonte para no dejar la vista apuntando al cielo.
                _fijacion.Liberar();
                ActualizarYawObjetivoGancho();
                _look.Tick(new Vector2(input.Look.x, 0f), dt, MultiplicadorSensibilidadGancho);
                _look.EstablecerOrientacion(
                    _hayYawObjetivoGancho
                        ? Mathf.SmoothDampAngle(
                            _look.Yaw,
                            _yawObjetivoGancho,
                            ref _velocidadYawGancho,
                            0.08f,
                            1080f,
                            dt
                        )
                        : _look.Yaw,
                    Mathf.MoveTowards(_look.Pitch, 0f, 180f * dt)
                );
                return;
            }

            _hayYawObjetivoGancho = false;

            if (_ganchoActivoPrevio || Time.time < _mantenerOrientacionGanchoHasta)
            {
                _look.EstablecerOrientacion(
                    _look.Yaw,
                    Mathf.MoveTowards(_look.Pitch, 0f, 180f * dt)
                );
                return;
            }

            ActualizarFijacion(input.LockOnPressed, input.Look, dt);

            if (_fijacion.Activo)
            {
                _look.EstablecerOrientacion(_fijacion.Yaw, _fijacion.Pitch);
                return;
            }

            _look.Tick(input.Look, dt);
        }

        private LookResolver CrearLookResolver(float yawInicial)
        {
            var config = _camera.Config;
            if (IsOwner)
            {
                Vector2 sensibilidad = new CameraSettingsService(config).Cargar();
                return LookResolver.CrearConSensibilidades(
                    sensibilidad.x,
                    sensibilidad.y,
                    config.PitchMin,
                    config.PitchMax,
                    yawInicial
                );
            }

            return new LookResolver(_sensitivity, config.PitchMin, config.PitchMax, yawInicial);
        }

        private void OnSensibilidadCambiada(float sensibilidadX, float sensibilidadY)
        {
            _look?.EstablecerSensibilidad(sensibilidadX, sensibilidadY);
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

            // Contra el jefe el gancho engancha pero corta el jalón antes de tocarlo.
            bool haciaJefe = impactado.GetComponentInParent<BossController>() != null;
            if (_locomocion.IntentarGancho(transform.position, apuntado.TargetPoint, haciaJefe))
            {
                ActualizarYawObjetivoGancho();
                _velocidadYawGancho = 0f;
                _fijacion.Liberar();
                _camera.PunchFov(8f, 0.2f);
                _feedback?.RegistrarAccionCamara(0.2f);
                LanzarVfxGancho();
            }
        }

        private void LanzarVfxGancho()
        {
            if (_vfxRed == null || _vfxGancho == null)
                return;

            Transform origen = _origenVfxGancho != null ? _origenVfxGancho : transform;
            _vfxRed.LanzarEntre(_vfxGancho, origen.position, _locomocion.PuntoGancho);
        }

        private void DetenerVfxGancho()
        {
            if (_vfxRed == null || _vfxGancho == null)
                return;

            _vfxRed.Detener(_vfxGancho);
        }

        private void ActualizarYawObjetivoGancho()
        {
            if (_locomocion == null || !_locomocion.GanchoActivo)
                return;

            // Reevaluar desde la posicion actual evita conservar un angulo obsoleto
            // cuando Ayla inicia el gancho de lado o mirando en sentido contrario.
            Vector3 haciaGancho = _locomocion.PuntoGancho - transform.position;
            haciaGancho.y = 0f;
            if (haciaGancho.sqrMagnitude <= 0.001f)
                return;

            _yawObjetivoGancho = Mathf.Atan2(haciaGancho.x, haciaGancho.z) * Mathf.Rad2Deg;
            _hayYawObjetivoGancho = true;
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

            if (BloqueoAnimacionMovimiento)
                _locomocion.CancelarDashEnCurso();

            var input = _inputReader.Read();
            bool apuntando =
                input.AimHeld && PuedeApuntar && !EstaAbatidoOMuerto && !_locomocion.ClimbActivo;
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
            CapturarContextoVfxDash(dt);

            if (!EstaAbatidoOMuerto)
            {
                if (!_locomocion.ClimbActivo && !BloqueoAnimacionMovimiento)
                {
                    AlinearConCamaraAlAtacar(input, apuntando);
                    IntentarEscaladaEnBorde(input);
                }

                if (!_locomocion.ClimbActivo && !BloqueoAnimacionMovimiento)
                {
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
                }

                if (_combat != null)
                    _combat.Tick(
                        _locomocion.ClimbActivo ? _inputSinControlDuranteEscalada : input,
                        dt
                    );

                if (
                    !_locomocion.ClimbActivo
                    && input.GrapplePressed
                    && !AtaqueActivo
                    && _definition != null
                    && _definition.Grapple != null
                    && _camera != null
                )
                    IntentarGanchoApuntado();
            }

            var intent =
                EstaAbatidoOMuerto || BloqueoAnimacionMovimiento
                    ? _locomocion.TickIncapacitado(dt)
                    : _locomocion.Tick(
                        input,
                        _look.YawRotation,
                        dt,
                        MovimientoBloqueado || BloqueoPorAtaqueAereo,
                        transform.position,
                        ModoPistola,
                        _combat != null ? _combat.MultiplicadorVelocidadAtaque : 1f,
                        _combat != null && _combat.GravedadReducidaPorAtaque
                    );
            bool ganchoActivoActual = _locomocion.GanchoActivo;
            if (_ganchoActivoPrevio && !ganchoActivoActual)
            {
                _mantenerOrientacionGanchoHasta = Time.time + 0.55f;
                DetenerVfxGancho();
            }
            if (_aylaAnimator != null)
            {
                _aylaAnimator.SetClimbingState(_locomocion.ClimbActivo);
                _aylaAnimator.SetSprintState(_locomocion.Corriendo);
                // La pose del gancho dura mientras el jalon siga activo y no se ataque.
                // Saltar cancela el gancho en la locomocion, asi que tambien la apaga.
                _aylaAnimator.SetGanchoState(_locomocion.GanchoActivo && !AtaqueActivo);
                // Pose del dash (y camara mas cerca) mientras dura el desplazamiento.
                _aylaAnimator.SetDashState(_locomocion.DashActivo);
            }
            _feedback?.ActualizarZoomDash(
                _locomocion.DashActivo,
                _characterId == CharacterIds.Zendre
            );
            if (_characterId == CharacterIds.Zendre)
                _zendreLocomotionAnimator?.ActualizarSalto(
                    _locomocion.SaltoEnCurso,
                    _locomocion.CayendoEnSalto
                );
            var plataformaAnclada =
                _anchorLauncher != null
                && _anchorLauncher.AnclaActiva
                && _anchorLauncher.AnclaActual != null
                && _anchorLauncher.AnclaActual.Pegada.Value
                    ? _anchorLauncher.AnclaActual.PlataformaAnclada
                    : null;
            if (
                _anchorLauncher != null
                && _anchorLauncher.AnclaActiva
                && _anchorLauncher.AnclaActual != null
                && _anchorLauncher.AnclaActual.Pegada.Value
            )
            {
                intent = new MotionIntent(Vector3.zero, ignoreGravity: true);
            }
            bool bloquearTodoMovimiento = BloqueoAnimacionMovimiento;
            if (bloquearTodoMovimiento)
            {
                _locomocion.CancelarDashEnCurso();
                _motor.CancelarEmpuje();
            }
            else
            {
                _motor.Apply(intent, dt, plataformaAnclada);
            }
            bool alineandoConGancho =
                ganchoActivoActual || Time.time < _mantenerOrientacionGanchoHasta;
            if (!bloquearTodoMovimiento)
            {
                _facing.Tick(
                    _locomocion.State.Velocity,
                    _look.Yaw,
                    apuntando
                        || ModoPistola
                        || alineandoConGancho
                        || (_combat != null && _combat.MovimientoLimitado),
                    dt,
                    alineandoConGancho ? 0.5f : 1f
                );
                transform.rotation = _facing.Rotacion;
            }
            AplicarMiradaACamara();

            DispararFeedbackDeLocomocion();
            ActualizarVfxSaltoAterrizaje();
            if (
                _locomocion.SaltoAereo
                && _characterId == CharacterIds.Ayla
                && _aylaAnimator != null
            )
                _aylaAnimator.IniciarDobleSalto();
            if (_locomocion.SaltoAereo && _characterId == CharacterIds.Ayla)
                LanzarVfxDobleSalto();
            if (_locomocion.DashIniciado)
            {
                if (_characterId == CharacterIds.Zendre)
                    _zendreLocomotionAnimator?.IniciarAssault();
                DashEmbestidaIniciada?.Invoke();
                LanzarVfxDash();
            }
            ActualizarVfxEmbestida();
            RegistrarDesplazamientoLateralEnCamara(input.Move);

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

        /// <summary>
        /// Solo servidor: si el jugador se beneficiaría de ese efecto ahora mismo. Curar: está
        /// vivo y con la vida incompleta. Los tickets de power-ups siguientes agregan casos.
        /// </summary>
        public bool NecesitaPowerUp(TipoEfectoPowerUp efecto)
        {
            if (!IsServer || _healthPool == null)
                return false;

            switch (efecto)
            {
                case TipoEfectoPowerUp.Curar:
                    return _healthPool.State == PlayerLifeState.Vivo
                        && _healthPool.Current < _healthPool.Max;
                default:
                    return false;
            }
        }

        /// <summary>Solo servidor: aplica el efecto del perfil. False si no lo necesitaba.</summary>
        public bool AplicarPowerUp(PowerUpProfile perfil)
        {
            if (perfil == null || !NecesitaPowerUp(perfil.Efecto))
                return false;

            switch (perfil.Efecto)
            {
                case TipoEfectoPowerUp.Curar:
                    _healthPool.Heal(perfil.Magnitud);
                    SincronizarVidaEnRed();
                    break;
                default:
                    return false;
            }

            PowerUpAplicadoRpc();
            return true;
        }

        [Rpc(SendTo.Owner)]
        private void PowerUpAplicadoRpc()
        {
            if (_feedback != null)
                _feedback.OnPowerUpRecibido();
        }

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
