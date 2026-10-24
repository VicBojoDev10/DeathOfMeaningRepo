using System;
using System.Collections;
using TDOM.Gameplay.Core;
using TDOM.Unity.UI;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
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

        private float _lastKeyboardCloseTime = -1f;
        private bool _connectionAttemptStarted;
        private bool _inicializado;
        private bool _navegandoAPersonaje;
        private CancelHostUI _cancelHostUI;
        private InputAction _cancelAction;
        private Coroutine _enfocandoCancelar;

        private void Awake()
        {
            if (firstSelectedObject == null && startHostButton != null)
                firstSelectedObject = startHostButton.gameObject;
        }

        public override void Initialize() => base.Initialize();

        void Start()
        {
            Initialize();
            _inicializado = true;
            ActualizarBotonComenzar(
                NetworkManager.Singleton != null
                    ? NetworkManager.Singleton.ConnectedClientsList.Count
                    : 0
            );
        }

        private void OnEnable()
        {
            _navegandoAPersonaje = false;
            SceneManager.sceneLoaded += AlCargarEscena;

            if (joinTest != null)
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

            HabilitarAccionCancelar();

            if (ConnectionManager != null)
            {
                ConnectionManager.OnEstadoCambiado += ActualizarTextoEstado;
                ConnectionManager.OnJugadoresCambiado += ActualizarBotonComenzar;
                ConnectionManager.OnConexionRechazada += MostrarMotivoRechazo;
                ConnectionManager.OnTiempoRestanteCambiado += ActualizarTiempo;
                ActualizarBotonComenzar(
                    NetworkManager.Singleton != null
                        ? NetworkManager.Singleton.ConnectedClientsList.Count
                        : 0
                );
            }

            ResolverCancelHostUI();

            if (ConnectionManager != null)
            {
                ActualizarTiempo(
                    ConnectionManager.TemporizadorActivo
                        ? ConnectionManager.SegundosRestantesConexion
                        : -1
                );
                ActualizarTextoEstado(ConnectionManager.EstadoActual);
            }

            GameFlowNetwork.Spawned -= SuscribirseAGameFlow;
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

            DeshabilitarAccionCancelar();
            _enfocandoCancelar = null;

            if (_cancelHostUI != null)
            {
                _cancelHostUI.OnCancelarSolicitado -= SolicitarCancelar;
                if (_cancelHostUI.IsOpen)
                    _cancelHostUI.Hide();
            }

            if (joinTest != null)
                joinTest.onClick.RemoveListener(JoinTestUi);
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
                ConnectionManager.OnTiempoRestanteCambiado -= ActualizarTiempo;
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

        private void HabilitarAccionCancelar()
        {
            if (_cancelAction == null)
            {
                _cancelAction = new InputAction("CancelarConexion", InputActionType.Button);
                _cancelAction.AddBinding("<Gamepad>/buttonEast");
                _cancelAction.AddBinding("<Keyboard>/escape");
                _cancelAction.performed += AlPresionarCancelar;
            }
            _cancelAction.Enable();
        }

        private void DeshabilitarAccionCancelar()
        {
            if (_cancelAction == null)
                return;

            _cancelAction.performed -= AlPresionarCancelar;
            _cancelAction.Disable();
            _cancelAction.Dispose();
            _cancelAction = null;
        }

        private void AlPresionarCancelar(InputAction.CallbackContext _)
        {
            if (IsOpen && DebeMostrarCancelar())
                SolicitarCancelar();
        }

        private void SolicitarCancelar()
        {
            if (ConnectionManager != null && ConnectionManager.PuedeCancelar)
                ConnectionManager.CancelarConexion();
        }

        private void ActualizarTiempo(int segundos)
        {
            if (_cancelHostUI != null)
                _cancelHostUI.ActualizarTiempo(segundos);
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
            _navegandoAPersonaje = true;
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
            if (GameFlowNetwork.Instance == null)
                return;

            GameFlowNetwork.Instance.OnCharacterSelectionStarted -= IrASeleccionDePersonaje;
            GameFlowNetwork.Instance.OnCharacterSelectionStarted += IrASeleccionDePersonaje;
        }

        private bool DebeMostrarCancelar(int jugadores = -1)
        {
            var manager = ConnectionManager;
            if (manager == null || manager.CerrandoRed || _navegandoAPersonaje)
                return false;

            var networkManager = NetworkManager.Singleton;
            switch (manager.EstadoActual)
            {
                case EstadoSesion.Conectando:
                case EstadoSesion.EsperandoJugador:
                    return true;
                case EstadoSesion.Listo:
                    if (networkManager == null)
                        return false;
                    if (networkManager.IsServer)
                    {
                        int cantidad =
                            jugadores >= 0 ? jugadores : networkManager.ConnectedClientsList.Count;
                        return cantidad < 2;
                    }
                    return networkManager.IsClient;
                default:
                    return false;
            }
        }

        private CancelHostUI ResolverCancelHostUI()
        {
            if (_cancelHostUI == null && UiManager.Instance != null)
            {
                _cancelHostUI =
                    UiManager.Instance.GetWindow(WindowsIds.CancelHostUI) as CancelHostUI;

                var cancelCanvas =
                    _cancelHostUI != null
                        ? _cancelHostUI.GetComponentInChildren<Canvas>(true)
                        : null;
                if (cancelCanvas != null)
                {
                    cancelCanvas.overrideSorting = true;
                    cancelCanvas.sortingOrder = 100;
                    if (cancelCanvas.GetComponentInParent<GraphicRaycaster>(true) == null)
                        cancelCanvas.gameObject.AddComponent<GraphicRaycaster>();
                }
            }

            if (_cancelHostUI != null)
            {
                _cancelHostUI.OnCancelarSolicitado -= SolicitarCancelar;
                _cancelHostUI.OnCancelarSolicitado += SolicitarCancelar;
            }
            return _cancelHostUI;
        }

        private void SincronizarVentanaCancelar(int jugadores = -1)
        {
            bool mostrar = DebeMostrarCancelar(jugadores);
            _connectionAttemptStarted = mostrar;

            if (_cancelHostUI == null && ResolverCancelHostUI() == null)
                return;

            if (mostrar && !_cancelHostUI.IsOpen)
            {
                _cancelHostUI.Show();
                EnfocarCancelar();
            }
            else if (!mostrar && _cancelHostUI.IsOpen)
                _cancelHostUI.Hide();
        }

        private void EnfocarCancelar()
        {
            if (!isActiveAndEnabled || _cancelHostUI == null || _cancelHostUI.cancelButton == null)
                return;

            if (_enfocandoCancelar != null)
                StopCoroutine(_enfocandoCancelar);
            _enfocandoCancelar = StartCoroutine(EnfocarCancelarCoroutine());
        }

        private IEnumerator EnfocarCancelarCoroutine()
        {
            for (int i = 0; i < 60 && _cancelHostUI != null && !_cancelHostUI.IsOpen; i++)
                yield return null;
            AplicarFocoCancelar();
            yield return null;
            AplicarFocoCancelar();
            _enfocandoCancelar = null;
        }

        private void AplicarFocoCancelar()
        {
            var boton = _cancelHostUI != null ? _cancelHostUI.cancelButton : null;
            if (boton == null || EventSystem.current == null || !_cancelHostUI.IsOpen)
                return;

            foreach (var grupo in boton.GetComponentsInParent<CanvasGroup>(true))
            {
                grupo.interactable = true;
                grupo.blocksRaycasts = true;
            }
            boton.interactable = true;

            if (EventSystem.current.currentSelectedGameObject != boton.gameObject)
                EventSystem.current.SetSelectedGameObject(boton.gameObject);
        }

        private void ActualizarBotonComenzar(int cantPlayers)
        {
            bool esHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;
            bool habilitado = esHost && cantPlayers >= 2;
            startMatchButton.interactable = habilitado;
            startMatchButton.gameObject.SetActive(habilitado);
            SincronizarVentanaCancelar(cantPlayers);

            if (habilitado)
            {
                SetInteractable(true);
                RestaurarFocoControl();
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
            if (ConnectionManager == null)
                return;

            ConnectionManager.OnCrearPartida();
            if (
                hostIpDisplayText != null
                && ConnectionManager.EstadoActual != EstadoSesion.Error
                && ConnectionManager.EstadoActual != EstadoSesion.Cancelada
            )
                hostIpDisplayText.text = $"IP: {ConnectionManager.GetLocalIPAddress()}";
        }

        private void StartClientButton_OnClick()
        {
            if (ConnectionManager == null)
                return;

            ConnectionManager.OnUnirse(ipInputField.text.Trim());
        }

        private void ActualizarTextoEstado(EstadoSesion estado)
        {
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

            bool activo = ConnectionManager.EstadoEsActivo(estado);
            if (startHostButton != null)
                startHostButton.interactable = !activo;
            if (startClientButton != null)
                startClientButton.interactable = !activo;
            if (joinTest != null)
                joinTest.interactable = !activo;

            SincronizarVentanaCancelar();

            if (!activo)
            {
                _navegandoAPersonaje = false;

                if (_inicializado)
                    SetInteractable(true);

                bool eraHost =
                    ConnectionManager != null && !ConnectionManager.UltimoIntentoFueCliente;
                if (eraHost && hostIpDisplayText != null)
                    hostIpDisplayText.text = string.Empty;
                if (eraHost && estado == EstadoSesion.Cancelada && ipInputField != null)
                    ipInputField.text = string.Empty;

                if (EventSystem.current != null)
                    RestaurarFocoControl();
            }
        }
    }
}
