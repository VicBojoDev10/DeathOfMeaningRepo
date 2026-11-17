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

        [SerializeField]
        private Transform _respawnAylaArena;

        [SerializeField]
        private Transform _respawnZendreArena;

        private Transform ObtenerRespawn(PlayerRoot player)
        {
            if (
                ArenaEncounterNetwork.Instance != null
                && ArenaEncounterNetwork.Instance.ArenaSellada.Value
            )
            {
                Transform respawnArena = player.CharacterId switch
                {
                    CharacterIds.Ayla => ObtenerPuntoArena(
                        ref _respawnAylaArena,
                        "RespawnAylaArena"
                    ),
                    CharacterIds.Zendre => ObtenerPuntoArena(
                        ref _respawnZendreArena,
                        "RespawnZendreArena"
                    ),
                    _ => ObtenerPuntoArena(ref _respawnAylaArena, "RespawnAylaArena"),
                };
                if (respawnArena != null)
                    return respawnArena;
            }

            return player.CharacterId switch
            {
                CharacterIds.Ayla => _respawnAyla,
                CharacterIds.Zendre => _respawnZendre,
                _ => _respawnAyla,
            };
        }

        private static Transform ObtenerPuntoArena(ref Transform punto, string nombre)
        {
            if (punto == null)
            {
                var objeto = GameObject.Find(nombre);
                if (objeto != null)
                    punto = objeto.transform;
            }
            return punto;
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

            bool tieneCheckpoint = player.TryObtenerCheckpointTutorial(
                out Vector3 posicionCheckpoint,
                out Quaternion rotacionCheckpoint
            );
            Transform respawn = tieneCheckpoint ? null : ObtenerRespawn(player);
            if (!tieneCheckpoint && respawn == null)
            {
                Debug.LogError(
                    $"[KillPlane] Falta asignar el punto de respawn para {player.CharacterId}.",
                    this
                );
                return false;
            }

            Vector3 posicionRespawn = tieneCheckpoint ? posicionCheckpoint : respawn.position;
            Quaternion rotacionRespawn = tieneCheckpoint ? rotacionCheckpoint : respawn.rotation;
            Debug.LogWarning(
                $"[SPAWN][KILL-PLANE] owner={player.OwnerClientId} personaje={player.CharacterId} "
                    + $"from={player.transform.position} respawn={posicionRespawn} "
                    + $"checkpoint={tieneCheckpoint}"
            );

            player.RespawnConFundido(posicionRespawn, rotacionRespawn);
            return true;
        }
    }
}
