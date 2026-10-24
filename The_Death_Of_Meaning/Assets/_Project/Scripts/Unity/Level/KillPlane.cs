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
            var player = other.GetComponentInParent<PlayerRoot>();
            if (player != null && player.IsOwner)
                RespawnJugador(player);
        }

        public bool RespawnJugador(PlayerRoot player)
        {
            if (player == null || !player.IsOwner)
                return false;

            Transform respawn = ObtenerRespawn(player);
            if (respawn == null)
            {
                Debug.LogError(
                    $"[KillPlane] Falta asignar el punto de respawn para {player.CharacterId}.",
                    this
                );
                return false;
            }

            Debug.LogWarning(
                $"[SPAWN][KILL-PLANE] owner={player.OwnerClientId} personaje={player.CharacterId} "
                    + $"from={player.transform.position} respawn={respawn.position}"
            );

            player.RespawnConFundido(respawn.position, respawn.rotation);
            return true;
        }
    }
}

