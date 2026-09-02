using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace TDOM.Unity
{
    public sealed class ConnectionMenuUI : MonoBehaviour
    {
        private readonly SessionStatus _status = new();

        public void OnCrearPartida()
        {
            _status.Marcar(EstadoSesion.Conectando);
            NetworkManager.Singleton.StartHost();
        }

        public void OnUnirse(string ip)
        {
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.ConnectionData.Address = ip;
            transport.ConnectionData.Port = 7777;

            _status.Marcar(EstadoSesion.Conectando);
            NetworkManager.Singleton.StartClient();
        }
    }
}
