using System;
using TDOM.Gameplay.Core;
using TDOM.Unity.UI;
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

        private float _lastKeyboardCloseTime = -1f;

        private void Awake()
        {
            if (firstSelectedObject == null && startHostButton != null)
                firstSelectedObject = startHostButton.gameObject;
        }

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

            if (ipInputField != null)
            {
                ipInputField.onSelect.AddListener(OnInputFieldSelect);
                ipInputField.onSubmit.AddListener(OnInputFieldSubmit);
            }

            var kbWindow =
                UiManager.Instance != null
                    ? UiManager.Instance.GetWindow(WindowsIds.KeyboardUI) as KeyboardUI
                    : null;
            if (kbWindow != null)
            {
                kbWindow.OnKeyboardClosed += HandleKeyboardClosed;
            }

            connectionManager.OnEstadoCambiado += ActualizarTextoEstado;
            connectionManager.OnJugadoresCambiado += ActualizarBotonComenzar;
            GameFlowNetwork.Spawned += SuscribirseAGameFlow;
            if (GameFlowNetwork.Instance != null)
                SuscribirseAGameFlow();
        }

        private void OnDisable()
        {
            startHostButton.onClick.RemoveListener(StartHostButton_OnClick);
            startClientButton.onClick.RemoveListener(StartClientButton_OnClick);
            startMatchButton.onClick.RemoveListener(StartMatchButton_OnClick);

            if (ipInputField != null)
            {
                ipInputField.onSelect.RemoveListener(OnInputFieldSelect);
                ipInputField.onSubmit.RemoveListener(OnInputFieldSubmit);
            }

            var kbWindow =
                UiManager.Instance != null
                    ? UiManager.Instance.GetWindow(WindowsIds.KeyboardUI) as KeyboardUI
                    : null;
            if (kbWindow != null)
            {
                kbWindow.OnKeyboardClosed -= HandleKeyboardClosed;
            }

            connectionManager.OnEstadoCambiado -= ActualizarTextoEstado;
            connectionManager.OnJugadoresCambiado -= ActualizarBotonComenzar;

            GameFlowNetwork.Spawned -= SuscribirseAGameFlow;
            if (GameFlowNetwork.Instance != null)
                GameFlowNetwork.Instance.OnCharacterSelectionStarted -= IrASeleccionDePersonaje;
        }

        private void HandleKeyboardClosed()
        {
            _lastKeyboardCloseTime = Time.unscaledTime;
            SetInteractable(true);
        }

        private void OnInputFieldSelect(string _)
        {
            if (Time.unscaledTime - _lastKeyboardCloseTime < 0.4f)
                return;

            AbrirTecladoVirtual();
        }

        private void OnInputFieldSubmit(string _)
        {
            AbrirTecladoVirtual();
        }

        public void AbrirTecladoVirtual()
        {
            if (UiManager.Instance == null)
                return;

            var kbWindow = UiManager.Instance.GetWindow(WindowsIds.KeyboardUI) as KeyboardUI;
            if (kbWindow != null)
            {
                if (kbWindow.IsOpen)
                    return;

                kbWindow.OnKeyboardClosed -= HandleKeyboardClosed;
                kbWindow.OnKeyboardClosed += HandleKeyboardClosed;

                kbWindow.AbrirParaInputField(ipInputField, this);
            }
            else
            {
                Debug.LogWarning(
                    "[ConnectionUI] No se encontró la ventana KeyboardUI en UiManager."
                );
            }
        }

        private void IrASeleccionDePersonaje()
        {
            Hide();
            UiManager.Instance.ShowWindow(WindowsIds.ChSelectionUI);
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
            startHostButton.interactable = false;
            startClientButton.interactable = false;
        }

        private void StartClientButton_OnClick()
        {
            connectionManager.OnUnirse(ipInputField.text.Trim());
            startHostButton.interactable = false;
            startClientButton.interactable = false;
        }

        private void ActualizarTextoEstado(EstadoSesion estado)
        {
            statusText.text = estado switch
            {
                EstadoSesion.Conectando => "Conectando...",
                EstadoSesion.Listo => "Listo",
                EstadoSesion.Desconectado => "Desconectado",
                EstadoSesion.Error => "Error de conexión",
                _ => estado.ToString(),
            };

            if (estado == EstadoSesion.Desconectado || estado == EstadoSesion.Error)
            {
                startHostButton.interactable = true;
                startClientButton.interactable = true;
            }
        }
    }
}
