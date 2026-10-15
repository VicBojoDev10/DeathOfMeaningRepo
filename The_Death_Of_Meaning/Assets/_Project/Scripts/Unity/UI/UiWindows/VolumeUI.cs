using TDOM.Unity.Audio;
using TDOM.Unity.UI;
using UnityEngine;
using UnityEngine.UI;

namespace TDOM.Unity
{
    public class VolumeUI : UIWindow
    {
        [SerializeField]
        private Slider generalVolumeSlider;

        [SerializeField]
        private Slider musicVolumeSlider;

        [SerializeField]
        private Slider sfxVolumeSlider;

        [SerializeField]
        private Button backButton;

        private void Awake()
        {
            if (firstSelectedObject == null)
            {
                if (generalVolumeSlider != null)
                    firstSelectedObject = generalVolumeSlider.gameObject;
                else if (backButton != null)
                    firstSelectedObject = backButton.gameObject;
            }
        }

        private void OnEnable()
        {
            if (backButton != null)
                backButton.onClick.AddListener(OnBackButtonClicked);

            SincronizarSliders();
            RegistrarListenersSliders();
        }

        private void OnDisable()
        {
            if (backButton != null)
                backButton.onClick.RemoveListener(OnBackButtonClicked);

            RemoverListenersSliders();
        }

        private void SincronizarSliders()
        {
            if (AudioManager.Instance == null)
                return;

            if (generalVolumeSlider != null)
                generalVolumeSlider.SetValueWithoutNotify(
                    AudioManager.Instance.GetVolume(AudioChannel.Master)
                );

            if (musicVolumeSlider != null)
                musicVolumeSlider.SetValueWithoutNotify(
                    AudioManager.Instance.GetVolume(AudioChannel.Music)
                );

            if (sfxVolumeSlider != null)
                sfxVolumeSlider.SetValueWithoutNotify(
                    AudioManager.Instance.GetVolume(AudioChannel.Sfx)
                );
        }

        private void RegistrarListenersSliders()
        {
            if (generalVolumeSlider != null)
                generalVolumeSlider.onValueChanged.AddListener(OnGeneralVolumeChanged);

            if (musicVolumeSlider != null)
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);

            if (sfxVolumeSlider != null)
                sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
        }

        private void RemoverListenersSliders()
        {
            if (generalVolumeSlider != null)
                generalVolumeSlider.onValueChanged.RemoveListener(OnGeneralVolumeChanged);

            if (musicVolumeSlider != null)
                musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);

            if (sfxVolumeSlider != null)
                sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
        }

        private void OnGeneralVolumeChanged(float value)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.SetVolume(AudioChannel.Master, value);
        }

        private void OnMusicVolumeChanged(float value)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.SetVolume(AudioChannel.Music, value);
        }

        private void OnSfxVolumeChanged(float value)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.SetVolume(AudioChannel.Sfx, value);
        }

        private void OnBackButtonClicked()
        {
            if (UiManager.Instance != null)
            {
                UiManager.Instance.CambiarVentana(WindowsIds.VolumeUI, WindowsIds.OptionsUI);
            }
        }
    }
}
