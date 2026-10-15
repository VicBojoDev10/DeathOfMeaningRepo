using System;
using System.Collections.Generic;
using TDOM.Gameplay.Core;
using TDOM.Unity.Combat;
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

        public static event Action Spawned;
        public static event Action Despawned;
        public event Action<CharacterIds> PersonajeRechazado;
        private readonly ConfirmedCharacterSelection _seleccion = new ConfirmedCharacterSelection();
        private bool _inicioPartidaEnProceso;
        private bool _inicioPartidaNotificado;
        private bool _cerrandoSesionPorSalida;

        private void Awake()
        {
            Jugadores = new NetworkList<PlayerSelectionState>();
        }

        public override void OnNetworkSpawn()
        {
            Instance = this;
            Spawned?.Invoke();

            if (IsServer)
            {
                Debug.Log("[SERVER] Suscrito a OnClientDisconnectCallback");
                NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
            }
        }

        public override void OnNetworkDespawn()
        {
            Despawned?.Invoke();
            if (Instance == this)
                Instance = null;

            if (IsServer && NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
            }
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            if (_cerrandoSesionPorSalida)
                return;

            Debug.Log($"[SERVER] Cliente desconectado: {clientId}");
            _seleccion.Desconectar(clientId);
            SincronizarJugadores();
            Debug.Log($"[SERVER] Jugador removido, quedan: {Jugadores.Count}");

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
            if (!_seleccion.Elegir(clientId, (ConfirmedCharacterSelection.Character)(int)id))
            {
                PersonajeRechazadoRpc(id, RpcTarget.Single(clientId, RpcTargetUse.Temp));
                return;
            }
            SincronizarJugadores();
        }

        [Rpc(SendTo.Server)]
        public void MarcarListoRpc(RpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            _seleccion.MarcarListo(clientId);
            SincronizarJugadores();
            VerificarInicioPartida();
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void PersonajeRechazadoRpc(CharacterIds character, RpcParams rpcParams = default) =>
            PersonajeRechazado?.Invoke(character);

        private void SincronizarJugadores()
        {
            for (int i = Jugadores.Count - 1; i >= 0; i--)
            {
                bool stillConnected = false;
                foreach (var player in _seleccion.Players)
                    if (player.ClientId == Jugadores[i].ClientId)
                    {
                        stillConnected = true;
                        break;
                    }
                if (!stillConnected)
                    Jugadores.RemoveAt(i);
            }

            foreach (var jugador in _seleccion.Players)
            {
                var state = new PlayerSelectionState
                {
                    ClientId = jugador.ClientId,
                    Character = (CharacterIds)(int)jugador.SelectedCharacter,
                    Ready = jugador.Ready,
                };
                int index = -1;
                for (int i = 0; i < Jugadores.Count; i++)
                    if (Jugadores[i].ClientId == jugador.ClientId)
                    {
                        index = i;
                        break;
                    }
                if (index < 0)
                    Jugadores.Add(state);
                else if (!Jugadores[index].Equals(state))
                    Jugadores[index] = state;
            }
        }

        private void VerificarInicioPartida()
        {
            if (!_seleccion.PuedeIniciar || _inicioPartidaEnProceso)
                return;

            _inicioPartidaEnProceso = true;

            foreach (var jugador in Jugadores)
            {
                // Un RPC de listo repetido nunca debe crear un segundo PlayerRoot para el mismo cliente.
                if (BuscarJugadorSpawneado(jugador.ClientId) != null)
                    continue;

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
                    _inicioPartidaEnProceso = false;
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
                    _inicioPartidaEnProceso = false;
                    return;
                }

                var networkObject = go.GetComponent<NetworkObject>();
                networkObject.SpawnWithOwnership(jugador.ClientId);
                player.EnviarSpawnInicial(spawnPoint.position, spawnPoint.rotation);
            }

            if (_prefabArek != null)
            {
                if (FindAnyObjectByType<TDOM.Unity.Combat.BossController>() == null)
                {
                    Vector3 posArek =
                        _puntoSpawnArek != null
                            ? _puntoSpawnArek.position
                            : new Vector3(0f, 0f, 10f);
                    Quaternion rotArek =
                        _puntoSpawnArek != null ? _puntoSpawnArek.rotation : Quaternion.identity;
                    var goArek = Instantiate(_prefabArek, posArek, rotArek);
                    goArek.GetComponent<NetworkObject>().Spawn();
                }
            }

            _inicioPartidaEnProceso = false;
            if (!_inicioPartidaNotificado)
            {
                _inicioPartidaNotificado = true;
                IniciarPartidaClientRpc();
            }
        }

        public void ReiniciarPartidaDesdePausa()
        {
            if (!IsServer || _inicioPartidaEnProceso || !_seleccion.PuedeIniciar)
                return;

            _inicioPartidaEnProceso = true;
            var objetosSpawneados = new List<NetworkObject>(
                NetworkManager.SpawnManager.SpawnedObjectsList
            );
            foreach (var objeto in objetosSpawneados)
            {
                if (objeto == null || !objeto.IsSpawned)
                    continue;

                bool esJugador = objeto.TryGetComponent(out TDOM.Unity.Player.PlayerRoot _);
                bool esJefe = objeto.TryGetComponent(out BossController _);
                bool esAtaque = objeto.TryGetComponent(out AtaqueArekBase _);
                bool esAncla = objeto.TryGetComponent(out Ancla _);
                bool esProyectil = objeto.TryGetComponent(out Proyectil _);
                if (esJugador || esJefe || esAtaque || esAncla || esProyectil)
                    objeto.Despawn(true);
            }

            _inicioPartidaEnProceso = false;
            VerificarInicioPartida();
        }

        public void PrepararSalidaAlMenu() => _cerrandoSesionPorSalida = true;

        private TDOM.Unity.Player.PlayerRoot BuscarJugadorSpawneado(ulong clientId)
        {
            if (NetworkManager == null || NetworkManager.SpawnManager == null)
                return null;

            foreach (var objeto in NetworkManager.SpawnManager.SpawnedObjectsList)
            {
                if (
                    objeto != null
                    && objeto.IsSpawned
                    && objeto.OwnerClientId == clientId
                    && objeto.TryGetComponent(out TDOM.Unity.Player.PlayerRoot player)
                )
                    return player;
            }

            return null;
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
                UiManager.Instance.SwitchWindow(WindowsIds.ChSelectionUI, WindowsIds.GamePlayUI);
            }
        }
    }
}
