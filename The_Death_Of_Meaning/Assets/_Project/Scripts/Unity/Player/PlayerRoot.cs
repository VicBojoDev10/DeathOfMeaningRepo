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

        [SerializeField]
        private PlayerInputReader _inputReader;

        private AnchorLauncher _anchorLauncher;

        private PlayerCombat _combat;

        [SerializeField]
        private GrappleVisual _grappleVisual;

        [SerializeField]
        private float _sensitivity = 200f;

        private bool _ganchoActivoPrevio;

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
        public bool AtaqueActivo =>
            (_combat != null && _combat.AtaqueActivo)
            || (_anchorLauncher != null && _anchorLauncher.AnclaActiva);

        public CharacterIds CharacterId => _characterId;
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
            _camera.gameObject.SetActive(IsOwner);
            _inputReader.enabled = IsOwner;

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

        private void AlinearConCamaraAlAtacar(InputSnapshot input)
        {
            bool iniciaAtaque = input.AttackPressed || input.FirePressed;
            if (!_definition.Movement.SnapToCameraOnAttack || input.AimHeld || !iniciaAtaque)
                return;

            _facing.AlinearCon(_look.Yaw);
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
            if (IsServer && _energyPool != null)
            {
                _energyPool.Tick(Time.deltaTime, isDowned: false);
                Energia.Value = _energyPool.Current;
            }

            if (!IsOwner)
                return;

            float dt = Time.deltaTime;
            var input = _inputReader.Read();

            _look.Tick(input.Look, dt);

            if (_camera != null)
            {
                _camera.ApplyLook(_look.Yaw, _look.Pitch);
                _camera.ActualizarEncuadre(input.AimHeld, AtaqueActivo, _locomocion.Corriendo, dt);
            }

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
                AtaqueActivo,
                transform.position
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
            _facing.Tick(_locomocion.State.Velocity, _look.Yaw, input.AimHeld, dt);
            transform.rotation = _facing.Rotacion;

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
    }
}
