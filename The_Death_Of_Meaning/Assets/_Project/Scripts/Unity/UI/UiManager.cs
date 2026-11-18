using System;
using System.Collections.Generic;
using TDOM.Unity.Player;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TDOM.Unity.UI
{
    public interface IGameplayHud
    {
        void SetHudVisible(bool visible, bool bossBarVisible = false);
    }

    public class UiManager : Singleton<UiManager>
    {
        [SerializeField]
        private List<UIWindow> windows = new List<UIWindow>();

        [SerializeField]
        private TransitionManager _transitionManager;

        private bool _mostrarHudGameplay;
        private string _ventanaGameplayAbierta;
        private string _hudNoEncontradoAdvertido;

        public TransitionManager TransitionManager => _transitionManager;

        public string GameplayWindowId
        {
            get
            {
                var localClient = NetworkManager.Singleton?.LocalClient;
                if (localClient != null)
                {
                    foreach (var ownedObject in localClient.OwnedObjects)
                    {
                        if (
                            ownedObject != null
                            && ownedObject.TryGetComponent(out PlayerRoot player)
                        )
                            return player.CharacterId == TDOM.Unity.CharacterIds.Zendre
                                ? WindowsIds.ZendreGameplayUI
                                : WindowsIds.AylaGameplayUI;
                    }
                }
                return WindowsIds.AylaGameplayUI;
            }
        }

        public UIWindow GetGameplayWindow() => GetWindow(GameplayWindowId);

        public void ShowGameplayWindow()
        {
            _mostrarHudGameplay = true;
            FoundUIScene();
            ActualizarVentanaGameplay();
        }

        public void CloseGameplayWindow()
        {
            _mostrarHudGameplay = false;
            _ventanaGameplayAbierta = null;
            CloseWindow(WindowsIds.AylaGameplayUI);
            CloseWindow(WindowsIds.ZendreGameplayUI);
        }

        private void Update()
        {
            if (_mostrarHudGameplay)
                ActualizarVentanaGameplay();
        }

        private void ActualizarVentanaGameplay()
        {
            FoundUIScene();
            var jugadorLocal = BuscarJugadorLocal();
            if (jugadorLocal == null || jugadorLocal.CharacterId == TDOM.Unity.CharacterIds.None)
                return;

            string idDeseado =
                jugadorLocal.CharacterId == TDOM.Unity.CharacterIds.Zendre
                    ? WindowsIds.ZendreGameplayUI
                    : WindowsIds.AylaGameplayUI;
            if (_ventanaGameplayAbierta != idDeseado)
            {
                string idAnterior = _ventanaGameplayAbierta;
                _ventanaGameplayAbierta = idDeseado;
                if (idAnterior != null && idAnterior != idDeseado)
                    CloseWindow(idAnterior);

                string idAlterno =
                    idDeseado == WindowsIds.ZendreGameplayUI
                        ? WindowsIds.AylaGameplayUI
                        : WindowsIds.ZendreGameplayUI;
                CloseWindow(idAlterno);
            }

            UIWindow ventana = BuscarVentanaRegistrada(idDeseado);
            if (ventana == null)
            {
                FoundUIScene();
                ventana = BuscarVentanaRegistrada(idDeseado);
            }
            if (ventana != null && !ventana.IsOpen)
                ventana.Show();
            else if (ventana == null && _hudNoEncontradoAdvertido != idDeseado)
            {
                Debug.LogError(
                    $"No se encontró la ventana de gameplay '{idDeseado}' en la escena."
                );
                _hudNoEncontradoAdvertido = idDeseado;
            }
        }

        private UIWindow BuscarVentanaRegistrada(string id)
        {
            foreach (UIWindow window in windows)
                if (window != null && window.WindowId == id)
                    return window;
            return null;
        }

        private static PlayerRoot BuscarJugadorLocal()
        {
            var localClient = NetworkManager.Singleton?.LocalClient;
            if (localClient == null)
                return null;

            foreach (var ownedObject in localClient.OwnedObjects)
            {
                if (ownedObject != null && ownedObject.TryGetComponent(out PlayerRoot player))
                    return player;
            }
            return null;
        }

        protected override void Awake()
        {
            base.Awake();
            FoundUIScene();
        }

        void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            FoundUIScene();
        }

        private void FoundUIScene()
        {
            var uiWindows = gameObject.GetComponentsInChildren<UIWindow>(true);
            foreach (var uiWindow in uiWindows)
            {
                if (uiWindow != null && !windows.Contains(uiWindow))
                {
                    windows.Add(uiWindow);
                }
            }
        }

        public void ShowWindow(string windowId)
        {
            FoundUIScene();

            UIWindow windowToShow = null;
            foreach (UIWindow window in windows)
            {
                if (window != null && window.WindowId == windowId)
                {
                    windowToShow = window;
                    break;
                }
            }
            if (windowToShow != null)
            {
                windowToShow.Show();
            }
            else
            {
                Debug.LogError($"No se encontro la ventana con ID {windowId}");
            }
        }

        public void CloseWindow(string windowId)
        {
            FoundUIScene();

            UIWindow windowToClose = null;
            foreach (UIWindow window in windows)
            {
                if (window != null && window.WindowId == windowId)
                {
                    windowToClose = window;
                    break;
                }
            }
            if (windowToClose != null)
            {
                windowToClose.Hide();
            }
            else
            {
                Debug.Log($"No se encontro la ventana con ID {windowId}");
            }
        }

        public void SwitchWindow(string fromWindowId, string toWindowId)
        {
            if (_transitionManager == null)
            {
                CloseWindow(fromWindowId);
                ShowWindow(toWindowId);
                return;
            }

            _transitionManager.CambiarVentana(GetWindow(fromWindowId), GetWindow(toWindowId));
        }

        public void ActivarInteraccionSolo(string windowId)
        {
            if (windows == null || windows.Count == 0)
                FoundUIScene();

            foreach (UIWindow window in windows)
                if (window != null)
                    window.SetInteractable(window.WindowId == windowId);
        }

        public void LoadScene(
            string sceneName,
            Action onScreenCovered = null,
            Action onSceneLoaded = null
        )
        {
            if (_transitionManager == null)
            {
                onScreenCovered?.Invoke();
                SceneManager.LoadScene(sceneName);
                onSceneLoaded?.Invoke();
                return;
            }

            _transitionManager.CargarEscena(sceneName, onScreenCovered, onSceneLoaded);
        }

        public UIWindow GetWindow(string windowId)
        {
            FoundUIScene();

            foreach (UIWindow window in windows)
            {
                if (window != null && window.WindowId == windowId)
                {
                    return window;
                }
            }
            Debug.LogError($"No se encontro la ventana con ID {windowId}");
            return null;
        }
    }

    public static class WindowsIds
    {
        public const string ConnectionMenuUI = "connectionui";
        public const string ChSelectionUI = "chselectionui";
        public const string KeyboardUI = "keyboardui";
        public const string MainMenuUI = "mainmenuui";
        public const string AylaGameplayUI = "aylagameplayui";
        public const string CreditsUI = "creditsui";
        public const string OptionsUI = "optionsui";
        public const string VolumeUI = "volumeui";
        public const string CameraSettingsUI = "camerasettingsui";
        public const string ControlEsquemaUI = "controlesquemaui";
        public const string SplashUI = "splashui";
        public const string PausaUI = "pausaui";
        public const string RetryPopUpUI = "retrypopupui";
        public const string ExitPopUpUI = "exitpopupui";
        public const string DerrotaUI = "derrotaui";
        public const string CancelHostUI = "cancelhostui";
        public const string ControllerReconnectUI = "controllerreconnectui";
        public const string ZendreGameplayUI = "zendregameplayui";
    }
}
