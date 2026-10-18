using TDOM.Unity.UI;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine.SceneManagement;

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
            {
                firstSelectedObject = jugarDenuevoButton.gameObject;
            }
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

        

       

        private void OnSalirMenuClicked()
        {
            if (UiManager.Instance != null)
            {
                UiManager.Instance.SwitchWindow(WindowsIds.OptionsUI, WindowsIds.MainMenuUI);
            }
        }

        private void OnJugarDeNuevoClicked()
        {
            Debug.Log("Jugar de nuevo");
        }
    }
}
