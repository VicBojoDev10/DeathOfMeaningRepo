using TDOM.Unity.UI;
using UnityEngine;
using UnityEngine.UI;

namespace TDOM.Unity
{
    public class OptionsUI : UIWindow
    {
        [SerializeField]
        private Button controllerEsquemButton;

        [SerializeField]
        private Button volumeCustomButton;

        [SerializeField]
        private Button creditsButton;

        [SerializeField]
        private Button backButton;

        private void Awake()
        {
            if (firstSelectedObject == null && controllerEsquemButton != null)
            {
                firstSelectedObject = controllerEsquemButton.gameObject;
            }
        }

        private void OnEnable()
        {
            if (controllerEsquemButton != null)
                controllerEsquemButton.onClick.AddListener(OnControllerEsquemaClicked);

            if (volumeCustomButton != null)
                volumeCustomButton.onClick.AddListener(OnVolumeCustomClicked);

            if (creditsButton != null)
                creditsButton.onClick.AddListener(OnCreditsClicked);

            if (backButton != null)
                backButton.onClick.AddListener(OnBackClicked);
        }

        private void OnDisable()
        {
            if (controllerEsquemButton != null)
                controllerEsquemButton.onClick.RemoveListener(OnControllerEsquemaClicked);

            if (volumeCustomButton != null)
                volumeCustomButton.onClick.RemoveListener(OnVolumeCustomClicked);

            if (creditsButton != null)
                creditsButton.onClick.RemoveListener(OnCreditsClicked);

            if (backButton != null)
                backButton.onClick.RemoveListener(OnBackClicked);
        }

        private void OnControllerEsquemaClicked()
        {
            if (UiManager.Instance != null)
            {
                UiManager.Instance.SwitchWindow(WindowsIds.OptionsUI, WindowsIds.ControlEsquemaUI);
            }
        }

        private void OnVolumeCustomClicked()
        {
            if (UiManager.Instance != null)
            {
                UiManager.Instance.SwitchWindow(WindowsIds.OptionsUI, WindowsIds.VolumeUI);
            }
        }

        private void OnCreditsClicked()
        {
            if (UiManager.Instance != null)
            {
                UiManager.Instance.SwitchWindow(WindowsIds.OptionsUI, WindowsIds.CreditsUI);
            }
        }

        private void OnBackClicked()
        {
            if (UiManager.Instance != null)
            {
                UiManager.Instance.SwitchWindow(WindowsIds.OptionsUI, WindowsIds.MainMenuUI);
            }
        }
    }
}
