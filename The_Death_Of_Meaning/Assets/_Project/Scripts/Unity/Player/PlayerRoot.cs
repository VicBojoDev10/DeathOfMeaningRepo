using TDOM.Data;
using TDOM.Gameplay.Camera;
using TDOM.Gameplay.Locomotion;
using TDOM.Unity.Camera;
using TDOM.Unity.Combat;
using TDOM.Unity.Input;
using TDOM.Unity.Locomotion;
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

        [SerializeField]
        private PlayerCombat _combat;

        [SerializeField]
        private GrappleVisual _grappleVisual;

        [SerializeField]
        private float _sensitivity = 200f;

        private bool _ganchoActivoPrevio;

        public bool AtaqueActivo => _combat != null && _combat.AtaqueActivo;

        public override void OnNetworkSpawn()
        {
            _look = new LookResolver(_sensitivity);
            _locomocion = new PlayerLocomotion(_definition);
            _camera.gameObject.SetActive(IsOwner);
            _inputReader.enabled = IsOwner;

            if (_grappleVisual == null)
                _grappleVisual = GetComponentInChildren<GrappleVisual>();

            if (IsOwner)
            {
                _inputReader.ActivarPersonaje(GetCharacterId());
            }
        }

        private CharacterIds GetCharacterId()
        {
            if (_definition != null)
            {
                if (
                    string.Equals(
                        _definition.DisplayName,
                        "Zendre",
                        System.StringComparison.OrdinalIgnoreCase
                    )
                )
                    return CharacterIds.Zendre;
                if (
                    string.Equals(
                        _definition.DisplayName,
                        "Ayla",
                        System.StringComparison.OrdinalIgnoreCase
                    )
                )
                    return CharacterIds.Ayla;
            }
            return CharacterIds.None;
        }

        private void Update()
        {
            if (!IsOwner)
                return;

            float dt = Time.deltaTime;
            var input = _inputReader.Read();

            // Actualizar rotación POCO y aplicar a la cámara
            _look.Tick(input.Look, dt);
            if (_camera != null)
                _camera.ApplyLook(_look.Yaw, _look.Pitch);

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

            _motor.ProbeGround(_locomocion.State);

            if (_combat != null)
                _combat.Tick(input, dt);

            var intent = _locomocion.Tick(
                input,
                _look.YawRotation,
                dt,
                AtaqueActivo,
                transform.position
            );
            _motor.Apply(intent, dt);

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
