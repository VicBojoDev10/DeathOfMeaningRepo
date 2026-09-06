using Unity.Netcode;
using UnityEngine;
using TDOM.Gameplay.Locomotion;
using TDOM.Data;

namespace TDOM.Unity.Player
{
    public class PlayerRoot : NetworkBehaviour
    {
        [SerializeField] private CharacterDefinition _definicion;
        private PlayerLocomotion _locomocion;
        public override void OnNetworkSpawn()
        {
            _locomocion = ConstruirLocomocion(_definicion);
            _camara.gameObject.SetActive(IsOwner);
            _inputReader.enabled = IsOwner;
        }
        private void Update()
        {
            if (!IsOwner) return;
            float dt = Time.deltaTime;
            _motor.ProbeGround(_locomocion.State);
            var input = _inputReader.Read();
            var intent = _locomocion.Tick(input, _look.YawRotation, dt);
            _motor.Apply(intent, dt);
        }
    }
}
