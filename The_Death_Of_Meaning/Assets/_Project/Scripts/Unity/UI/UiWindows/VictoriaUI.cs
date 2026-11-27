using TDOM.Unity.UI;
using TDOM.Gameplay;
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

        public void MostrarRegistro(RegistroPartida registro)
        {
            if (registro == null)
                return;

            MostrarTiempoFinal(registro.DuracionSegundos);
            EstablecerTexto("NumDT", Formatear(registro.DanoRecibidoTotal));
            EstablecerTexto("NumDIZ", Formatear(DanoHecho(registro, "Zendre")));
            EstablecerTexto("NumDIA", Formatear(DanoHecho(registro, "Ayla")));
            EstablecerTexto("NumVCZ", Formatear(VecesAbatido(registro, "Zendre")));
            EstablecerTexto("NumVCA", Formatear(VecesAbatido(registro, "Ayla")));
        }

        private float DanoHecho(RegistroPartida registro, string personaje)
        {
            float total = 0f;
            foreach (var jugador in registro.Jugadores)
                if (jugador.Personaje == personaje)
                    total += jugador.DanoHecho;
            return total;
        }

        private int VecesAbatido(RegistroPartida registro, string personaje)
        {
            int total = 0;
            foreach (var jugador in registro.Jugadores)
                if (jugador.Personaje == personaje)
                    total += jugador.VecesAbatido;
            return total;
        }

        private static string Formatear(float valor) => $"{Mathf.Max(0f, valor):F0}";
        private static string Formatear(int valor) => Mathf.Max(0, valor).ToString();

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
