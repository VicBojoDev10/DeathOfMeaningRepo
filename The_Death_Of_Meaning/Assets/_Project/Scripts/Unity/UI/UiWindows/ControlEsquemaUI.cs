using TDOM.Unity.UI;
using UnityEngine;
using UnityEngine.UI;

namespace TDOM.Unity
{
    public class ControlEsquemaUI : UIWindow
    {
        [SerializeField]
        private Button backControlButton;

        private void Awake()
        {
            if (firstSelectedObject == null && backControlButton != null)
            {
                firstSelectedObject = backControlButton.gameObject;
            }
        }

        private void OnEnable()
        {
            if (backControlButton != null)
                backControlButton.onClick.AddListener(OnBackButtonClicked);
        }

        private void OnDisable()
        {
            if (backControlButton != null)
                backControlButton.onClick.RemoveListener(OnBackButtonClicked);
        }

        private void OnBackButtonClicked()
        {
            if (UiManager.Instance != null)
            {
                UiManager.Instance.CloseWindow(WindowsIds.ControlEsquemaUI);
                UiManager.Instance.ShowWindow(WindowsIds.OptionsUI);
            }
        }
    }
}
