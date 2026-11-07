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
        private bool _partidaIniciada;
        private bool _cronometroActivo;
        private float _inicioCronometro;

        public bool PartidaIniciadaEnServidor => _partidaIniciada;

        private bool _reinicioVictoriaEnCurso;

        private void IniciarCronometro()
        {
            _inicioCronometro = Time.time;
            _cronometroActivo = true;
            Debug.Log("[GameSessionManager] Cronómetro de partida iniciado.");
        }

        public void FinalizarPartidaDesdeServidor()
        {
            if (!IsServer || !_cronometroActivo)
                return;

            float tiempoFinal = Mathf.Max(0f, Time.time - _inicioCronometro);
            PauseManager.Instance?.EstablecerResultadoDesdeServidor(ResultadoPartida.Victoria);
            LimpiarMinionsEnServidor();
            _cronometroActivo = false;
            Debug.Log($"[GameSessionManager] Partida terminada en {tiempoFinal:F1} segundos.");
            MostrarVictoriaClientRpc(tiempoFinal);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void MostrarVictoriaClientRpc(float tiempoFinal)
        {
            GlobalGameManager.Instance?.DetenerMusicaPartida();
            var ui = UiManager.Instance;

            void MostrarVictoria()
            {
                ui?.CloseWindow(WindowsIds.GamePlayUI);
                ui?.ShowWindow("victoriaui");
                (ui?.GetWindow("victoriaui") as VictoriaUI)?.MostrarTiempoFinal(tiempoFinal);
            }

            var transicion = ui?.TransitionManager;
            if (transicion != null)
                transicion.EjecutarAccionConCarga(MostrarVictoria);
            else
                MostrarVictoria();
        }

        private void Awake()
        {
            Jugadores = new NetworkList<PlayerSelectionState>();
        }

        public override void OnNetworkSpawn()
        {
            Debug.Log(
                $"[NetSpawn GameSessionManager] server={IsServer}, client={IsClient}, "
                    + $"id={NetworkObjectId}, escena={gameObject.scene.name}"
            );
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
            Debug.Log(
                $"[NetDespawn GameSessionManager] server={IsServer}, client={IsClient}, "
                    + $"id={NetworkObjectId}, escena={gameObject.scene.name}"
            );
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

            _inicioPartidaEnProceso = false;
            if (!_partidaIniciada)
            {
                _partidaIniciada = true;
                IniciarCronometro();
            }

            if (!_inicioPartidaNotificado)
            {
                _inicioPartidaNotificado = true;
                IniciarPartidaClientRpc();
            }
        }
        public void SpawnBossForEncounter()
        {
            if (!IsServer || HayJefeSpawneadoEnRed()) return;
            if (_prefabArek == null)
            {
                Debug.LogError("No se puede iniciar la presentación: falta el prefab de Arek.");
                return;
            }

            Vector3 posicion = _puntoSpawnArek != null ? _puntoSpawnArek.position : new Vector3(0f, 0f, 10f);
            Quaternion rotacion = _puntoSpawnArek != null ? _puntoSpawnArek.rotation : Quaternion.identity;
            var jefe = Instantiate(_prefabArek, posicion, rotacion);
            var networkObject = jefe.GetComponent<NetworkObject>();
            if (networkObject == null)
            {
                Debug.LogError("El prefab de Arek no tiene NetworkObject.");
                Destroy(jefe);
                return;
            }
            networkObject.Spawn();
            Debug.Log($"[SPAWN][SERVER] Arek creado para presentación. id={networkObject.NetworkObjectId}, posición={posicion}");
        }

        public void SolicitarReinicioDePartida()
        {
            SolicitarReinicioDesdeVictoriaRpc();
        }

        public void SolicitarReinicioDePartidaDesdeServidor()
        {
            if (
                !IsServer
                || _reinicioVictoriaEnCurso
                || _inicioPartidaEnProceso
                || !_seleccion.PuedeIniciar
            )
                return;

            _reinicioVictoriaEnCurso = true;
            ReiniciarConCargaVictoriaRpc();
        }

        [Rpc(SendTo.Server)]
        private void SolicitarReinicioDesdeVictoriaRpc(RpcParams rpcParams = default)
        {
            SolicitarReinicioDePartidaDesdeServidor();
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void ReiniciarConCargaVictoriaRpc()
        {
            var transicion = UiManager.Instance?.TransitionManager;

            if (transicion == null)
            {
                if (IsServer)
                {
                    ReiniciarPartidaDesdePausa();
                    _reinicioVictoriaEnCurso = false;
                }

                GlobalGameManager.Instance?.ReproducirMusicaTutorial();

                return;
            }

            transicion.EjecutarAccionConCarga(
                () =>
                {
                    if (IsServer)
                        ReiniciarPartidaDesdePausa();
                    UiManager.Instance?.CloseWindow("victoriaui");
                    UiManager.Instance?.CloseWindow(WindowsIds.RetryPopUpUI);
                    (UiManager.Instance?.GetWindow(WindowsIds.PausaUI) as PausaUI)?.SetPopupActivo(
                        false
                    );
                    UiManager.Instance?.ShowWindow(WindowsIds.GamePlayUI);
                    GlobalGameManager.Instance?.ReproducirMusicaTutorial();
                },
                () =>
                {
                    if (IsServer)
                        _reinicioVictoriaEnCurso = false;
                }
            );
        }

        private bool HayJefeSpawneadoEnRed()
        {
            var spawnManager = NetworkManager?.SpawnManager;
            if (spawnManager == null)
                return false;

            foreach (var objeto in spawnManager.SpawnedObjectsList)
            {
                if (
                    objeto != null
                    && objeto.IsSpawned
                    && objeto.TryGetComponent(out BossController _)
                )
                    return true;
            }

            return false;
        }

        public void ReiniciarPartidaDesdePausa()
        {
            if (!IsServer || _inicioPartidaEnProceso || !_seleccion.PuedeIniciar)
                return;

            ArenaEncounterNetwork.Instance?.ReiniciarServidor();

            PauseManager.Instance?.RestablecerResultadoDesdeServidor();
            LimpiarMinionsEnServidor();
            DespawnearJefesEnServidor();
            _inicioPartidaEnProceso = true;
            var objetosSpawneados = new List<NetworkObject>(
                NetworkManager.SpawnManager.SpawnedObjectsList
            );
            foreach (var objeto in objetosSpawneados)
            {
                if (objeto == null || !objeto.IsSpawned)
                    continue;

                bool esJugador = objeto.TryGetComponent(out TDOM.Unity.Player.PlayerRoot _);
                bool esAtaque = objeto.TryGetComponent(out AtaqueArekBase _);
                bool esAncla = objeto.TryGetComponent(out Ancla _);
                bool esProyectil = objeto.TryGetComponent(out Proyectil _);
                if (esJugador || esAtaque || esAncla || esProyectil)
                    objeto.Despawn(true);
            }

            _inicioPartidaEnProceso = false;
            VerificarInicioPartida();
            IniciarCronometro();
        }

        private void DespawnearJefesEnServidor()
        {
            if (!IsServer || NetworkManager?.SpawnManager == null) return;

            var objetos = new List<NetworkObject>(NetworkManager.SpawnManager.SpawnedObjectsList);
            int despawneados = 0;
            foreach (var objeto in objetos)
            {
                if (objeto == null || !objeto.IsSpawned || !objeto.TryGetComponent(out BossController _))
                    continue;
                objeto.Despawn(true);
                despawneados++;
            }
            Debug.Log($"[GameSessionManager] Jefes despawneados al reiniciar: {despawneados}.");
        }

        private void LimpiarMinionsEnServidor()
        {
            if (!IsServer || NetworkManager == null || NetworkManager.SpawnManager == null)
                return;

            PowerUpsSpawnController.LimpiarEnServidor();

            var objetosSpawneados = new List<NetworkObject>(
                NetworkManager.SpawnManager.SpawnedObjectsList
            );

            foreach (var objeto in objetosSpawneados)
            {
                if (objeto != null && objeto.TryGetComponent(out AtaquesArek ataques))
                    ataques.LimpiarColumnasEnServidor();
            }

            objetosSpawneados = new List<NetworkObject>(
                NetworkManager.SpawnManager.SpawnedObjectsList
            );
            foreach (var objeto in objetosSpawneados)
            {
                if (
                    objeto != null
                    && objeto.IsSpawned
                    && objeto.TryGetComponent(out ColumnaMortero _)
                )
                    objeto.Despawn(true);
            }

            MinionSpawner spawner = null;
            foreach (var objeto in NetworkManager.SpawnManager.SpawnedObjectsList)
            {
                if (objeto != null && objeto.TryGetComponent(out spawner))
                    break;
            }

            spawner?.LimpiarTodos();
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

            if (UiManager.Instance != null)
            {
                UiManager.Instance.CloseWindow(WindowsIds.ConnectionMenuUI);
                UiManager.Instance.CloseWindow(WindowsIds.CancelHostUI);
                UiManager.Instance.CloseWindow(WindowsIds.ChSelectionUI);
                UiManager.Instance.ShowWindow(WindowsIds.GamePlayUI);
            }
        }

        public void FinalizarPartidaPorDerrotaDesdeServidor(int fase)
        {
            if (!IsServer || !_cronometroActivo)
                return;

            _cronometroActivo = false;
            PauseManager.Instance?.EstablecerResultadoDesdeServidor(ResultadoPartida.Derrota);
            LimpiarMinionsEnServidor();
            Debug.Log($"[GameSessionManager] Derrota registrada en la fase {fase}.");
            MostrarDerrotaClientRpc(fase);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void MostrarDerrotaClientRpc(int fase)
        {
            GlobalGameManager.Instance?.DetenerMusicaPartida();
            var ui = UiManager.Instance;

            void MostrarDerrota()
            {
                ui?.CloseWindow(WindowsIds.GamePlayUI);
                ui?.ShowWindow(WindowsIds.DerrotaUI);
                (ui?.GetWindow(WindowsIds.DerrotaUI) as DerrotaUI)?.MostrarFaseJefe(fase);
            }

            var transicion = ui?.TransitionManager;
            if (transicion != null)
                transicion.EjecutarAccionConCarga(MostrarDerrota);
            else
                MostrarDerrota();
        }

#if UNITY_EDITOR
        private void OnGUI()
        {
            if (!IsSpawned || !IsServer || !IsHost || !_inicioPartidaNotificado)
                return;

            if (GUI.Button(new Rect(20, 20, 220, 40), "DEBUG: Mostrar victoria"))
                ForzarVictoriaDebugRpc();

            if (GUI.Button(new Rect(20, 65, 220, 40), "DEBUG: Mostrar derrota"))
                ForzarDerrotaDebugRpc();
        }

        [Rpc(SendTo.Server)]
        private void ForzarVictoriaDebugRpc(RpcParams rpcParams = default)
        {
            if (!IsServer || !_inicioPartidaNotificado)
                return;

            PauseManager.Instance?.EstablecerResultadoDesdeServidor(ResultadoPartida.Victoria);
            MostrarVictoriaDebugClientRpc();
        }

        [Rpc(SendTo.Server)]
        private void ForzarDerrotaDebugRpc(RpcParams rpcParams = default)
        {
            if (!IsServer || !_inicioPartidaNotificado)
                return;

            var jefe = FindAnyObjectByType<BossController>();
            int fase = jefe != null ? jefe.Fase.Value : 1;
            FinalizarPartidaPorDerrotaDesdeServidor(fase);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void MostrarVictoriaDebugClientRpc()
        {
            GlobalGameManager.Instance?.DetenerMusicaPartida();
            var ui = UiManager.Instance;

            void MostrarVictoria()
            {
                ui?.CloseWindow(WindowsIds.GamePlayUI);
                ui?.ShowWindow("victoriaui");
            }

            var transicion = ui?.TransitionManager;

            if (transicion != null)
                transicion.EjecutarAccionConCarga(MostrarVictoria);
            else
                MostrarVictoria();
        }
#endif
    }
}
