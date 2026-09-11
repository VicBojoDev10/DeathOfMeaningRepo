using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity.Camera
{
    [RequireComponent(typeof(CinemachineCamera))]
    public sealed class PlayerCameraActivator : NetworkBehaviour
    {
        [SerializeField]
        private CinemachineCamera _camara;

        [SerializeField]
        private AudioListener _audioListener;

        public override void OnNetworkSpawn()
        {
            bool esMio = IsOwner;

            if (_camara != null)
                _camara.enabled = esMio;

            if (_audioListener != null)
                _audioListener.enabled = esMio;
        }
    }
}
