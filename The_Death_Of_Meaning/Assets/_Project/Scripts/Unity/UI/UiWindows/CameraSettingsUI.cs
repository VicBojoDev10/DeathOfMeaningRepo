using System.Globalization;
using TDOM.Data;
using TDOM.Gameplay.Camera;
using TDOM.Unity.Settings;
using TDOM.Unity.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TDOM.Unity
{
    public class CameraSettingsUI : UIWindow
    {
        private const string FormatoNivel = "0.0";

        [SerializeField]
        private ThirdPersonCameraConfig cameraConfig;

        [SerializeField]
        private Slider horizontalSensitivitySlider;

        [SerializeField]
        private Slider verticalSensitivitySlider;

        [SerializeField]
        private TMP_Text horizontalSensitivityValueText;

        [SerializeField]
        private TMP_Text verticalSensitivityValueText;

        [SerializeField]
        private Button backButton;

        private CameraSettingsService _settingsService;
        private EscalaSensibilidad _escala;

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
            _escala = new EscalaSensibilidad(
                cameraConfig.NivelSensibilidadMinimo,
                cameraConfig.NivelSensibilidadMaximo,
                cameraConfig.PasoNivelSensibilidad,
                cameraConfig.GradosPorNivelSensibilidad
            );
            SincronizarSliders();
            RegistrarListenersSliders();
        }

        private void OnDisable()
        {
            _settingsService?.GuardarEnDisco();

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
            SincronizarSlider(
                horizontalSensitivitySlider,
                horizontalSensitivityValueText,
                sensibilidad.x
            );
            SincronizarSlider(
                verticalSensitivitySlider,
                verticalSensitivityValueText,
                sensibilidad.y
            );
        }

        private void SincronizarSlider(Slider slider, TMP_Text texto, float grados)
        {
            int paso = _escala.PasoDesdeGrados(grados);
            MostrarNivel(texto, paso);

            if (slider == null)
                return;

            slider.wholeNumbers = true;
            slider.minValue = _escala.PasoMinimo;
            slider.maxValue = _escala.PasoMaximo;
            slider.SetValueWithoutNotify(paso);
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
            int paso = Mathf.RoundToInt(value);
            MostrarNivel(horizontalSensitivityValueText, paso);
            _settingsService?.GuardarSensibilidadX(_escala.GradosDesdePaso(paso));
        }

        private void OnVerticalSensitivityChanged(float value)
        {
            int paso = Mathf.RoundToInt(value);
            MostrarNivel(verticalSensitivityValueText, paso);
            _settingsService?.GuardarSensibilidadY(_escala.GradosDesdePaso(paso));
        }

        private void MostrarNivel(TMP_Text texto, int paso)
        {
            if (texto != null)
                texto.text = _escala
                    .NivelDesdePaso(paso)
                    .ToString(FormatoNivel, CultureInfo.InvariantCulture);
        }

        private void OnBackButtonClicked()
        {
            if (UiManager.Instance != null)
                UiManager.Instance.SwitchWindow(WindowId, WindowsIds.OptionsUI);
        }
    }
}
