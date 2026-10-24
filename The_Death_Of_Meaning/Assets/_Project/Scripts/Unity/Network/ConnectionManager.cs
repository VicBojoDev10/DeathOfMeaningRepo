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
        private const float TiempoMaxCierreRed = 3f;

        [SerializeField]
        private GameObject gameFlowNetworkPrefab;

        [Header("Tiempos de espera")]
        [Tooltip("Segundos que un cliente espera respuesta del host antes de cancelar solo.")]
        [SerializeField, Min(1f)]
        private float tiempoLimiteCliente = 10f;

        [Tooltip(
            "Segundos que un host espera a que entre un jugador antes de cerrar la partida. 0 = sin límite."
        )]
        [SerializeField, Min(0f)]
        private float tiempoEsperaHost = 0f;

        private readonly SessionStatus _status = new();
        private readonly HashSet<ulong> _clientesAprobadosPendientes = new();
        public event Action<EstadoSesion> OnEstadoCambiado;
        public event Action<int> OnJugadoresCambiado;
        public event Action OnCargaEscenaIniciada;
        public event Action OnCargaEscenaCompletada;
        public event Action<string> OnConexionRechazada;

        public event Action<int> OnTiempoRestanteCambiado;
        public EstadoSesion EstadoActual => _status.Estado;

        private bool _isSubscribed;
        private float _finTiempoLimiteConexion = -1f;
        private bool _cerrandoRed;
        private bool _intentoComoCliente;
        private int _ultimoSegundoNotificado = -1;

        public bool CerrandoRed => _cerrandoRed;

        public bool PuedeCancelar => !_cerrandoRed && EstadoEsActivo(_status.Estado);

        public bool ClienteEsperandoConexion =>
            _intentoComoCliente && _status.Estado == EstadoSesion.Conectando;
        public bool UltimoIntentoFueCliente => _intentoComoCliente;

        public bool TemporizadorActivo => _finTiempoLimiteConexion >= 0f;
        public int SegundosRestantesConexion =>
            _finTiempoLimiteConexion < 0f
                ? 0
                : Mathf.CeilToInt(Mathf.Max(0f, _finTiempoLimiteConexion - Time.unscaledTime));

        public static bool EstadoEsActivo(EstadoSesion estado) =>
            estado == EstadoSesion.Conectando
            || estado == EstadoSesion.Listo
            || estado == EstadoSesion.EsperandoJugador;

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

            _cerrandoRed = false;
            DesuscribirEventosNetwork();
        }

        private void OnDestroy()
        {
            DesuscribirEventosNetwork();
        }

        private void Update()
        {
            if (_finTiempoLimiteConexion < 0f)
                return;

            NotificarTiempo();
            if (_cerrandoRed || Time.unscaledTime < _finTiempoLimiteConexion)
                return;

            DetenerTimeout();
            var networkManager = NetworkManager.Singleton;

            if (_intentoComoCliente && _status.Estado == EstadoSesion.Conectando)
            {
                CancelarConexion("No se encontró la partida. Puedes intentar de nuevo.", true);
            }
            else if (
                !_intentoComoCliente
                && _status.Estado == EstadoSesion.Listo
                && networkManager != null
                && networkManager.IsServer
                && JugadoresConectados() < 2
            )
            {
                CancelarConexion("Se agotó el tiempo de espera. Puedes crear la partida de nuevo.", true);
            }
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
            if (!PrepararIntento(false))
                return;

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
            if (!PrepararIntento(true))
                return;

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
            EstablecerLimite(tiempoLimiteCliente);
            CambiarEstado(EstadoSesion.Conectando);
            if (!NetworkManager.Singleton.StartClient())
                FallarIntento("No se pudo iniciar la conexión.");
        }

        private bool PrepararIntento(bool comoCliente)
        {
            var networkManager = NetworkManager.Singleton;
            if (_cerrandoRed || (networkManager != null && networkManager.IsListening))
            {

                Debug.LogWarning(
                    "[ConnectionManager] Intento ignorado: la red está activa o cerrándose todavía."
                );
                return false;
            }

            _intentoComoCliente = comoCliente;
            DetenerTimeout();
            return true;
        }

        private void EstablecerLimite(float segundos)
        {
            _finTiempoLimiteConexion = Time.unscaledTime + segundos;
            NotificarTiempo();
        }

        private void DetenerTimeout()
        {
            _finTiempoLimiteConexion = -1f;
            NotificarTiempo();
        }

        private void IniciarTemporizadorHost()
        {
            if (tiempoEsperaHost > 0f)
                EstablecerLimite(tiempoEsperaHost);
        }

        private void NotificarTiempo()
        {
            int segundos = _finTiempoLimiteConexion < 0f ? -1 : SegundosRestantesConexion;
            if (segundos == _ultimoSegundoNotificado)
                return;

            _ultimoSegundoNotificado = segundos;
            OnTiempoRestanteCambiado?.Invoke(segundos);
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
            if (_cerrandoRed)
                return;

            var networkManager = NetworkManager.Singleton;
            bool redActiva =
                networkManager != null
                && (networkManager.IsListening || networkManager.ShutdownInProgress);
            if (!redActiva && !EstadoEsActivo(_status.Estado))
                return;

            Debug.Log(
                $"[ConnectionManager] Cancelando. estado={_status.Estado}, cliente={_intentoComoCliente}, redActiva={redActiva}"
            );
            CerrarRed(EstadoSesion.Cancelada, mostrarMensaje ? mensaje : null, false);
        }

        private void CerrarRed(EstadoSesion estadoFinal, string mensaje, bool diferir)
        {
            if (_cerrandoRed)
                return;

            _cerrandoRed = true;
            DetenerTimeout();
            StartCoroutine(CerrarRedCoroutine(estadoFinal, mensaje, diferir));
        }

        private IEnumerator CerrarRedCoroutine(EstadoSesion estadoFinal, string mensaje, bool diferir)
        {

            if (diferir)
                yield return null;

            var networkManager = NetworkManager.Singleton;
            if (networkManager != null && networkManager.IsListening && !networkManager.ShutdownInProgress)
            {
                if (networkManager.IsServer)
                    DespawnGameFlow();
                networkManager.Shutdown();
            }

            float limite = Time.unscaledTime + TiempoMaxCierreRed;
            while (
                networkManager != null
                && (networkManager.IsListening || networkManager.ShutdownInProgress)
            )
            {
                if (Time.unscaledTime > limite)
                {
                    Debug.LogWarning(
                        "[ConnectionManager] NGO tardó demasiado en cerrar; se continúa de todos modos."
                    );
                    break;
                }
                yield return null;
            }

            _clientesAprobadosPendientes.Clear();
            _cerrandoRed = false;

            OnJugadoresCambiado?.Invoke(0);
            CambiarEstado(estadoFinal);
            if (!string.IsNullOrWhiteSpace(mensaje))
                OnConexionRechazada?.Invoke(mensaje);
        }

        private static void DespawnGameFlow()
        {
            if (GameFlowNetwork.Instance == null)
                return;

            var networkObject = GameFlowNetwork.Instance.NetworkObject;
            if (networkObject != null && networkObject.IsSpawned)
                networkObject.Despawn(true);
        }

        private int JugadoresConectados(ulong? excluir = null)
        {
            var networkManager = NetworkManager.Singleton;
            if (networkManager == null)
                return 0;

            int total = 0;
            foreach (var cliente in networkManager.ConnectedClientsList)
                if (excluir == null || cliente.ClientId != excluir.Value)
                    total++;
            return total;
        }

        private void HandleServerStarted()
        {
            if (_cerrandoRed)
                return;

            CambiarEstado(EstadoSesion.Listo);
            if (NetworkManager.Singleton.IsServer)
            {
                var flowObj = Instantiate(gameFlowNetworkPrefab);
                flowObj.GetComponent<NetworkObject>().Spawn();
            }
        }

        private void HandleClientConnected(ulong clientId)
        {
            var networkManager = NetworkManager.Singleton;
            _clientesAprobadosPendientes.Remove(clientId);
            if (_cerrandoRed)
                return;

            if (clientId == networkManager.LocalClientId)
            {
                DetenerTimeout();
                CambiarEstado(EstadoSesion.Listo);
            }

            if (networkManager.IsServer)
            {
                int jugadores = networkManager.ConnectedClientsList.Count;
                if (jugadores >= 2)
                    DetenerTimeout();
                else
                    IniciarTemporizadorHost();
                OnJugadoresCambiado?.Invoke(jugadores);
            }
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            _clientesAprobadosPendientes.Remove(clientId);
            if (_cerrandoRed)
                return;

            var networkManager = NetworkManager.Singleton;

            if (clientId == networkManager.LocalClientId)
            {
                bool esperandoPrimeraConexion =
                    _intentoComoCliente && _status.Estado == EstadoSesion.Conectando;
                string motivo = networkManager.DisconnectReason;

                if (esperandoPrimeraConexion && string.IsNullOrWhiteSpace(motivo))
                    return;

                if (!networkManager.IsServer)
                {
                    if (motivo != null)
                    {
                        if (motivo.Contains("Partida llena", StringComparison.OrdinalIgnoreCase))
                            motivo = "Partida llena";
                        else if (
                            motivo.Contains("Partida en curso", StringComparison.OrdinalIgnoreCase)
                        )
                            motivo = "Partida en curso";
                    }
                    if (string.IsNullOrWhiteSpace(motivo))
                        motivo = "Se perdió la conexión con la partida.";
                    CerrarRed(EstadoSesion.Error, motivo, true);
                }
                else
                {
                    CerrarRed(EstadoSesion.Error, null, true);
                }
                return;
            }

            if (networkManager.IsServer)
            {

                int jugadores = JugadoresConectados(clientId);
                if (jugadores < 2)
                    IniciarTemporizadorHost();
                OnJugadoresCambiado?.Invoke(jugadores);
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
            if (_cerrandoRed)
                return;

            bool esperandoPrimeraConexion =
                _intentoComoCliente && _status.Estado == EstadoSesion.Conectando;

            if (esperandoPrimeraConexion)
                return;

            if (_status.Estado == EstadoSesion.Conectando || _status.Estado == EstadoSesion.Listo)
                CerrarRed(EstadoSesion.Error, "Se perdió la conexión con la partida.", true);
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
            if (_cerrandoRed)
                return;

            if (_intentoComoCliente && _status.Estado == EstadoSesion.Conectando)
                return;

            CerrarRed(EstadoSesion.Error, "Falló la conexión de red.", true);
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
