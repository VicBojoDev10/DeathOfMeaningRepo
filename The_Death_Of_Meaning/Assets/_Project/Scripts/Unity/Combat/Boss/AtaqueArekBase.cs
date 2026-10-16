using TDOM.Unity.Combat;
using TDOM.Unity.Player;
using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity
{
    /// <summary>
    /// Base de los ataques de AREK. AtaquesArek se asigna como lanzador antes de Spawn; al spawnear,
    /// servidor y clientes lo buscan para leer el perfil de tuning que está en BossArek.prefab.
    /// </summary>
    public abstract class AtaqueArekBase : NetworkBehaviour
    {
        private const float DuracionFlashDebugGolpe = 0.2f;

        private readonly NetworkVariable<ulong> _lanzadorId = new NetworkVariable<ulong>();

        protected AtaquesArek Lanzador { get; private set; }

        /// <summary>Lo llama AtaquesArek antes de Spawn.</summary>
        public void AsignarLanzador(AtaquesArek lanzador)
        {
            _lanzadorId.Value = lanzador.NetworkObjectId;
        }

        public override void OnNetworkSpawn()
        {
            if (
                NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(
                    _lanzadorId.Value,
                    out var obj
                )
            )
                Lanzador = obj.GetComponent<AtaquesArek>();

            if (Lanzador == null)
                Debug.LogError(
                    $"[{GetType().Name}] No se encontró AtaquesArek que lanzó el ataque."
                );

            AlSpawnear();
        }

        /// <summary>Reemplaza a OnNetworkSpawn en cada ataque; corre en servidor y clientes.</summary>
        protected abstract void AlSpawnear();

        // internal para que MinionController (TW-125) reporte sus golpes igual que el jefe.
        internal static void NotificarGolpeAlJugadorLocal()
        {
            var jugador = BuscarJugadorLocal();
            if (jugador == null)
                return;

            jugador.RecibirGolpe();

            var feedbackDebug = jugador.GetComponentInChildren<CombatDebugFeedback>();
            if (feedbackDebug != null)
                feedbackDebug.FlashActive(DuracionFlashDebugGolpe);
        }

        private static PlayerRoot BuscarJugadorLocal()
        {
            var cliente = NetworkManager.Singleton?.LocalClient;
            if (cliente == null)
                return null;

            foreach (var objeto in cliente.OwnedObjects)
            {
                if (objeto.TryGetComponent(out PlayerRoot jugador))
                    return jugador;
            }

            return null;
        }
    }
}
