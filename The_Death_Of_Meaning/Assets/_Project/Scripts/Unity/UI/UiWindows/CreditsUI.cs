using TDOM.Unity.UI;
using UnityEngine;
using UnityEngine.UI;

namespace TDOM.Unity
{
    public class CreditsUI : UIWindow
    {
        [SerializeField]
        private Button backCreditsButton;

        private void Awake()
        {
            if (firstSelectedObject == null && backCreditsButton != null)
            {
                firstSelectedObject = backCreditsButton.gameObject;
            }
        }

        private void OnEnable()
        {
            if (backCreditsButton != null)
                backCreditsButton.onClick.AddListener(OnBackCreditsButtonClicked);
        }

        private void OnDisable()
        {
            if (backCreditsButton != null)
                backCreditsButton.onClick.RemoveListener(OnBackCreditsButtonClicked);
        }

        private void OnBackCreditsButtonClicked()
        {
            if (UiManager.Instance != null)
            {
                UiManager.Instance.CloseWindow(WindowsIds.CreditsUI);
                UiManager.Instance.ShowWindow(WindowsIds.OptionsUI);
            }
        }
    }
}
