using System.Collections.Generic;
using TDOM.Contracts;
using TDOM.Data;
using TDOM.Gameplay.Camera;
using TDOM.Gameplay.Core;
using TDOM.Gameplay.Locomotion;
using TDOM.Unity.Camera;
using TDOM.Unity.Combat;
using TDOM.Unity.Input;
using TDOM.Unity.Locomotion;
using TDOM.Unity.UI;
using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity.Player
{
    public class PlayerRoot : NetworkBehaviour
    {
        [SerializeField]
        private CharacterDefinition _definition;

        [SerializeField]
        private PlayerMotor _motor;
        private PlayerLocomotion _locomocion;

        [SerializeField]
        private PlayerCameraRig _camera;
        private LookResolver _look;
        private FacingResolver _facing;
        private LockOnResolver _fijacion;
        private readonly List<LockOnCandidate> _candidatosFijacion = new List<LockOnCandidate>();

        [SerializeField]
        private PlayerInputReader _inputReader;

        private AnchorLauncher _anchorLauncher;

        private PlayerCombat _combat;

        [SerializeField]
        private GrappleVisual _grappleVisual;

        [SerializeField]
        private float _sensitivity = 200f;

        private bool _ganchoActivoPrevio;

        [SerializeField]
        private float _alturaMinima = -30f;

        public NetworkVariable<float> Energia = new NetworkVariable<float>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );
        private EnergyPool _energyPool;

        [SerializeField]
        private GameObject _hudEnergiaPrefab;
        private GameObject _hudInstance;
        private FeedbackDirector _feedback;
        private CharacterIds _characterId = CharacterIds.None;
        private bool _initialSpawnApplied;
        private bool _originResetLogged;
        private Vector3 _respawnPosition;
        private Quaternion _respawnRotation;
        private Vector3 _initialSpawnPosition;
        private Vector3 _lastObservedPosition;
        public bool AtaqueActivo => MovimientoBloqueado || ModoPistola;
        private bool ModoPistola => _combat != null && _combat.DisparoActivo;
        private bool MovimientoBloqueado =>
            (_combat != null && _combat.BloqueaMovimiento)
            || (_anchorLauncher != null && _anchorLauncher.AnclaActiva);

        public CharacterIds CharacterId => _characterId;
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
            _anchorLauncher = GetComponentInChildren<AnchorLauncher>();
            _combat = GetComponentInChildren<PlayerCombat>();
            _look = new LookResolver(
                _sensitivity,
                _camera.Config.PitchMin,
                _camera.Config.PitchMax,
                transform.eulerAngles.y
            );
            _locomocion = new PlayerLocomotion(_definition);
            _facing = new FacingResolver(_definition.Movement.TurnSpeed, transform.eulerAngles.y);
            _fijacion = new LockOnResolver(CrearAjustesFijacion(_camera.Config));
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

            if (IsOwner)
            {
                _characterId = GetCharacterId();
                _inputReader.ActivarPersonaje(GetCharacterId());
                if (_hudEnergiaPrefab != null)
                {
                    _hudInstance = Instantiate(_hudEnergiaPrefab);
                    var hudComp = _hudInstance.GetComponent<PlayerEnergyHud>();
                    if (hudComp != null)
                    {
                        string etiqueta = _characterId switch
                        {
                            CharacterIds.Ayla => "Stamina",
                            CharacterIds.Zendre => "Maná",
                            _ => "Energy",
                        };
                        hudComp.Initialize(this, etiqueta, _definition?.Energy?.Max ?? 100f);
                    }
                }
                if (_camera != null)
                    _feedback = _camera.GetComponent<FeedbackDirector>();
            }
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
            _respawnPosition = position;
            _respawnRotation = rotation;
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
            if (_hudInstance != null)
                _hudInstance.SetActive(!activa);
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

        public void ActualizarRespawn(Vector3 position, Quaternion rotation)
        {
            _respawnPosition = position;
            _respawnRotation = rotation;
        }

        public void Respawn()
        {
            Respawn(_respawnPosition, _respawnRotation);
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
            ActualizarFijacion(input.LockOnPressed, dt);

            if (_fijacion.Activo)
            {
                _look.EstablecerOrientacion(_fijacion.Yaw, _fijacion.Pitch);
                return;
            }

            _look.Tick(input.Look, dt);
        }

        private void ActualizarFijacion(bool alternar, float dt)
        {
            _candidatosFijacion.Clear();
            foreach (var objetivo in LockOnTarget.Activos)
                _candidatosFijacion.Add(new LockOnCandidate(objetivo.Id, objetivo.PuntoDeMira));

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

        private void AlinearConCamaraAlAtacar(InputSnapshot input)
        {
            bool iniciaAtaque = input.AttackPressed || input.FirePressed;
            if (!_definition.Movement.SnapToCameraOnAttack || input.AimHeld || !iniciaAtaque)
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

        public override void OnNetworkDespawn()
        {
            if (_hudInstance != null)
            {
                Destroy(_hudInstance);
                _hudInstance = null;
            }
        }

        private void Update()
        {
            if (PauseManager.PausadoGlobal)
                return;

            if (IsServer && _energyPool != null)
            {
                _energyPool.Tick(Time.deltaTime, isDowned: false);
                Energia.Value = _energyPool.Current;
            }

            if (!IsOwner || !_initialSpawnApplied)
                return;

            float dt = Time.deltaTime;
            var input = _inputReader.Read();

            ActualizarMirada(input, dt);

            if (_camera != null)
                _camera.ActualizarEncuadre(
                    input.AimHeld,
                    AtaqueActivo || _fijacion.Activo,
                    _locomocion.Corriendo,
                    dt
                );

            _motor.ProbeGround(_locomocion.State);

            AlinearConCamaraAlAtacar(input);

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

            var intent = _locomocion.Tick(
                input,
                _look.YawRotation,
                dt,
                MovimientoBloqueado,
                transform.position,
                ModoPistola
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
            _facing.Tick(_locomocion.State.Velocity, _look.Yaw, input.AimHeld || ModoPistola, dt);
            transform.rotation = _facing.Rotacion;
            AplicarMiradaACamara();

            DispararFeedbackDeLocomocion();
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
    }
}
