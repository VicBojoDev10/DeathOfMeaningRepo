using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity
{
    public class PlayerCameraSetUp : NetworkBehaviour
    {
        [SerializeField]
        private Vector3 offset = new Vector3(0, 5, -7);

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
                return;

            var cam = Camera.main;
            if (cam == null)
                return;

            cam.transform.SetParent(transform);
            cam.transform.localPosition = offset;
            cam.transform.LookAt(transform);
        }
    }
}
