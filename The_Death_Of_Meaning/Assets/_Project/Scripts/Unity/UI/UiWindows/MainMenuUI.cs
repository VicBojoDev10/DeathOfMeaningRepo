using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using UnityEngine.InputSystem;
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
        [Tooltip("0 = solo avanza con input")]
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
        [SerializeField] private Button _backFromOptionsButton;
        [SerializeField] private Button[] _backToOptionsButtons;

        [Header("Volumen")]
        [SerializeField] private AudioMixer _mixer;
        [SerializeField] private VolumeChannel[] _volumeChannels;

        [Header("Escenas")]
        [SerializeField] private string _multiplayerSceneName = "Multiplayer";

        [Serializable]
        private class VolumeChannel
        {
            public Slider slider;
            public string mixerParam;
        }

        private GameObject[] _panels;
        private IDisposable _anyButtonSub;

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

            Bind(_volumeButton, () => ShowPanel(_volumePanel, FirstSlider()));
            Bind(_controlsButton, () => ShowPanel(_controlsPanel, BackButtonOf(_controlsPanel)));
            Bind(_creditsButton, () => ShowPanel(_creditsPanel, BackButtonOf(_creditsPanel)));
            Bind(_backFromOptionsButton, () => ShowPanel(_mainPanel, _optionsButton));

            if (_backToOptionsButtons != null)
            {
                foreach (var back in _backToOptionsButtons)
                {
                    Bind(back, ReturnToOptions);
                }
            }

            BindVolume();

            ShowPanel(_splashPanel, (GameObject)null);
            WaitForSplashInput();
            if (_splashAutoAdvanceSeconds > 0f)
                StartCoroutine(SplashAutoAdvance());

            Show();
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
           private void WaitForSplashInput()
        {
            _anyButtonSub?.Dispose();
            _anyButtonSub = InputSystem.onAnyButtonPress.CallOnce(_ => GoToMain());
        }
          private void OnDisable()
        {
            _anyButtonSub?.Dispose();
            _anyButtonSub = null;
        }
        private void GoToMain()
        {
            if (_splashPanel != null && _splashPanel.activeSelf)
                ShowPanel(_mainPanel, _playButton != null ? _playButton.gameObject : null);
        }

        private IEnumerator SplashAutoAdvance()
        {
            yield return new WaitForSecondsRealtime(_splashAutoAdvanceSeconds);
            GoToMain();
        }

        private void ReturnToOptions()
        {
            GameObject focus = null;
            if (_volumePanel != null && _volumePanel.activeSelf && _volumeButton != null)
                focus = _volumeButton.gameObject;
            else if (_controlsPanel != null && _controlsPanel.activeSelf && _controlsButton != null)
                focus = _controlsButton.gameObject;
            else if (_creditsPanel != null && _creditsPanel.activeSelf && _creditsButton != null)
                focus = _creditsButton.gameObject;

            ShowPanel(_optionsPanel, focus);
        }

        private GameObject FirstSlider()
        {
            if (_volumeChannels != null && _volumeChannels.Length > 0 && _volumeChannels[0].slider != null)
                return _volumeChannels[0].slider.gameObject;
            return BackButtonOf(_volumePanel);
        }

        private GameObject BackButtonOf(GameObject panel)
        {
            if (panel == null || _backToOptionsButtons == null)
                return null;

            foreach (var back in _backToOptionsButtons)
            {
                if (back != null && back.transform.IsChildOf(panel.transform))
                    return back.gameObject;
            }
            return null;
        }
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
            if (_mixer == null || _volumeChannels == null)
                return;

            foreach (var channel in _volumeChannels)
            {
                if (channel == null || channel.slider == null || string.IsNullOrEmpty(channel.mixerParam))
                    continue;

                string param = channel.mixerParam;
                string key = "vol_" + param;

                float saved = PlayerPrefs.GetFloat(key, 1f);
                channel.slider.SetValueWithoutNotify(saved);
                ApplyVolume(param, saved);

                channel.slider.onValueChanged.AddListener(value =>
                {
                    ApplyVolume(param, value);
                    PlayerPrefs.SetFloat(key, value);
                });
            }
        }

        private void ApplyVolume(string param, float linear)
        {
            float db = Mathf.Log10(Mathf.Clamp(linear, 0.0001f, 1f)) * 20f;
            _mixer.SetFloat(param, db);
        }
        private static void Bind(Button button, UnityAction action)
        {
            if (button != null)
                button.onClick.AddListener(action);
        }
    }
}