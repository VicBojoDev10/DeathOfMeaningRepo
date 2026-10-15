using System;
using TDOM.Unity.UI;
using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity
{
    public class GameSessionManager : NetworkBehaviour
    {
        public static GameSessionManager Instance { get; private set; }

        [SerializeField]
        private GameObject _prefabZendre;

        [SerializeField]
        private GameObject _prefabAyla;

        [SerializeField]
        private GameObject _prefabArek;

        [SerializeField]
        private Transform _puntoSpawnArek;

        [SerializeField]
        private Transform _spawnPointZendre;

        [SerializeField]
        private Transform _spawnPointAyla;
        public NetworkList<PlayerSelectionState> Jugadores { get; private set; }

        public static event Action PartidaIniciada;
        public event Action OnJugadorDesconectado;
        public event Action OnPartidaIniciada;

        private void Awake()
        {
            Jugadores = new NetworkList<PlayerSelectionState>();
        }

        public override void OnNetworkSpawn()
        {
            Instance = this;

            if (IsServer)
            {
                Debug.Log("[SERVER] Suscrito a OnClientDisconnectCallback");
                NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (Instance == this)
                Instance = null;

            if (IsServer && NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
            }
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            Debug.Log($"[SERVER] Cliente desconectado: {clientId}");
            int idx = BuscarIndice(clientId);
            if (idx < 0)
            {
                Debug.Log("[SERVER] No se encontró ese clientId en Jugadores");
                return;
            }
            Jugadores.RemoveAt(idx);
            Debug.Log($"[SERVER] Jugador removido, quedan: {Jugadores.Count}");
            for (int i = 0; i < Jugadores.Count; i++)
            {
                if (Jugadores[i].Ready)
                {
                    var estado = Jugadores[i];
                    estado.Ready = false;
                    Jugadores[i] = estado;
                }
            }

            NotificarDesconexionClientRpc();
        }

        [ClientRpc]
        private void NotificarDesconexionClientRpc()
        {
            OnJugadorDesconectado?.Invoke();
        }

        [Rpc(SendTo.Server)]
        public void ElegirPersonajeRpc(CharacterIds id, RpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            for (int i = 0; i < Jugadores.Count; i++)
            {
                if (Jugadores[i].Character == id && Jugadores[i].ClientId != clientId)
                {
                    PersonajeRechazadoRpc(RpcTarget.Single(clientId, RpcTargetUse.Temp));
                    return;
                }
            }
            int idx = BuscarIndice(clientId);
            var estado = new PlayerSelectionState
            {
                ClientId = clientId,
                Character = id,
                Ready = false,
            };
            if (idx >= 0)
                Jugadores[idx] = estado;
            else
                Jugadores.Add(estado);
        }

        [Rpc(SendTo.Server)]
        public void MarcarListoRpc(RpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            int idx = BuscarIndice(clientId);
            if (idx < 0 || Jugadores[idx].Character == CharacterIds.None)
                return;
            var estado = Jugadores[idx];
            estado.Ready = true;
            Jugadores[idx] = estado;
            VerificarInicioPartida();
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void PersonajeRechazadoRpc(RpcParams rpcParams) { }

        private int BuscarIndice(ulong clientId)
        {
            for (int i = 0; i < Jugadores.Count; i++)
                if (Jugadores[i].ClientId == clientId)
                    return i;
            return -1;
        }

        private void VerificarInicioPartida()
        {
            if (Jugadores.Count != 2)
                return;
            foreach (var j in Jugadores)
                if (!j.Ready)
                    return;

            foreach (var jugador in Jugadores)
            {
                var (prefab, spawnPoint) = jugador.Character switch
                {
                    CharacterIds.Zendre => (_prefabZendre, _spawnPointZendre),
                    CharacterIds.Ayla => (_prefabAyla, _spawnPointAyla),
                    _ => (null, null),
                };

                if (prefab == null || spawnPoint == null)
                {
                    Debug.LogError(
                        $"No se puede iniciar: falta prefab o punto de spawn para {jugador.Character}."
                    );
                    return;
                }

                var go = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
                Debug.Log(
                    $"[SPAWN][SERVER] {jugador.Character} owner={jugador.ClientId} "
                        + $"point={spawnPoint.name} position={spawnPoint.position}"
                );
                var player = go.GetComponent<TDOM.Unity.Player.PlayerRoot>();
                if (player == null)
                {
                    Debug.LogError($"El prefab de {jugador.Character} no tiene PlayerRoot.");
                    Destroy(go);
                    return;
                }

                var networkObject = go.GetComponent<NetworkObject>();
                networkObject.SpawnWithOwnership(jugador.ClientId);
                player.EnviarSpawnInicial(spawnPoint.position, spawnPoint.rotation);
            }

            if (_prefabArek != null)
            {
                Vector3 posArek =
                    _puntoSpawnArek != null ? _puntoSpawnArek.position : new Vector3(0f, 0f, 10f);
                Quaternion rotArek =
                    _puntoSpawnArek != null ? _puntoSpawnArek.rotation : Quaternion.identity;
                var goArek = Instantiate(_prefabArek, posArek, rotArek);
                goArek.GetComponent<NetworkObject>().Spawn();
            }

            IniciarPartidaClientRpc();
        }

        [ClientRpc]
        private void IniciarPartidaClientRpc()
        {
            PartidaIniciada?.Invoke();
            OnPartidaIniciada?.Invoke();

            if (TDOM.Unity.Audio.AudioManager.Instance != null)
            {
                TDOM.Unity.Audio.AudioManager.Instance.Play("battle");
            }

            if (UiManager.Instance != null)
            {
                UiManager.Instance.CambiarVentana(WindowsIds.ChSelectionUI, WindowsIds.GamePlayUI);
            }
        }
    }
}
