using TDOM.Gameplay;
using TDOM.Unity.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TDOM.Unity
{
    public class DerrotaUI : UIWindow
    {
        [SerializeField]
        private TMP_Text faseJefeText;

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

        public void MostrarFaseJefe(int fase)
        {
            if (faseJefeText == null)
            {
                Debug.LogWarning("Asigna el texto de la fase del jefe en DerrotaUI.");
                return;
            }

            faseJefeText.text = $"Fase: {Mathf.Max(1, fase)}";
        }

        public void MostrarRegistro(RegistroPartida registro)
        {
            if (registro == null)
                return;
            MostrarFaseJefe(registro.FaseAlcanzada);
            EstablecerTexto("NumTC", FormatearTiempo(registro.DuracionSegundos));
        }

        private static string FormatearTiempo(float tiempoSegundos)
        {
            int segundosTotales = Mathf.Max(0, Mathf.FloorToInt(tiempoSegundos));
            int horas = segundosTotales / 3600;
            int minutos = (segundosTotales % 3600) / 60;
            int segundos = segundosTotales % 60;
            return horas > 0
                ? $"({horas:00}:{minutos:00}:{segundos:00})"
                : $"({minutos:00}:{segundos:00})";
        }

        private void EstablecerTexto(string nombreObjeto, string valor)
        {
            foreach (var texto in GetComponentsInChildren<TMP_Text>(true))
            {
                if (texto.gameObject.name == nombreObjeto)
                {
                    texto.text = valor;
                    return;
                }
            }
        }

        private void OnJugarDeNuevoClicked()
        {
            var pauseManager = PauseManager.Instance;
            if (pauseManager == null || !pauseManager.IsSpawned)
            {
                Debug.LogError("No hay un PauseManager activo para solicitar la confirmación.");
                return;
            }

            pauseManager.PedirConfirmacionReinicioDerrota();
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

            pauseManager.PedirConfirmacionSalirDerrota();
        }
    }
}
