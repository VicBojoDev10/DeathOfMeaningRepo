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
        private readonly CharacterDefinition _definicion;

        [SerializeField]
        private readonly PlayerMotor _motor;
        private PlayerLocomotion _locomocion;

        [SerializeField]
        private readonly PlayerCameraRig _camera;
        private readonly LookResolver _look;

        [SerializeField]
        private PlayerInputReader _inputReader;

        public override void OnNetworkSpawn()
        {
            _locomocion = new PlayerLocomotion(_definicion);
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
            var intent = _locomocion.Tick(input, _look.YawRotation, dt);
            _motor.Apply(intent, dt);
        }
    }
}
