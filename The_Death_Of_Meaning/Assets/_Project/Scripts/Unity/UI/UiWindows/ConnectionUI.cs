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
        private enum ConnectionMode
        {
            None,
            Host,
            Client,
        }

        public ConnectionManager ConnectionManager => ConnectionManager.Instance;
        public TMP_InputField ipInputField;
        public TextMeshProUGUI statusText;
        public Button startHostButton;
        public Button startClientButton;
        public Button startMatchButton;
        public Button joinTest;

        [Header("Selección visual (opcional; se busca por nombre si queda vacío)")]
        public RectTransform createHolder;
        public RectTransform joinHolder;
        public Image disconnectedIndicator;
        public Image connectedIndicator;
        public Color selectedTint = Color.white;
        public Color dimmedTint = new Color(0.42f, 0.42f, 0.42f, 1f);
        public float backgroundScale = 0.82f;
        public float focusDuration = 0.35f;

        private float _lastKeyboardCloseTime = -1f;
        private int _segundosRestantesConexion = -1;
        private bool _connectionAttemptStarted;
        private bool _inicializado;
        private bool _navegandoAPersonaje;
        private CancelHostUI _cancelHostUI;
        private InputAction _cancelAction;
        private Coroutine _enfocandoCancelar;
        private ConnectionMode _selectedMode;
        private Vector2 _createStartPosition;
        private Vector2 _joinStartPosition;
        private Vector3 _createStartScale;
        private Vector3 _joinStartScale;
        private int _createStartSiblingIndex;
        private int _joinStartSiblingIndex;
        private CanvasGroup _createGroup;
        private CanvasGroup _joinGroup;
        private Image _createImage;
        private Image _joinImage;
        private Coroutine _animacionSeleccion;
        private event Action<string> IpHostGenerada;

        private void Awake()
        {
            if (createHolder == null)
                createHolder = BuscarRectTransform("CrearPartidaUI");
            if (joinHolder == null)
                joinHolder = BuscarRectTransform("UnirseAPartidaUI");
            if (disconnectedIndicator == null)
                disconnectedIndicator = BuscarImagen("NotConnectedImage");
            if (connectedIndicator == null)
                connectedIndicator = BuscarImagen("SuccessConnectionImage");
            if (createHolder != null)
            {
                _createStartPosition = createHolder.anchoredPosition;
                _createStartScale = createHolder.localScale;
                _createStartSiblingIndex = createHolder.GetSiblingIndex();
                _createGroup =
                    createHolder.GetComponent<CanvasGroup>()
                    ?? createHolder.gameObject.AddComponent<CanvasGroup>();
                _createImage = createHolder.GetComponent<Image>();
            }
            if (joinHolder != null)
            {
                _joinStartPosition = joinHolder.anchoredPosition;
                _joinStartScale = joinHolder.localScale;
                _joinStartSiblingIndex = joinHolder.GetSiblingIndex();
                _joinGroup =
                    joinHolder.GetComponent<CanvasGroup>()
                    ?? joinHolder.gameObject.AddComponent<CanvasGroup>();
                _joinImage = joinHolder.GetComponent<Image>();
            }
            if (firstSelectedObject == null && startHostButton != null)
                firstSelectedObject = startHostButton.gameObject;
        }

        private RectTransform BuscarRectTransform(string nombre)
        {
            foreach (var elemento in GetComponentsInChildren<RectTransform>(true))
                if (elemento.name == nombre)
                    return elemento;
            return null;
        }

        private Image BuscarImagen(string nombre)
        {
            foreach (var elemento in GetComponentsInChildren<Transform>(true))
                if (elemento.name == nombre)
                    return elemento.GetComponent<Image>();
            return null;
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
            if (_animacionSeleccion != null)
            {
                StopCoroutine(_animacionSeleccion);
                _animacionSeleccion = null;
            }
            SceneManager.sceneLoaded -= AlCargarEscena;

            DeshabilitarAccionCancelar();
            _enfocandoCancelar = null;

            if (_cancelHostUI != null)
            {
                _cancelHostUI.OnCancelarSolicitado -= SolicitarCancelar;
                IpHostGenerada -= _cancelHostUI.ActualizarIpHost;
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
            if (!IsOpen)
                return;
            if (_selectedMode != ConnectionMode.None)
                PedirCancelarPorEvento();
            else if (DebeMostrarCancelar())
                PedirCancelarPorEvento();
        }

        private void SolicitarCancelar()
        {
            if (ConnectionManager != null && ConnectionManager.PuedeCancelar)
                ConnectionManager.CancelarConexion();
            RestaurarSeleccionVisual();
        }

        private void PedirCancelarPorEvento()
        {
            var cancelUI = ResolverCancelHostUI();
            if (cancelUI != null)
                cancelUI.CancelarConexion();
            else
                SolicitarCancelar();
        }

        private void ActualizarTiempo(int segundos)
        {
            _segundosRestantesConexion = segundos;
            if (_cancelHostUI != null)
                _cancelHostUI.ActualizarTiempo(segundos);

            ActualizarEstadoConTiempoCliente();
        }

        private void ActualizarEstadoConTiempoCliente()
        {
            if (
                statusText != null
                && _segundosRestantesConexion >= 0
                && ConnectionManager != null
                && ConnectionManager.EstadoActual == EstadoSesion.Conectando
                && ConnectionManager.UltimoIntentoFueCliente
            )
                statusText.text = $"Conectando... {_segundosRestantesConexion}s";
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
                IpHostGenerada -= _cancelHostUI.ActualizarIpHost;
                IpHostGenerada += _cancelHostUI.ActualizarIpHost;
            }
            return _cancelHostUI;
        }

        private void SincronizarVentanaCancelar(int jugadores = -1)
        {
            bool mostrar = DebeMostrarCancelar(jugadores);
            _connectionAttemptStarted = mostrar;

            if (_cancelHostUI == null && ResolverCancelHostUI() == null)
                return;

            bool esAnfitrion = NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;
            _cancelHostUI.ConfigurarModoHost(esAnfitrion);

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
            ActualizarIndicadorEstado(ConexionListaParaPartida(cantPlayers));
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

            SeleccionarModo(ConnectionMode.Host);

            ConnectionManager.OnCrearPartida();
            if (
                NetworkManager.Singleton != null
                && NetworkManager.Singleton.IsHost
                && ConnectionManager.EstadoActual != EstadoSesion.Error
                && ConnectionManager.EstadoActual != EstadoSesion.Cancelada
            )
                IpHostGenerada?.Invoke(ConnectionManager.GetLocalIPAddress());
        }

        private void StartClientButton_OnClick()
        {
            if (ConnectionManager == null)
                return;

            SeleccionarModo(ConnectionMode.Client);
            _cancelHostUI = ResolverCancelHostUI();
            _cancelHostUI?.ConfigurarModoHost(false);

            ConnectionManager.OnUnirse(ipInputField.text.Trim());
        }

        private void SeleccionarModo(ConnectionMode mode)
        {
            _selectedMode = mode;
            (mode == ConnectionMode.Host ? createHolder : joinHolder)?.SetAsLastSibling();
            AnimarSeleccion(mode);
            var boton = mode == ConnectionMode.Host ? startHostButton : startClientButton;
            if (EventSystem.current != null && boton != null)
                EventSystem.current.SetSelectedGameObject(boton.gameObject);
        }

        private void RestaurarSeleccionVisual()
        {
            _selectedMode = ConnectionMode.None;
            if (createHolder != null && joinHolder != null)
            {
                if (_createStartSiblingIndex < _joinStartSiblingIndex)
                {
                    createHolder.SetSiblingIndex(_createStartSiblingIndex);
                    joinHolder.SetSiblingIndex(_joinStartSiblingIndex);
                }
                else
                {
                    joinHolder.SetSiblingIndex(_joinStartSiblingIndex);
                    createHolder.SetSiblingIndex(_createStartSiblingIndex);
                }
            }
            AnimarSeleccion(ConnectionMode.None);
            RestaurarFocoControl();
        }

        private void AnimarSeleccion(ConnectionMode mode)
        {
            if (_animacionSeleccion != null)
                StopCoroutine(_animacionSeleccion);
            _animacionSeleccion = StartCoroutine(AnimarSeleccionCoroutine(mode));
        }

        private IEnumerator AnimarSeleccionCoroutine(ConnectionMode mode)
        {
            Vector2 createTarget = _createStartPosition;
            Vector2 joinTarget = _joinStartPosition;
            Vector3 createScale = _createStartScale;
            Vector3 joinScale = _joinStartScale;
            float createAlpha = 1f;
            float joinAlpha = 1f;
            Color createColor = selectedTint;
            Color joinColor = selectedTint;

            if (mode != ConnectionMode.None)
            {
                if (mode == ConnectionMode.Host)
                {
                    createTarget = PosicionCentrada(createHolder);
                    joinScale = _joinStartScale * backgroundScale;
                }
                else
                {
                    joinTarget = PosicionCentrada(joinHolder);
                    createScale = _createStartScale * backgroundScale;
                }
                createAlpha = mode == ConnectionMode.Host ? 1f : 0.48f;
                joinAlpha = mode == ConnectionMode.Client ? 1f : 0.48f;
                createColor = mode == ConnectionMode.Host ? selectedTint : dimmedTint;
                joinColor = mode == ConnectionMode.Client ? selectedTint : dimmedTint;
            }

            Vector2 createFrom =
                createHolder != null ? createHolder.anchoredPosition : createTarget;
            Vector2 joinFrom = joinHolder != null ? joinHolder.anchoredPosition : joinTarget;
            Vector3 createScaleFrom = createHolder != null ? createHolder.localScale : createScale;
            Vector3 joinScaleFrom = joinHolder != null ? joinHolder.localScale : joinScale;
            float createAlphaFrom = _createGroup != null ? _createGroup.alpha : createAlpha;
            float joinAlphaFrom = _joinGroup != null ? _joinGroup.alpha : joinAlpha;
            Color createColorFrom = _createImage != null ? _createImage.color : createColor;
            Color joinColorFrom = _joinImage != null ? _joinImage.color : joinColor;
            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, focusDuration);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = t * t * (3f - 2f * t);
                if (createHolder != null)
                    createHolder.anchoredPosition = Vector2.LerpUnclamped(
                        createFrom,
                        createTarget,
                        t
                    );
                if (joinHolder != null)
                    joinHolder.anchoredPosition = Vector2.LerpUnclamped(joinFrom, joinTarget, t);
                if (createHolder != null)
                    createHolder.localScale = Vector3.LerpUnclamped(
                        createScaleFrom,
                        createScale,
                        t
                    );
                if (joinHolder != null)
                    joinHolder.localScale = Vector3.LerpUnclamped(joinScaleFrom, joinScale, t);
                if (_createGroup != null)
                    _createGroup.alpha = Mathf.Lerp(createAlphaFrom, createAlpha, t);
                if (_joinGroup != null)
                    _joinGroup.alpha = Mathf.Lerp(joinAlphaFrom, joinAlpha, t);
                if (_createImage != null)
                    _createImage.color = Color.Lerp(createColorFrom, createColor, t);
                if (_joinImage != null)
                    _joinImage.color = Color.Lerp(joinColorFrom, joinColor, t);
                yield return null;
            }

            if (createHolder != null)
                createHolder.anchoredPosition = createTarget;
            if (joinHolder != null)
                joinHolder.anchoredPosition = joinTarget;
            if (createHolder != null)
                createHolder.localScale = createScale;
            if (joinHolder != null)
                joinHolder.localScale = joinScale;
            if (_createGroup != null)
                _createGroup.alpha = createAlpha;
            if (_joinGroup != null)
                _joinGroup.alpha = joinAlpha;
            if (_createImage != null)
                _createImage.color = createColor;
            if (_joinImage != null)
                _joinImage.color = joinColor;
            _animacionSeleccion = null;
        }

        private static Vector2 PosicionCentrada(RectTransform elemento)
        {
            if (elemento == null || elemento.parent is not RectTransform padre)
                return Vector2.zero;

            Rect rect = padre.rect;
            Vector2 anchorRelativo =
                elemento.anchorMin
                + Vector2.Scale(elemento.anchorMax - elemento.anchorMin, elemento.pivot);
            Vector2 puntoAncla = rect.min + Vector2.Scale(rect.size, anchorRelativo);
            Vector2 posicionPivoteObjetivo = rect.center - elemento.rect.center;
            return posicionPivoteObjetivo - puntoAncla;
        }

        private void ActualizarTextoEstado(EstadoSesion estado)
        {
            CerrarTecladoVirtual();
            ActualizarIndicadorEstado(ConexionListaParaPartida());

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
            ActualizarEstadoConTiempoCliente();

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
                if (eraHost)
                    ResolverCancelHostUI()?.ActualizarIpHost(string.Empty);
                if (eraHost && estado == EstadoSesion.Cancelada && ipInputField != null)
                    ipInputField.text = string.Empty;

                if (EventSystem.current != null)
                    RestaurarFocoControl();
            }
        }

        private bool ConexionListaParaPartida(int jugadoresHost = -1)
        {
            var networkManager = NetworkManager.Singleton;
            if (networkManager == null || ConnectionManager == null)
                return false;

            if (networkManager.IsHost)
            {
                int cantidad =
                    jugadoresHost >= 0 ? jugadoresHost : networkManager.ConnectedClientsList.Count;
                return cantidad >= 2;
            }

            return networkManager.IsClient && ConnectionManager.EstadoActual == EstadoSesion.Listo;
        }

        private void ActualizarIndicadorEstado(bool conectado)
        {
            if (connectedIndicator != null)
                connectedIndicator.gameObject.SetActive(conectado);
            if (disconnectedIndicator != null)
                disconnectedIndicator.gameObject.SetActive(!conectado);
        }
    }
}
