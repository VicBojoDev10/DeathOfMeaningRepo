using TDOM.Data;
using TDOM.Unity.Player;
using UnityEngine;

namespace TDOM.Unity.Level
{
    public sealed class KillPlane : MonoBehaviour
    {
        [SerializeField]
        private Transform _respawnAyla;

        [SerializeField]
        private Transform _respawnZendre;

        private Transform ObtenerRespawn(PlayerRoot player)
        {
            return player.CharacterId switch
            {
                CharacterIds.Ayla => _respawnAyla,
                CharacterIds.Zendre => _respawnZendre,
                _ => _respawnAyla,
            };
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent<PlayerRoot>(out var player))
                return;

            if (!player.IsOwner)
                return;

            Transform respawn = ObtenerRespawn(player);

            Debug.LogWarning(
                $"[SPAWN][KILL-PLANE] owner={player.OwnerClientId} "
                    + $"from={other.transform.position} "
                    + $"respawn={respawn.position}"
            );

            player.Respawn(respawn.position, respawn.rotation);
        }
    }
}
