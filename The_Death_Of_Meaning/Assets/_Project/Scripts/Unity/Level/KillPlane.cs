using TDOM.Unity.Player;
using UnityEngine;

namespace TDOM.Unity.Level
{
    public sealed class KillPlane : MonoBehaviour
    {
        [SerializeField]
        private Transform _respawn;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent<PlayerRoot>(out var player))
                return;
            if (!player.IsOwner)
                return;

            if (other.TryGetComponent<CharacterController>(out var cc))
            {
                Debug.LogWarning(
                    $"[SPAWN][KILL-PLANE] owner={player.OwnerClientId} "
                        + $"from={other.transform.position} respawn={_respawn.position}"
                );
                cc.enabled = false;
                var networkTransform = other.GetComponent<TDOM.Unity.ClientNetworkTransform>();
                if (networkTransform != null)
                {
                    networkTransform.Teleport(
                        _respawn.position,
                        _respawn.rotation,
                        other.transform.localScale
                    );
                }
                else
                {
                    other.transform.SetPositionAndRotation(_respawn.position, _respawn.rotation);
                }
                cc.enabled = true;
            }
        }
    }
}
