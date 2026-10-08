using System.Collections.Generic;
using UnityEngine;

namespace TDOM.Unity.UI
{
    public class UiManager : Singleton<UiManager>
    {
        [SerializeField]
        private List<UIWindow> windows = new List<UIWindow>();

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
            windows.Clear();
            var uiWindows = gameObject.GetComponentsInChildren<UIWindow>(true);
            foreach (var uiWindow in uiWindows)
            {
                if (!windows.Contains(uiWindow))
                {
                    windows.Add(uiWindow);
                }
            }
        }

        public void ShowWindow(string windowId)
        {
            UIWindow windowToShow = null;
            foreach (UIWindow window in windows)
            {
                if (window.WindowId == windowId)
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
            UIWindow windowToClose = null;
            foreach (UIWindow window in windows)
            {
                if (window.WindowId == windowId)
                {
                    windowToClose = window;
                }
            }
            if (windowToClose != null)
            {
                windowToClose.Hide();
            }
            else
            {
                Debug.Log($"No se encontro la ventana con ID");
            }
        }

        public UIWindow GetWindow(string windowId)
        {
            foreach (UIWindow window in windows)
            {
                if (window.WindowId == windowId)
                {
                    return window;
                }
            }
            Debug.LogError($"No se encontro la ventana con ID");
            return null;
        }
    }

    public static class WindowsIds
    {
        public const string ConnectionMenuUI = "connectionui";
        public const string ChSelectionUI = "chselectionui";
        public const string KeyboardUI = "keyboardui";
        public const string MainMenuUI = "mainmenuui";
    }
}
