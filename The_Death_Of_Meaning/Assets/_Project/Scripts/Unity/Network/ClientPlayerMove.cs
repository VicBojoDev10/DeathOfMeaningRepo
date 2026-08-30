using System;
using StarterAssets;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class ClientPlayerMove : NetworkBehaviour
{
    [SerializeField]
    private PlayerInput m_playerInput;

    [SerializeField]
    private StarterAssetsInputs m_StarterAssetsInputs;

    [SerializeField]
    private ThirdPersonController m_ThirdPersonController;

    private void Awake()
    {
        m_StarterAssetsInputs.enabled = false;
        m_playerInput.enabled = false;
        m_ThirdPersonController.enabled = false;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsOwner)
        {
            m_StarterAssetsInputs.enabled = true;
            m_playerInput.enabled = true;
            m_ThirdPersonController.enabled = true;
        }
    }
}
