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
        private TransitionManager _transiciones;

        public TransitionManager Transiciones => _transiciones;

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

        public void CambiarVentana(string deWindowId, string aWindowId)
        {
            if (_transiciones == null)
            {
                CloseWindow(deWindowId);
                ShowWindow(aWindowId);
                return;
            }

            _transiciones.CambiarVentana(GetWindow(deWindowId), GetWindow(aWindowId));
        }

        public void CargarEscena(string nombreEscena, Action alCubrirPantalla = null)
        {
            if (_transiciones == null)
            {
                alCubrirPantalla?.Invoke();
                SceneManager.LoadScene(nombreEscena);
                return;
            }

            _transiciones.CargarEscena(nombreEscena, alCubrirPantalla);
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
    }
}
