using System.Collections;
using TDOM.Unity.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TDOM.Unity
{
    public class MainMenuUI : UIWindow
    {
        [Header("Main Menu Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button optionsButton;
        [SerializeField] private Button exitButton;

        [Header("Scene Settings")]
        [SerializeField] private string networkSetupSceneName = "NetworkSetup";

        private void Awake()
        {
            FindButtonsIfMissing();

            if (firstSelectedObject == null || firstSelectedObject.GetComponent<Selectable>() == null)
            {
                if (playButton != null)
                {
                    firstSelectedObject = playButton.gameObject;
                }
            }
        }

        private void Start()
        {
            StartCoroutine(StartupRoutine());
        }

        private IEnumerator StartupRoutine()
        {
            yield return null;

            // En la escena MainMenu nos aseguramos de que ConnectionUI esté cerrado
            if (UiManager.Instance != null)
            {
                UiManager.Instance.CloseWindow(WindowsIds.ConnectionMenuUI);
            }

            if (!HideOnStart)
            {
                FocusFirstElement();
            }
        }

        private void OnEnable()
        {
            if (playButton != null)
                playButton.onClick.AddListener(OnPlayButtonClicked);

            if (optionsButton != null)
                optionsButton.onClick.AddListener(OnOptionsButtonClicked);

            if (exitButton != null)
                exitButton.onClick.AddListener(OnExitButtonClicked);
        }

        private void OnDisable()
        {
            if (playButton != null)
                playButton.onClick.RemoveListener(OnPlayButtonClicked);

            if (optionsButton != null)
                optionsButton.onClick.RemoveListener(OnOptionsButtonClicked);

            if (exitButton != null)
                exitButton.onClick.RemoveListener(OnExitButtonClicked);

            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnPlayButtonClicked()
        {
            if (UiManager.Instance != null)
            {
                UiManager.Instance.CloseWindow(WindowsIds.MainMenuUI);
            }

            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.LoadScene(networkSetupSceneName);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == networkSetupSceneName)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                if (UiManager.Instance != null)
                {
                    UiManager.Instance.ShowWindow(WindowsIds.ConnectionMenuUI);
                }
            }
        }

        private void OnOptionsButtonClicked()
        {
            if (UiManager.Instance != null)
            {
                UiManager.Instance.CloseWindow(WindowsIds.MainMenuUI);
                UiManager.Instance.ShowWindow(WindowsIds.OptionsUI);
            }
        }

        private void OnExitButtonClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void FindButtonsIfMissing()
        {
            if (playButton == null)
            {
                Transform t = transform.Find("MainMenuCanvas/ConnectionMenuCanvasGroup/MainMenuContainer/PlayButton")
                           ?? transform.Find("MainMenuCanvas/MainMenuContainer/PlayButton");
                if (t != null) playButton = t.GetComponent<Button>();
            }

            if (optionsButton == null)
            {
                Transform t = transform.Find("MainMenuCanvas/ConnectionMenuCanvasGroup/MainMenuContainer/OptionsButton")
                           ?? transform.Find("MainMenuCanvas/MainMenuContainer/OptionsButton");
                if (t != null) optionsButton = t.GetComponent<Button>();
            }

            if (exitButton == null)
            {
                Transform t = transform.Find("MainMenuCanvas/ConnectionMenuCanvasGroup/MainMenuContainer/ExitGame")
                           ?? transform.Find("MainMenuCanvas/MainMenuContainer/ExitGame");
                if (t != null) exitButton = t.GetComponent<Button>();
            }

            if (playButton == null || optionsButton == null || exitButton == null)
            {
                Button[] allButtons = GetComponentsInChildren<Button>(true);
                foreach (var btn in allButtons)
                {
                    if (playButton == null && btn.name.IndexOf("Play", System.StringComparison.OrdinalIgnoreCase) >= 0)
                        playButton = btn;
                    else if (optionsButton == null && btn.name.IndexOf("Option", System.StringComparison.OrdinalIgnoreCase) >= 0)
                        optionsButton = btn;
                    else if (exitButton == null && btn.name.IndexOf("Exit", System.StringComparison.OrdinalIgnoreCase) >= 0)
                        exitButton = btn;
                }
            }
        }
    }
}
