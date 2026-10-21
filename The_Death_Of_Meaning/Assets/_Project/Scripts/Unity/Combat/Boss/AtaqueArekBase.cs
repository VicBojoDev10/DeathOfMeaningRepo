using TDOM.Unity.Combat;
using TDOM.Unity.Player;
using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity
{
    public abstract class AtaqueArekBase : NetworkBehaviour
    {
        private const float DuracionFlashDebugGolpe = 0.2f;

        private readonly NetworkVariable<ulong> _lanzadorId = new NetworkVariable<ulong>();

        protected AtaquesArek Lanzador { get; private set; }

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

        protected abstract void AlSpawnear();

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

        protected internal static PlayerRoot BuscarJugadorLocal()
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
