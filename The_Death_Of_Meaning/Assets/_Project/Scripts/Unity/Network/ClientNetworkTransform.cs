using Unity.Netcode.Components;
using UnityEngine;

namespace TDOM.Unity
{
    [DisallowMultipleComponent]
    public class ClientNetworkTransform : NetworkTransform
    {
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsOwner)
                CanCommitToTransform = false;
        }

        public void HabilitarAutoridadDelDueño()
        {
            if (IsSpawned && IsOwner)
                CanCommitToTransform = true;
        }

        protected override void OnNetworkTransformStateUpdated(
            ref NetworkTransformState oldState,
            ref NetworkTransformState newState
        )
        {
            Vector3 position = newState.GetPosition();
            if (newState.HasPositionChange && new Vector2(position.x, position.z).sqrMagnitude < 9f)
            {
                Debug.LogError(
                    $"[SPAWN][NET-STATE-NEAR-ORIGIN] local={NetworkManager.LocalClientId} "
                        + $"owner={NetworkObject.OwnerClientId} position={position} "
                        + $"tick={newState.GetNetworkTick()}"
                );
            }

            base.OnNetworkTransformStateUpdated(ref oldState, ref newState);
        }

        protected override bool OnIsServerAuthoritative()
        {
            return false;
        }
    }
}
