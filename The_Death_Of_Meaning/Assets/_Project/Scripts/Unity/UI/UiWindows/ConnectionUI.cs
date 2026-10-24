using System;
using System.Collections;
using TDOM.Gameplay.Core;
using TDOM.Unity.UI;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TDOM.Unity
{
    public class ConnectionUI : UIWindow
    {
        public ConnectionManager ConnectionManager => ConnectionManager.Instance;
        public TMP_InputField ipInputField;
        public TextMeshProUGUI hostIpDisplayText;
        public TextMeshProUGUI statusText;
        public Button startHostButton;
        public Button startClientButton;
        public Button startMatchButton;
        public Button joinTest;
        [SerializeField]
        private CancelHostUI cancelHostPrefab;

        private float _lastKeyboardCloseTime = -1f;
        private bool _connectionAttemptStarted;
        private CancelHostUI _cancelHostUI;

        private void Awake()
        {
            if (firstSelectedObject == null && startHostButton != null)
                firstSelectedObject = startHostButton.gameObject;
        }

        public override void Initialize() => base.Initialize();

        void Start()
        {
            Initialize();
            ActualizarBotonComenzar(
                NetworkManager.Singleton != null
                    ? NetworkManager.Singleton.ConnectedClientsList.Count
                    : 0
            );
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += AlCargarEscena;

            joinTest.onClick.AddListener(JoinTestUi);
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

            if (ConnectionManager != null)
            {
                ConnectionManager.OnEstadoCambiado += ActualizarTextoEstado;
                ConnectionManager.OnJugadoresCambiado += ActualizarBotonComenzar;
                ConnectionManager.OnConexionRechazada += MostrarMotivoRechazo;
                ActualizarBotonComenzar(
                    NetworkManager.Singleton != null
                        ? NetworkManager.Singleton.ConnectedClientsList.Count
                        : 0
                );
            }

            _cancelHostUI = FindFirstObjectByType<CancelHostUI>(FindObjectsInactive.Include);
            if (_cancelHostUI == null && cancelHostPrefab != null)
            {
                _cancelHostUI = Instantiate(cancelHostPrefab, Vector3.zero, Quaternion.identity);
                _cancelHostUI.Initialize();
            }
            var cancelCanvas = _cancelHostUI != null
                ? _cancelHostUI.GetComponentInChildren<Canvas>(true)
                : null;
            if (cancelCanvas != null)
            {
                cancelCanvas.overrideSorting = true;
                cancelCanvas.sortingOrder = 100;
            }

            if (ConnectionManager != null)
                ActualizarTextoEstado(ConnectionManager.EstadoActual);

            GameFlowNetwork.Spawned += SuscribirseAGameFlow;
            if (GameFlowNetwork.Instance != null)
                SuscribirseAGameFlow();
        }

        private void JoinTestUi()
        {
            if (ConnectionManager != null)
                ConnectionManager.OnUnirse("127.0.0.1");
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= AlCargarEscena;

            if (_cancelHostUI != null && _cancelHostUI.IsOpen)
                _cancelHostUI.Hide();

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

            if (ConnectionManager != null)
            {
                ConnectionManager.OnEstadoCambiado -= ActualizarTextoEstado;
                ConnectionManager.OnJugadoresCambiado -= ActualizarBotonComenzar;
                ConnectionManager.OnConexionRechazada -= MostrarMotivoRechazo;
            }

            GameFlowNetwork.Spawned -= SuscribirseAGameFlow;
            if (GameFlowNetwork.Instance != null)
                GameFlowNetwork.Instance.OnCharacterSelectionStarted -= IrASeleccionDePersonaje;
        }

        private void AlCargarEscena(Scene escena, LoadSceneMode modo)
        {
            if (escena.name == "NetworkSetup")
                StartCoroutine(RestaurarFocoTrasCambioDeEscena());
        }

        private IEnumerator RestaurarFocoTrasCambioDeEscena()
        {
            yield return null;
            yield return null;
            RestaurarFocoControl();
        }

        private void Update()
        {
            if (!IsOpen || EventSystem.current == null || _connectionAttemptStarted)
                return;

            var seleccionado = EventSystem.current.currentSelectedGameObject;
            var selectable = seleccionado != null ? seleccionado.GetComponent<Selectable>() : null;
            if (selectable != null && selectable.IsInteractable())
                return;

            RestaurarFocoControl();
        }

        private void RestaurarFocoControl()
        {
            if (!IsOpen || EventSystem.current == null || _connectionAttemptStarted)
                return;

            Selectable objetivo = null;
            if (
                startMatchButton != null
                && startMatchButton.gameObject.activeInHierarchy
                && startMatchButton.IsInteractable()
            )
                objetivo = startMatchButton;
            else if (startHostButton != null && startHostButton.IsInteractable())
                objetivo = startHostButton;
            else if (
                NetworkManager.Singleton != null
                && NetworkManager.Singleton.IsHost
                && joinTest != null
                && joinTest.IsInteractable()
            )
                objetivo = joinTest;
            else if (startClientButton != null && startClientButton.IsInteractable())
                objetivo = startClientButton;
            else if (joinTest != null && joinTest.IsInteractable())
                objetivo = joinTest;

            if (
                objetivo != null
                && EventSystem.current.currentSelectedGameObject != objetivo.gameObject
            )
                EventSystem.current.SetSelectedGameObject(objetivo.gameObject);
        }

        private void HandleKeyboardClosed()
        {
            _lastKeyboardCloseTime = Time.unscaledTime;
            SetInteractable(true);
        }

        private void OnInputFieldSelect(string _)
        {
            if (_connectionAttemptStarted)
                return;

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
            if (_connectionAttemptStarted || UiManager.Instance == null)
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
            CerrarTecladoVirtual();
            if (_cancelHostUI != null && _cancelHostUI.IsOpen)
                _cancelHostUI.Hide();
            UiManager.Instance.SwitchWindow(WindowId, WindowsIds.ChSelectionUI);
        }

        private void CerrarTecladoVirtual()
        {
            var kbWindow =
                UiManager.Instance != null
                    ? UiManager.Instance.GetWindow(WindowsIds.KeyboardUI) as KeyboardUI
                    : null;
            if (kbWindow != null && kbWindow.IsOpen)
                kbWindow.CerrarTeclado();
        }

        private void SuscribirseAGameFlow()
        {
            GameFlowNetwork.Instance.OnCharacterSelectionStarted += IrASeleccionDePersonaje;
        }

        private void ActualizarBotonComenzar(int cantPlayers)
        {
            bool esHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;
            bool habilitado = esHost && cantPlayers >= 2;
            startMatchButton.interactable = habilitado;
            startMatchButton.gameObject.SetActive(habilitado);

            if (habilitado)
            {
                SetInteractable(true);
                _connectionAttemptStarted = false;
                if (_cancelHostUI != null && _cancelHostUI.IsOpen)
                    _cancelHostUI.Hide();
                RestaurarFocoControl();
            }
            else if (
                esHost
                && ConnectionManager != null
                && ConnectionManager.EstadoActual == EstadoSesion.Listo
                && _cancelHostUI != null
                && !_cancelHostUI.IsOpen
            )
            {
                _cancelHostUI.Show();
            }
        }

        private void MostrarMotivoRechazo(string motivo)
        {
            if (statusText != null && !string.IsNullOrWhiteSpace(motivo))
                statusText.text = motivo;
        }

        private void StartMatchButton_OnClick()
        {
            GameFlowNetwork.Instance.StartMatch();
        }

        private void StartHostButton_OnClick()
        {
            _connectionAttemptStarted = true;
            ConnectionManager.OnCrearPartida();
            if (hostIpDisplayText != null)
                hostIpDisplayText.text = $"IP: {ConnectionManager.GetLocalIPAddress()}";
        }

        private void StartClientButton_OnClick()
        {
            _connectionAttemptStarted = true;
            ConnectionManager.OnUnirse(ipInputField.text.Trim());
        }

        private void ActualizarTextoEstado(EstadoSesion estado)
        {
            _connectionAttemptStarted = estado == EstadoSesion.Conectando
                || estado == EstadoSesion.Listo
                || estado == EstadoSesion.EsperandoJugador;
            CerrarTecladoVirtual();

            if (statusText != null)
                statusText.text = estado switch
            {
                EstadoSesion.Conectando => "Conectando...",
                EstadoSesion.Listo => "Listo",
                EstadoSesion.Desconectado => "Desconectado",
                EstadoSesion.Error => "Error de conexión",
                EstadoSesion.Cancelada => "Conexión cancelada",
                _ => estado.ToString(),
            };

            bool activo = estado == EstadoSesion.Conectando || estado == EstadoSesion.Listo
                || estado == EstadoSesion.EsperandoJugador;
            if (startHostButton != null)
                startHostButton.interactable = !activo;
            if (startClientButton != null)
                startClientButton.interactable = !activo;

            if (_cancelHostUI != null)
            {
                bool esperandoConexion = estado == EstadoSesion.Conectando
                    || (estado == EstadoSesion.Listo
                        && NetworkManager.Singleton != null
                        && NetworkManager.Singleton.IsHost
                        && NetworkManager.Singleton.ConnectedClientsList.Count < 2);

                if (esperandoConexion && !_cancelHostUI.IsOpen)
                    _cancelHostUI.Show();
                else if (!esperandoConexion && _cancelHostUI.IsOpen)
                    _cancelHostUI.Hide();
            }

            if (!activo)
            {
                _connectionAttemptStarted = false;
                if (
                    ipInputField != null
                    && estado == EstadoSesion.Cancelada
                    && ConnectionManager != null
                    && !ConnectionManager.UltimoIntentoFueCliente
                )
                    ipInputField.text = string.Empty;
                if (
                    hostIpDisplayText != null
                    && estado == EstadoSesion.Cancelada
                    && ConnectionManager != null
                    && !ConnectionManager.UltimoIntentoFueCliente
                )
                    hostIpDisplayText.text = string.Empty;
                if (EventSystem.current != null)
                    RestaurarFocoControl();
            }
        }
    }
}
