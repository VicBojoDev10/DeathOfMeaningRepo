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
        private float _sensitivity = 200f;

        public bool AtaqueActivo => _combat != null && _combat.AtaqueActivo;

        public override void OnNetworkSpawn()
        {
            _look = new LookResolver(_sensitivity);
            _locomocion = new PlayerLocomotion(_definition);
            _camera.gameObject.SetActive(IsOwner);
            _inputReader.enabled = IsOwner;
        }

        private void Update()
        {
            if (!IsOwner)
                return;
            float dt = Time.deltaTime;
            _motor.ProbeGround(_locomocion.State);
            var input = _inputReader.Read();
            if (_combat != null)
                _combat.Tick(input, dt);
            var intent = _locomocion.Tick(input, _look.YawRotation, dt, AtaqueActivo);
            _motor.Apply(intent, dt);
        }
    }
}
