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

        public override void OnNetworkSpawn()
        {
            _anchorLauncher = GetComponentInChildren<AnchorLauncher>();
            _look = new LookResolver(_sensitivity, yawInicial: transform.eulerAngles.y);
            _locomocion = new PlayerLocomotion(_definition);
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
            transform.rotation = _look.YawRotation;

            if (_camera != null)
                _camera.ApplyLook(_look.Yaw, _look.Pitch);

            _motor.ProbeGround(_locomocion.State);

            if (_combat != null)
                _combat.Tick(input, dt);

            if (
                input.GrapplePressed
                && !AtaqueActivo
                && _definition != null
                && _definition.Grapple != null
                && _camera != null
            )
            {
                Ray ray = new Ray(_camera.transform.position, _camera.transform.forward);
                if (
                    Physics.Raycast(
                        ray,
                        out RaycastHit hit,
                        _definition.Grapple.Range,
                        Physics.AllLayers,
                        QueryTriggerInteraction.Ignore
                    )
                )
                {
                    if (hit.collider.GetComponentInParent<PlayerRoot>() == null)
                    {
                        if (_locomocion.IntentarGancho(transform.position, hit.point))
                        {
                            _camera.PunchFov(8f, 0.2f);
                        }
                    }
                }
            }

            var intent = _locomocion.Tick(
                input,
                _look.YawRotation,
                dt,
                AtaqueActivo,
                transform.position
            );
            _motor.Apply(intent, dt);

            DispararFeedbackDeLocomocion();

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
