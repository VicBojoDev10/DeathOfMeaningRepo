using TDOM.Data;
using TDOM.Gameplay.Camera;
using TDOM.Gameplay.Locomotion;
using TDOM.Unity.Camera;
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
        private float _sensitivity = 200f;

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
            _look.Tick(input.Look, dt);
            var intent = _locomocion.Tick(input, _look.YawRotation, dt);
            _camera.ApplyLook(_look.Yaw, _look.Pitch);
            _motor.Apply(intent, dt);
        }
    }
}
