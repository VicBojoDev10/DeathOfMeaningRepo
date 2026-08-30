using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace _Project.Scripts.Unity.UI
{
    public class ConnectionMenuUI : MonoBehaviour
    {
        private readonly SessionStatus _status = new();

        public void OncrearPartida()
        {
            _status.Marcar((EstadoSession.Conectando));
            NetworkManager.Singleton.StartHost();
        }

        public void OnUnirse(string ip)
        {
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.ConnectionData.Address = ip;
            transport.ConnectionData.Port = 7777;

            _status.Marcar(EstadoSession.Conectando);
            NetworkManager.Singleton.StartClient();
        }
    }
}
