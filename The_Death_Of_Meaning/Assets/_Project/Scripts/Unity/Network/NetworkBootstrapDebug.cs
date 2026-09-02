using Unity.Netcode;
using UnityEngine;
namespace TDOM.Unity.Network
{
    public class NetworkBootstrapDebug : MonoBehaviour
    {
        private void OnGUI()
        {
            if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer)
            {
                GUILayout.Label($"Host: {NetworkManager.Singleton.IsHost}");
                GUILayout.Label($"Clientes: {NetworkManager.Singleton.ConnectedClientsIds.Count}");
                return;
            }
            if (GUILayout.Button("Host"))   NetworkManager.Singleton.StartHost();
            if (GUILayout.Button("Client")) NetworkManager.Singleton.StartClient();
        }
    }
}
