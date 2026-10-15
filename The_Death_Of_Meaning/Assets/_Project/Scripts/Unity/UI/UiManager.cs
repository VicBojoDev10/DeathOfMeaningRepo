using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TDOM.Unity.UI
{
    public class UiManager : Singleton<UiManager>
    {
        [SerializeField]
        private List<UIWindow> windows = new List<UIWindow>();

        [SerializeField]
        private TransitionManager _transitionManager;

        public TransitionManager TransitionManager => _transitionManager;

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
            if (windows == null || windows.Count == 0)
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
            if (windows == null || windows.Count == 0)
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
            if (windows == null || windows.Count == 0)
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
        public const string GamePlayUI = "gameplayui";
        public const string CreditsUI = "creditsui";
        public const string OptionsUI = "optionsui";
        public const string VolumeUI = "volumeui";
        public const string ControlEsquemaUI = "controlesquemaui";
        public const string SplashUI = "splashui";
        public const string PausaUI = "pausaui";
        public const string RetryPopUpUI = "retrypopupui";
        public const string ExitPopUpUI = "exitpopupui";
    }
}
