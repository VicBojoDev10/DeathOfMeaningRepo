using UnityEngine;

namespace TDOM.Unity.Level
{
    public sealed class KillPlane : MonoBehaviour
    {
        [SerializeField] private Transform _respawn;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent<PlayerRoot>(out var player)) return;
            if (!player.IsOwner) return;

            // Evitamos un GetComponent extra
            if (other.TryGetComponent<CharacterController>(out var cc))
            {
                cc.enabled = false;
                other.transform.position = _respawn.position;
                cc.enabled = true;
            }
        }
    }
}
