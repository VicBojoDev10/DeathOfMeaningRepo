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
            Instance = this;
            Spawned?.Invoke();
        }

        public override void OnNetworkDespawn()
        {
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
    }
}
