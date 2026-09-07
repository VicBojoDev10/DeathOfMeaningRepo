using System;
using TDOM.Gameplay.Core;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace TDOM.Unity
{
    public class ConnectionUI : UIWindow
    {
        public ConnectionManager connectionManager;
        public TMP_InputField ipInputField;
        public TextMeshProUGUI hostIpDisplayText;
        public TextMeshProUGUI statusText;
        public Button startHostButton;
        public Button startClientButton;
        public Button startMatchButton;

        void Start()
        {
            Show();
            startMatchButton.gameObject.SetActive(false);
        }


        private void OnEnable()
        {
            startHostButton.onClick.AddListener(StartHostButton_OnClick);
            startClientButton.onClick.AddListener(StartClientButton_OnClick);
            startMatchButton.onClick.AddListener(StartMatchButton_OnClick);
            connectionManager.OnEstadoCambiado += ActualizarTextoEstado;
            connectionManager.OnJugadoresCambiado += ActualizarBotonComenzar;
            GameFlowNetwork.Spawned += SuscribirseAGameFlow;
            if (GameFlowNetwork.Instance != null) SuscribirseAGameFlow();
        }
        private void OnDisable()
        {
            startHostButton.onClick.RemoveListener(StartHostButton_OnClick);
            startClientButton.onClick.RemoveListener(StartClientButton_OnClick);
            startMatchButton.onClick.RemoveListener(StartMatchButton_OnClick);

            connectionManager.OnEstadoCambiado -= ActualizarTextoEstado;
            connectionManager.OnJugadoresCambiado -= ActualizarBotonComenzar;

            GameFlowNetwork.Spawned -= SuscribirseAGameFlow;
            if (GameFlowNetwork.Instance != null)
                GameFlowNetwork.Instance.OnCharacterSelectionStarted -= IrASeleccionDePersonaje;
        }

        private void IrASeleccionDePersonaje()
        {
            Hide();
            //Mostrar la ui window de personaje

        }

        private void SuscribirseAGameFlow()
        {
            GameFlowNetwork.Instance.OnCharacterSelectionStarted += IrASeleccionDePersonaje;
        }

        private void ActualizarBotonComenzar(int cantPlayers)
        {
            bool esHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;
            startMatchButton.gameObject.SetActive(esHost && cantPlayers >= 2);
        }

        private void StartMatchButton_OnClick()
        {
            GameFlowNetwork.Instance.StartMatch();
        }
        private void StartHostButton_OnClick()
        {
            connectionManager.OnCrearPartida();
            hostIpDisplayText.text = $"IP: {connectionManager.GetLocalIPAddress()}";
            startHostButton.onClick.RemoveAllListeners();
        }

        private void StartClientButton_OnClick()
        {
            connectionManager.OnUnirse(ipInputField.text.Trim());
            startClientButton.onClick.RemoveAllListeners();
        }
        private void ActualizarTextoEstado(EstadoSesion estado)
        {
            statusText.text = estado switch
            {
                EstadoSesion.Conectando => "Conectando...",
                EstadoSesion.Listo => "Listo",
                EstadoSesion.Desconectado => "Desconectado",
                EstadoSesion.Error => "Error de conexión",
                _ => estado.ToString()
            };
        }

    }
}
