using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using TDOM.Gameplay.Core;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace TDOM.Unity
{
    public class ConnectionManager : Singleton<ConnectionManager>
    {
        [SerializeField]
        private GameObject gameFlowNetworkPrefab;
        private readonly SessionStatus _status = new();
        private readonly HashSet<ulong> _clientesAprobadosPendientes = new();
        public event Action<EstadoSesion> OnEstadoCambiado;
        public event Action<int> OnJugadoresCambiado;
        public event Action OnCargaEscenaIniciada;
        public event Action OnCargaEscenaCompletada;
        public event Action<string> OnConexionRechazada;

        private bool _isSubscribed;

        private void Start()
        {
            SuscribirEventosNetwork();
        }

        private void OnEnable()
        {
            SuscribirEventosNetwork();
        }

        private void OnDisable()
        {
            DesuscribirEventosNetwork();
        }

        private void OnDestroy()
        {
            DesuscribirEventosNetwork();
        }

        private void SuscribirEventosNetwork()
        {
            if (_isSubscribed || NetworkManager.Singleton == null)
                return;

            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
            NetworkManager.Singleton.OnServerStarted += HandleServerStarted;
            NetworkManager.Singleton.OnTransportFailure += HandleTransportFailure;
            NetworkManager.Singleton.OnClientStarted += HandleClientStarted;
            NetworkManager.Singleton.OnClientStopped += HandleClientStopped;
            NetworkManager.Singleton.ConnectionApprovalCallback = HandleConnectionApproval;
            _isSubscribed = true;
        }

        private void DesuscribirEventosNetwork()
        {
            if (!_isSubscribed || NetworkManager.Singleton == null)
                return;

            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
            NetworkManager.Singleton.OnServerStarted -= HandleServerStarted;
            NetworkManager.Singleton.OnTransportFailure -= HandleTransportFailure;
            NetworkManager.Singleton.OnClientStarted -= HandleClientStarted;
            NetworkManager.Singleton.OnClientStopped -= HandleClientStopped;
            if (NetworkManager.Singleton.ConnectionApprovalCallback == HandleConnectionApproval)
                NetworkManager.Singleton.ConnectionApprovalCallback = null;
            DesuscribirEventosEscena();
            _isSubscribed = false;
        }

        private void HandleConnectionApproval(
            NetworkManager.ConnectionApprovalRequest request,
            NetworkManager.ConnectionApprovalResponse response
        )
        {
            var networkManager = NetworkManager.Singleton;
            int conectados =
                networkManager != null
                    ? networkManager.ConnectedClientsList.Count + _clientesAprobadosPendientes.Count
                    : 0;
            bool partidaIniciada =
                GameSessionManager.Instance != null
                && GameSessionManager.Instance.PartidaIniciadaEnServidor;

            response.Approved = TDOM.Gameplay.ConnectionAccepted.PuedeAceptarConexion(
                conectados,
                partidaIniciada
            );
            response.CreatePlayerObject = false;

            if (!response.Approved)
            {
                response.Reason = partidaIniciada ? "Partida en curso" : "Partida llena";
            }
            else
            {
                _clientesAprobadosPendientes.Add(request.ClientNetworkId);
            }
        }

        public void OnCrearPartida()
        {
            if (NetworkManager.Singleton == null)
            {
                Debug.LogError(
                    "[ConnectionManager] NetworkManager.Singleton es null al intentar crear partida."
                );
                CambiarEstado(EstadoSesion.Error);
                return;
            }

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport != null)
            {
                transport.ConnectionData.Address = "127.0.0.1";
                transport.ConnectionData.Port = 7777;
                transport.ConnectionData.ServerListenAddress = "0.0.0.0";
            }
            NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;
            NetworkManager.Singleton.ConnectionApprovalCallback = HandleConnectionApproval;
            CambiarEstado(EstadoSesion.Conectando);
            NetworkManager.Singleton.StartHost();
        }

        public void OnUnirse(string ip)
        {
            if (!IPAddress.TryParse(ip, out _))
            {
                Debug.LogError($"IP inválida: '{ip}'");
                CambiarEstado(EstadoSesion.Error);
                return;
            }

            if (NetworkManager.Singleton == null)
            {
                Debug.LogError(
                    "[ConnectionManager] NetworkManager.Singleton es null al intentar unirse."
                );
                CambiarEstado(EstadoSesion.Error);
                return;
            }

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport != null)
            {
                transport.ConnectionData.Address = ip;
                transport.ConnectionData.Port = 7777;
            }

            NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;
            CambiarEstado(EstadoSesion.Conectando);
            NetworkManager.Singleton.StartClient();
        }

        private void HandleServerStarted()
        {
            CambiarEstado(EstadoSesion.Listo);
            if (NetworkManager.Singleton.IsServer)
            {
                var flowObj = Instantiate(gameFlowNetworkPrefab);
                flowObj.GetComponent<NetworkObject>().Spawn();
            }
        }

        private void HandleClientConnected(ulong clientId)
        {
            _clientesAprobadosPendientes.Remove(clientId);
            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                CambiarEstado(EstadoSesion.Listo);
            }

            if (NetworkManager.Singleton.IsServer)
            {
                OnJugadoresCambiado?.Invoke(NetworkManager.Singleton.ConnectedClientsList.Count);
            }
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            _clientesAprobadosPendientes.Remove(clientId);
            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                CambiarEstado(EstadoSesion.Desconectado);
                if (!NetworkManager.Singleton.IsServer)
                {
                    string motivo = NetworkManager.Singleton.DisconnectReason;
                    if (!string.IsNullOrWhiteSpace(motivo))
                    {
                        if (motivo.Contains("Partida llena", StringComparison.OrdinalIgnoreCase))
                            motivo = "Partida llena";
                        else if (
                            motivo.Contains("Partida en curso", StringComparison.OrdinalIgnoreCase)
                        )
                            motivo = "Partida en curso";
                        OnConexionRechazada?.Invoke(motivo);
                    }
                }
            }

            if (NetworkManager.Singleton.IsServer)
            {
                OnJugadoresCambiado?.Invoke(NetworkManager.Singleton.ConnectedClientsList.Count);
            }
        }

        private void HandleClientStarted()
        {
            var escenas = NetworkManager.Singleton.SceneManager;
            if (escenas == null)
                return;

            escenas.OnSceneEvent -= HandleSceneEvent;
            escenas.OnSceneEvent += HandleSceneEvent;
        }

        private void HandleClientStopped(bool eraHost)
        {
            Debug.Log($"[Network ClientStopped] eraHost={eraHost}");
            DesuscribirEventosEscena();
        }

        private void DesuscribirEventosEscena()
        {
            var escenas = NetworkManager.Singleton.SceneManager;
            if (escenas != null)
                escenas.OnSceneEvent -= HandleSceneEvent;
        }

        private void HandleSceneEvent(SceneEvent evento)
        {
            Debug.Log(
                $"[Network Scene Event] tipo={evento.SceneEventType}, cliente={evento.ClientId}"
            );
            switch (evento.SceneEventType)
            {
                case SceneEventType.Load
                    when evento.ClientId == NetworkManager.Singleton.LocalClientId:
                    OnCargaEscenaIniciada?.Invoke();
                    break;
                case SceneEventType.LoadEventCompleted:
                    OnCargaEscenaCompletada?.Invoke();
                    break;
            }
        }

        private void HandleTransportFailure()
        {
            CambiarEstado(EstadoSesion.Error);
        }

        private void CambiarEstado(EstadoSesion nuevoEstado)
        {
            _status.Marcar(nuevoEstado);
            OnEstadoCambiado?.Invoke(nuevoEstado);
        }

        public string GetLocalIPAddress()
        {
            try
            {
                using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
                socket.Connect("8.8.8.8", 65530);
                string ip = (socket.LocalEndPoint as IPEndPoint)?.Address.ToString();
                Debug.Log(ip);
                if (!string.IsNullOrEmpty(ip))
                    return ip;
            }
            catch
            {
                // Fallback si la red LAN no tiene conexión externa
            }

            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                    {
                        return ip.ToString();
                    }
                }
                Debug.Log(host.ToString());
            }
            catch (Exception e)
            {
                Debug.LogWarning($"No se pudo obtener la IP local: {e.Message}");
            }

            return "No encontrada";
        }
    }
}
