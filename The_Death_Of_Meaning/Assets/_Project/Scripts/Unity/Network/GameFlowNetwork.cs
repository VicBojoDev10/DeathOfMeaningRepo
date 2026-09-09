using System;
using Unity.Netcode;
using UnityEngine;

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

        [ClientRpc]
        private void StartMatchClientRpc()
        {
            OnCharacterSelectionStarted?.Invoke();
        }
    }
}
