using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TDOM.Unity
{
    public class GameFlowNetwork : NetworkBehaviour
    {
        public static event Action Spawned;
        public static GameFlowNetwork Instance { get; private set; }
        public event Action OnCharacterSelectionStarted;

        public override void OnNetworkSpawn()
        {
            Debug.Log(
                $"[NetSpawn GameFlowNetwork] server={IsServer}, client={IsClient}, "
                + $"id={NetworkObjectId}, escena={gameObject.scene.name}"
            );

            Instance = this;
            Spawned?.Invoke();
        }

        public override void OnNetworkDespawn()
        {
            Debug.Log(
                $"[NetDespawn GameFlowNetwork] server={IsServer}, client={IsClient}, "
                + $"id={NetworkObjectId}, escena={gameObject.scene.name}"
            );

            if (Instance == this)
                Instance = null;
        }

        public void StartMatch()
        {
            if (!IsServer)
                return;
            StartMatchClientRpc();
        }

        public void CargarEscenaEnRed(string nombreEscena)
        {
            if (!IsServer || NetworkManager.SceneManager == null)
                return;

            NetworkManager.SceneManager.LoadScene(nombreEscena, LoadSceneMode.Single);
        }

        [ClientRpc]
        private void StartMatchClientRpc()
        {
            OnCharacterSelectionStarted?.Invoke();
        }

        public void SolicitarReinicioDePartida()
        {
            SolicitarReinicioDePartidaRpc();
        }

        [Rpc(SendTo.Server, RequireOwnership = false)]
        private void SolicitarReinicioDePartidaRpc(RpcParams rpcParams = default)
        {
            Debug.Log(
                $"[Restart RPC server] recibido; sender={rpcParams.Receive.SenderClientId}, "
                + $"GameSessionManager={GameSessionManager.Instance != null}"
            );

            if (!IsServer)
                return;

            GameSessionManager.Instance?.SolicitarReinicioDePartidaDesdeServidor();
        }
    }
}
