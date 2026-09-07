using TDOM.Gameplay.Core;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;

namespace TDOM.Unity
{
    public class ConnectionManager : MonoBehaviour
    {
        private readonly SessionStatus _status = new();
        public event Action<EstadoSesion> OnEstadoCambiado;

    private void OnEnable()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
        NetworkManager.Singleton.OnServerStarted += HandleServerStarted;
        NetworkManager.Singleton.OnTransportFailure += HandleTransportFailure;
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton == null) return;

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
    }

    private void HandleClientConnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            CambiarEstado(EstadoSesion.Listo);
        }
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            CambiarEstado(EstadoSesion.Desconectado);
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
