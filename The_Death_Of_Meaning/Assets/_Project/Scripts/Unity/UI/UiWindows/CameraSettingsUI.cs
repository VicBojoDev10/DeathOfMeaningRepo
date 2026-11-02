using TDOM.Data;
using TDOM.Unity.Settings;
using TDOM.Unity.UI;
using UnityEngine;
using UnityEngine.UI;

namespace TDOM.Unity
{
    public class CameraSettingsUI : UIWindow
    {
        [SerializeField]
        private ThirdPersonCameraConfig cameraConfig;

        [SerializeField]
        private Slider horizontalSensitivitySlider;

        [SerializeField]
        private Slider verticalSensitivitySlider;

        [SerializeField]
        private Button backButton;

        private CameraSettingsService _settingsService;

        private void Awake()
        {
            if (firstSelectedObject == null)
            {
                if (horizontalSensitivitySlider != null)
                    firstSelectedObject = horizontalSensitivitySlider.gameObject;
                else if (backButton != null)
                    firstSelectedObject = backButton.gameObject;
            }
        }

        private void OnEnable()
        {
            if (backButton != null)
                backButton.onClick.AddListener(OnBackButtonClicked);

            if (cameraConfig == null)
                return;

            _settingsService = new CameraSettingsService(cameraConfig);
            SincronizarSliders();
            RegistrarListenersSliders();
        }

        private void OnDisable()
        {
            if (backButton != null)
                backButton.onClick.RemoveListener(OnBackButtonClicked);

            if (horizontalSensitivitySlider != null)
                horizontalSensitivitySlider.onValueChanged.RemoveListener(
                    OnHorizontalSensitivityChanged
                );

            if (verticalSensitivitySlider != null)
                verticalSensitivitySlider.onValueChanged.RemoveListener(
                    OnVerticalSensitivityChanged
                );
        }

        private void SincronizarSliders()
        {
            Vector2 sensibilidad = _settingsService.Cargar();

            if (horizontalSensitivitySlider != null)
            {
                horizontalSensitivitySlider.minValue = cameraConfig.SensibilidadMinima;
                horizontalSensitivitySlider.maxValue = cameraConfig.SensibilidadMaxima;
                horizontalSensitivitySlider.SetValueWithoutNotify(sensibilidad.x);
            }

            if (verticalSensitivitySlider != null)
            {
                verticalSensitivitySlider.minValue = cameraConfig.SensibilidadMinima;
                verticalSensitivitySlider.maxValue = cameraConfig.SensibilidadMaxima;
                verticalSensitivitySlider.SetValueWithoutNotify(sensibilidad.y);
            }
        }

        private void RegistrarListenersSliders()
        {
            if (horizontalSensitivitySlider != null)
                horizontalSensitivitySlider.onValueChanged.AddListener(
                    OnHorizontalSensitivityChanged
                );

            if (verticalSensitivitySlider != null)
                verticalSensitivitySlider.onValueChanged.AddListener(OnVerticalSensitivityChanged);
        }

        private void OnHorizontalSensitivityChanged(float value)
        {
            _settingsService?.GuardarSensibilidadX(value);
        }

        private void OnVerticalSensitivityChanged(float value)
        {
            _settingsService?.GuardarSensibilidadY(value);
        }

        private void OnBackButtonClicked()
        {
            if (UiManager.Instance != null)
                UiManager.Instance.SwitchWindow(WindowId, WindowsIds.OptionsUI);
        }
    }
}
