using System;
using System.Collections;
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
        private const float TiempoLimiteConexion = 10f;
        [SerializeField]
        private GameObject gameFlowNetworkPrefab;
        private readonly SessionStatus _status = new();
        private readonly HashSet<ulong> _clientesAprobadosPendientes = new();
        public event Action<EstadoSesion> OnEstadoCambiado;
        public event Action<int> OnJugadoresCambiado;
        public event Action OnCargaEscenaIniciada;
        public event Action OnCargaEscenaCompletada;
        public event Action<string> OnConexionRechazada;
        public EstadoSesion EstadoActual => _status.Estado;

        private bool _isSubscribed;
        private Coroutine _timeoutConexion;
        private float _finTiempoLimiteConexion = -1f;
        private bool _esCancelacionIntencional;
        private bool _intentoComoCliente;

        public bool ClienteEsperandoConexion =>
            _intentoComoCliente && _status.Estado == EstadoSesion.Conectando;
        public bool UltimoIntentoFueCliente => _intentoComoCliente;
        public int SegundosRestantesConexion =>
            _finTiempoLimiteConexion < 0f
                ? 0
                : Mathf.CeilToInt(Mathf.Max(0f, _finTiempoLimiteConexion - Time.unscaledTime));

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
            PrepararIntento(false);
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
            if (!NetworkManager.Singleton.StartHost())
                FallarIntento("No se pudo crear la partida.");
        }

        public void OnUnirse(string ip)
        {
            PrepararIntento(true);
            if (!IPAddress.TryParse(ip, out _))
            {
                FallarIntento("La dirección IP no es válida.");
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
            _finTiempoLimiteConexion = Time.unscaledTime + TiempoLimiteConexion;
            if (!NetworkManager.Singleton.StartClient())
            {
                _finTiempoLimiteConexion = -1f;
                FallarIntento("No se pudo iniciar la conexión.");
            }
            else
            {
                if (_status.Estado == EstadoSesion.Conectando)
                    _timeoutConexion = StartCoroutine(EsperarConexion());
            }
        }

        private void PrepararIntento(bool comoCliente)
        {
            _intentoComoCliente = comoCliente;
            _esCancelacionIntencional = false;
            DetenerTimeout();
        }

        private IEnumerator EsperarConexion()
        {
            yield return new WaitForSecondsRealtime(TiempoLimiteConexion);
            _timeoutConexion = null;
            if (_status.Estado == EstadoSesion.Conectando && _intentoComoCliente)
                CancelarConexion("No se encontró la partida. Puedes intentar de nuevo.", true);
        }

        private void DetenerTimeout()
        {
            _finTiempoLimiteConexion = -1f;
            if (_timeoutConexion == null)
                return;
            StopCoroutine(_timeoutConexion);
            _timeoutConexion = null;
        }

        private void FallarIntento(string mensaje)
        {
            DetenerTimeout();
            CambiarEstado(EstadoSesion.Error);
            OnConexionRechazada?.Invoke(mensaje);
        }

        public void CancelarConexion() => CancelarConexion("Conexión cancelada.", true);

        private void CancelarConexion(string mensaje, bool mostrarMensaje)
        {
            var networkManager = NetworkManager.Singleton;
            if (_status.Estado == EstadoSesion.Cancelada
                && (networkManager == null || !networkManager.IsListening))
                return;

            DetenerTimeout();
            _esCancelacionIntencional = true;
            if (networkManager != null && networkManager.IsListening)
            {
                if (networkManager.IsServer && GameFlowNetwork.Instance != null)
                {
                    var networkObject = GameFlowNetwork.Instance.NetworkObject;
                    if (networkObject != null && networkObject.IsSpawned)
                        networkObject.Despawn(true);
                }
                networkManager.Shutdown();
            }
            _clientesAprobadosPendientes.Clear();
            OnJugadoresCambiado?.Invoke(0);
            CambiarEstado(EstadoSesion.Cancelada);
            if (mostrarMensaje)
                OnConexionRechazada?.Invoke(mensaje);
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
            if (clientId == NetworkManager.Singleton.LocalClientId)
                DetenerTimeout();
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
                bool esperandoPrimeraConexion =
                    _intentoComoCliente && _status.Estado == EstadoSesion.Conectando;
                string motivo = NetworkManager.Singleton.DisconnectReason;

                if (_esCancelacionIntencional)
                {
                    DetenerTimeout();
                    CambiarEstado(EstadoSesion.Cancelada);
                }
                else if (!esperandoPrimeraConexion || !string.IsNullOrWhiteSpace(motivo))
                {
                    DetenerTimeout();
                    CambiarEstado(EstadoSesion.Error);
                }
                if (!NetworkManager.Singleton.IsServer)
                {
                    if (!string.IsNullOrWhiteSpace(motivo))
                    {
                        if (motivo.Contains("Partida llena", StringComparison.OrdinalIgnoreCase))
                            motivo = "Partida llena";
                        else if (
                            motivo.Contains("Partida en curso", StringComparison.OrdinalIgnoreCase)
                        )
                            motivo = "Partida en curso";
                        if (!string.IsNullOrWhiteSpace(motivo))
                            OnConexionRechazada?.Invoke(motivo);
                    }
                    else if (!_esCancelacionIntencional && !esperandoPrimeraConexion)
                    {
                        OnConexionRechazada?.Invoke("Se perdió la conexión con la partida.");
                    }

                    if (!_esCancelacionIntencional && !esperandoPrimeraConexion)
                        StartCoroutine(LimpiarRedTrasFallo());
                }
            }

            if (NetworkManager.Singleton.IsServer)
            {
                OnJugadoresCambiado?.Invoke(NetworkManager.Singleton.ConnectedClientsList.Count);
            }
        }

        private IEnumerator LimpiarRedTrasFallo()
        {
            // NGO puede seguir dentro del callback de desconexión; apagarlo al frame siguiente.
            yield return null;
            var networkManager = NetworkManager.Singleton;
            if (networkManager != null && networkManager.IsListening)
            {
                if (networkManager.IsServer && GameFlowNetwork.Instance != null)
                {
                    var networkObject = GameFlowNetwork.Instance.NetworkObject;
                    if (networkObject != null && networkObject.IsSpawned)
                        networkObject.Despawn(true);
                }
                networkManager.Shutdown();
            }
            _clientesAprobadosPendientes.Clear();
            OnJugadoresCambiado?.Invoke(0);
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
            bool esperandoPrimeraConexion =
                _intentoComoCliente && _status.Estado == EstadoSesion.Conectando;
            if (!esperandoPrimeraConexion || _esCancelacionIntencional)
                DetenerTimeout();
            DesuscribirEventosEscena();
            if (_esCancelacionIntencional)
                CambiarEstado(EstadoSesion.Cancelada);
            else if (esperandoPrimeraConexion)
            {
                // Mantener visible el contador hasta agotar los 10 segundos del intento.
            }
            else if (_status.Estado == EstadoSesion.Conectando || _status.Estado == EstadoSesion.Listo)
            {
                CambiarEstado(EstadoSesion.Error);
                OnConexionRechazada?.Invoke("Se perdió la conexión con la partida.");
            }
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
            if (!_esCancelacionIntencional)
            {
                if (_intentoComoCliente && _status.Estado == EstadoSesion.Conectando)
                    return;

                FallarIntento("Falló la conexión de red.");
                StartCoroutine(LimpiarRedTrasFallo());
            }
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
