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
            {
                firstSelectedObject = jugarDenuevoButton.gameObject;
            }
        }

        private void OnEnable()
        {
           

           

            if (salirMenuButton != null)
                salirMenuButton.onClick.AddListener(OnSalirMenuClicked);
        }

        private void OnDisable()
        {
            

           

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
    }
}
