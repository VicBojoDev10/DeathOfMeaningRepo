using TDOM.Data;
using TDOM.Unity.Player;
using Unity.Netcode;
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

        private ArenaEncounterNetwork _encuentroArena;

        private bool ArenaSellada()
        {
            if (_encuentroArena == null || !_encuentroArena.IsSpawned)
                _encuentroArena = FindAnyObjectByType<ArenaEncounterNetwork>();

            return _encuentroArena != null
                && _encuentroArena.IsSpawned
                && _encuentroArena.ArenaSellada.Value;
        }

        private Transform ObtenerRespawn(PlayerRoot player, out bool arenaSellada)
        {
            arenaSellada = ArenaSellada();
            if (arenaSellada)
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

            Transform respawn = ObtenerRespawn(player, out bool arenaSellada);
            Vector3 posicionCheckpoint = default;
            Quaternion rotacionCheckpoint = Quaternion.identity;
            bool tieneCheckpoint =
                !arenaSellada
                && player.TryObtenerCheckpointTutorial(
                    out posicionCheckpoint,
                    out rotacionCheckpoint
                );

            if (arenaSellada)
                player.LimpiarCheckpointTutorial();

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
                    + $"checkpoint={tieneCheckpoint} arena={arenaSellada}"
            );

            player.RespawnConFundido(posicionRespawn, rotacionRespawn);
            return true;
        }
    }
}
