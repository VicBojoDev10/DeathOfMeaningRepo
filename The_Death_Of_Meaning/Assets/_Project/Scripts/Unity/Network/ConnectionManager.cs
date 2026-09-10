using System;
using System.Net;
using System.Net.Sockets;
using TDOM.Gameplay.Core;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace TDOM.Unity
{
    public class ConnectionManager : MonoBehaviour
    {
        [SerializeField]
        private GameObject gameFlowNetworkPrefab;
        private readonly SessionStatus _status = new();
        public event Action<EstadoSesion> OnEstadoCambiado;
        public event Action<int> OnJugadoresCambiado;

        private void OnEnable()
        {
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
            NetworkManager.Singleton.OnServerStarted += HandleServerStarted;
            NetworkManager.Singleton.OnTransportFailure += HandleTransportFailure;
        }

        private void OnDisable()
        {
            if (NetworkManager.Singleton == null)
                return;

            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
            NetworkManager.Singleton.OnServerStarted -= HandleServerStarted;
            NetworkManager.Singleton.OnTransportFailure -= HandleTransportFailure;
        }

        public void OnCrearPartida()
        {
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

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.ConnectionData.Address = ip;
            transport.ConnectionData.Port = 7777;

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
            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                CambiarEstado(EstadoSesion.Desconectado);
            }

            if (NetworkManager.Singleton.IsServer)
            {
                OnJugadoresCambiado?.Invoke(NetworkManager.Singleton.ConnectedClientsList.Count);
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
                return (socket.LocalEndPoint as IPEndPoint)?.Address.ToString() ?? "No encontrada";
            }
            catch (Exception e)
            {
                Debug.LogWarning($"No se pudo obtener la IP local: {e.Message}");
                return "No encontrada";
            }
        }
    }
}
