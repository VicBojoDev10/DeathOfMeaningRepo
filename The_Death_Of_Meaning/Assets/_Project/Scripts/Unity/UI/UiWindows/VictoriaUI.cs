using TDOM.Unity.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TDOM.Unity
{
    public class VictoriaUI : UIWindow
    {
        [SerializeField]
        private TMP_Text tiempoFinalText;

        [SerializeField]
        private Button jugarDenuevoButton;

        [SerializeField]
        private Button salirMenuButton;

        private void Awake()
        {
            if (firstSelectedObject == null && jugarDenuevoButton != null)
                firstSelectedObject = jugarDenuevoButton.gameObject;
        }

        private void OnEnable()
        {
            if (jugarDenuevoButton != null)
                jugarDenuevoButton.onClick.AddListener(OnJugarDeNuevoClicked);

            if (salirMenuButton != null)
                salirMenuButton.onClick.AddListener(OnSalirMenuClicked);
        }

        private void OnDisable()
        {
            if (jugarDenuevoButton != null)
                jugarDenuevoButton.onClick.RemoveListener(OnJugarDeNuevoClicked);

            if (salirMenuButton != null)
                salirMenuButton.onClick.RemoveListener(OnSalirMenuClicked);
        }

        public void MostrarTiempoFinal(float tiempoSegundos)
        {
            if (tiempoFinalText == null)
            {
                Debug.LogWarning("Asigna el texto del tiempo final en VictoriaUI.");
                return;
            }

            int segundosTotales = Mathf.Max(0, Mathf.FloorToInt(tiempoSegundos));
            int horas = segundosTotales / 3600;
            int minutos = (segundosTotales % 3600) / 60;
            int segundos = segundosTotales % 60;
            tiempoFinalText.text =
                horas > 0
                    ? $"Tiempo: {horas:00}:{minutos:00}:{segundos:00}"
                    : $"Tiempo: {minutos:00}:{segundos:00}";
        }

        private void OnJugarDeNuevoClicked()
        {
            var pauseManager = PauseManager.Instance;
            if (pauseManager == null || !pauseManager.IsSpawned)
            {
                Debug.LogError("No hay un PauseManager activo para solicitar la confirmación.");
                return;
            }

            pauseManager.PedirConfirmacionReinicioVictoria();
        }

        private void OnSalirMenuClicked()
        {
            var pauseManager = PauseManager.Instance;
            if (pauseManager == null || !pauseManager.IsSpawned)
            {
                Debug.LogError(
                    "No hay un PauseManager activo para solicitar la confirmación de salida."
                );
                return;
            }

            pauseManager.PedirConfirmacionSalirVictoria();
        }
    }
}
