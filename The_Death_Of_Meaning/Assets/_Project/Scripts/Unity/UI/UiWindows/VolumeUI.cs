using TDOM.Unity.UI;
using UnityEngine;
using UnityEngine.UI;

namespace TDOM.Unity
{
    public class VolumeUI : UIWindow
    {
        [SerializeField] private Slider generalVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Button backButton;

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
        }

        private void OnDisable()
        {
            if (backButton != null)
                backButton.onClick.RemoveListener(OnBackButtonClicked);
        }

        private void OnBackButtonClicked()
        {
            if (UiManager.Instance != null)
            {
                UiManager.Instance.CloseWindow(WindowsIds.VolumeUI);
                UiManager.Instance.ShowWindow(WindowsIds.OptionsUI);
            }
        }
    }
}
