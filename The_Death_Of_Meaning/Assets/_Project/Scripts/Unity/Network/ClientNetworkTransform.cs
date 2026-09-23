using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace TDOM.Unity
{
    [DisallowMultipleComponent]
    public class ClientNetworkTransform : NetworkTransform
    {
        [ServerRpc]
        public void RandomTeleportServerRpc()
        {
            var oldPosition = transform.position;
            transform.position = GetRandomPositionOnXYPlane();
            var newPosition = transform.position;
            print(
                $"{nameof(RandomTeleportServerRpc)}() -> {nameof(OwnerClientId)}: {OwnerClientId} --- {nameof(oldPosition)}: {oldPosition} --- {nameof(newPosition)}: {newPosition}"
            );
        }

        private static Vector3 GetRandomPositionOnXYPlane()
        {
            return new Vector3(Random.Range(-3f, 3f), Random.Range(-3f, 3f), 0f);
        }

        protected override bool OnIsServerAuthoritative()
        {
            return false;
        }
    }
}
