using TDOM.Unity.UI;
using UnityEngine;
using UnityEngine.UI;

namespace TDOM.Unity
{
    public class VictoriaUI : UIWindow
    {
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
                Debug.LogError("No hay un PauseManager activo para solicitar la confirmación de salida.");
                return;
            }

            pauseManager.PedirConfirmacionSalirVictoria();
        }
    }
}