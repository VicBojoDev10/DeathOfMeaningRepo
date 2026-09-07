using System;
using TDOM.Gameplay.Core;
using TMPro;
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

        void Start()
        {
            Show();
        }


        private void OnEnable()
        {
            startHostButton.onClick.AddListener(StartHostButton_OnClick);
            startClientButton.onClick.AddListener(StartClientButton_OnClick);
            connectionManager.OnEstadoCambiado += ActualizarTextoEstado;
        }

        private void OnDisable()
        {
            startHostButton.onClick.RemoveListener(StartHostButton_OnClick);
            startClientButton.onClick.RemoveListener(StartClientButton_OnClick);
            connectionManager.OnEstadoCambiado -= ActualizarTextoEstado;
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
