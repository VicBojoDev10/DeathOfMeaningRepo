using System;
using System.Collections;
using TDOM.Unity.Audio;
using TDOM.Unity.Input;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace TDOM.Unity
{
    public class MainMenuUI : UIWindow
    {
        [Header("Paneles")]
        [SerializeField] private GameObject _splashPanel;
        [SerializeField] private GameObject _mainPanel;
        [SerializeField] private GameObject _optionsPanel;
        [SerializeField] private GameObject _volumePanel;
        [SerializeField] private GameObject _controlsPanel;
        [SerializeField] private GameObject _creditsPanel;

        [Header("Splash")]
        [Tooltip("0 = solo avanza al presionar cualquier botón")]
        [SerializeField] private float _splashAutoAdvanceSeconds = 0f;

        [Header("Menú principal")]
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _optionsButton;
        [SerializeField] private Button _exitButton;

        [Header("Ajustes")]
        [FormerlySerializedAs("_continueButton")]
        [SerializeField] private Button _controlsButton;
        [SerializeField] private Button _volumeButton;
        [SerializeField] private Button _creditsButton;
        [SerializeField] private Button _optionsBackButton;

        [Header("Botones Regresar (-> Ajustes)")]
        [SerializeField] private Button _volumeBackButton;
        [SerializeField] private Button _controlsBackButton;
        [SerializeField] private Button _creditsBackButton;

        [Header("Volumen (el mixer vive en AudioManager)")]
        [FormerlySerializedAs("_volumeChannels")]
        [SerializeField] private VolumeSlider[] _volumeSliders;

        [Header("Escenas")]
        [SerializeField] private string _multiplayerSceneName = "Multiplayer";

        [Serializable]
        private class VolumeSlider
        {
            public AudioChannel channel;
            public Slider slider;
        }

        private GameObject[] _panels;
        private IDisposable _anyButtonSub;
        private PlayerInputActions _uiActions;

        public override void Initialize()
        {
            base.Initialize();

            _panels = new[]
            {
                _splashPanel, _mainPanel, _optionsPanel, _volumePanel, _controlsPanel, _creditsPanel,
            };

            Bind(_playButton, OnPlayClicked);
            Bind(_optionsButton, () => ShowPanel(_optionsPanel, _volumeButton));
            Bind(_exitButton, OnExitClicked);

            Bind(_volumeButton, () =>
            {
                RefreshVolumeSliders();
                ShowPanel(_volumePanel, FirstSlider());
            });
            Bind(_controlsButton, () => ShowPanel(_controlsPanel, _controlsBackButton));
            Bind(_creditsButton, () => ShowPanel(_creditsPanel, _creditsBackButton));
            Bind(_optionsBackButton, () => ShowPanel(_mainPanel, _optionsButton));

            Bind(_volumeBackButton, ReturnToOptions);
            Bind(_controlsBackButton, ReturnToOptions);
            Bind(_creditsBackButton, ReturnToOptions);

            BindVolume();
            SetupGamepadUI();

            ShowPanel(_splashPanel, (GameObject)null);
            WaitForSplashInput();

            if (_splashAutoAdvanceSeconds > 0f)
                StartCoroutine(SplashAutoAdvance());

            Show();
        }

        private void OnDisable()
        {
            _anyButtonSub?.Dispose();
            _anyButtonSub = null;

            if (_uiActions != null)
            {
                _uiActions.UI.Cancel.performed -= OnCancelPerformed;
                _uiActions.Dispose();
                _uiActions = null;
            }
        }

        private void SetupGamepadUI()
        {
            var eventSystem = EventSystem.current;
            var module = eventSystem != null ? eventSystem.GetComponent<InputSystemUIInputModule>() : null;
            if (module == null)
            {
                Debug.LogWarning(
                    "El EventSystem no tiene InputSystemUIInputModule: no se podrá navegar con gamepad."
                );
                return;
            }

            _uiActions = new PlayerInputActions();
            module.move = InputActionReference.Create(_uiActions.UI.Navigate);
            module.submit = InputActionReference.Create(_uiActions.UI.Submit);
            module.cancel = InputActionReference.Create(_uiActions.UI.Cancel);

            _uiActions.UI.Cancel.performed += OnCancelPerformed;
            _uiActions.UI.Enable();
        }

        private void OnCancelPerformed(InputAction.CallbackContext ctx) => GoBack();

        private void WaitForSplashInput()
        {
            _anyButtonSub?.Dispose();
            _anyButtonSub = InputSystem.onAnyButtonPress.CallOnce(_ => GoToMain());
        }
        private void ShowPanel(GameObject panel, Button focusButton)
        {
            ShowPanel(panel, focusButton != null ? focusButton.gameObject : null);
        }

        private void ShowPanel(GameObject panel, GameObject focusTarget)
        {
            foreach (var p in _panels)
            {
                if (p != null)
                    p.SetActive(p == panel);
            }

            if (focusTarget != null)
                FocusElement(focusTarget);
        }

        private void GoToMain()
        {
            if (IsActive(_splashPanel))
                ShowPanel(_mainPanel, _playButton);
        }

        private IEnumerator SplashAutoAdvance()
        {
            yield return new WaitForSecondsRealtime(_splashAutoAdvanceSeconds);
            GoToMain();
        }

        private void GoBack()
        {
            if (IsActive(_volumePanel) || IsActive(_controlsPanel) || IsActive(_creditsPanel))
                ReturnToOptions();
            else if (IsActive(_optionsPanel))
                ShowPanel(_mainPanel, _optionsButton);
        }

        private void ReturnToOptions()
        {
            Button focus = null;
            if (IsActive(_volumePanel))
                focus = _volumeButton;
            else if (IsActive(_controlsPanel))
                focus = _controlsButton;
            else if (IsActive(_creditsPanel))
                focus = _creditsButton;

            ShowPanel(_optionsPanel, focus);
        }

        private GameObject FirstSlider()
        {
            if (
                _volumeSliders != null
                && _volumeSliders.Length > 0
                && _volumeSliders[0] != null
                && _volumeSliders[0].slider != null
            )
                return _volumeSliders[0].slider.gameObject;

            return _volumeBackButton != null ? _volumeBackButton.gameObject : null;
        }

        private static bool IsActive(GameObject go) => go != null && go.activeSelf;
        private void OnPlayClicked()
        {
            if (!Application.CanStreamedLevelBeLoaded(_multiplayerSceneName))
            {
                Debug.LogError(
                    $"La escena '{_multiplayerSceneName}' no está en Build Settings o el nombre es incorrecto."
                );
                return;
            }

            SetInteractable(false);
            SceneManager.LoadSceneAsync(_multiplayerSceneName);
        }

        private void OnExitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        private void BindVolume()
        {
            if (_volumeSliders == null)
                return;

            if (AudioManager.Instance == null)
            {
                Debug.LogWarning("No hay AudioManager en la escena: los sliders de volumen no harán nada.");
                return;
            }

            foreach (var entry in _volumeSliders)
            {
                if (entry == null || entry.slider == null)
                    continue;

                AudioChannel channel = entry.channel;
                entry.slider.onValueChanged.AddListener(value => SetVolume(channel, value));
            }

            RefreshVolumeSliders();
        }

        private void RefreshVolumeSliders()
        {
            if (_volumeSliders == null || AudioManager.Instance == null)
                return;

            foreach (var entry in _volumeSliders)
            {
                if (entry != null && entry.slider != null)
                    entry.slider.SetValueWithoutNotify(AudioManager.Instance.GetVolume(entry.channel));
            }
        }

        private static void SetVolume(AudioChannel channel, float value)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.SetVolume(channel, value);
        }
        private static void Bind(Button button, UnityAction action)
        {
            if (button != null)
                button.onClick.AddListener(action);
        }
    }
}