using TDOM.Unity.UI;
using UnityEngine;
using UnityEngine.UI;

namespace TDOM.Unity
{
    public class ControlEsquemaUI : UIWindow
    {
        [SerializeField]
        private Button backControlButton;

        [SerializeField]
        private Button aylaControlButton;

        [SerializeField]
        private Button zendreControlButton;

        [SerializeField]
        private Image squemImage;

        [SerializeField]
        private Sprite aylaEsquemaSprite;

        [SerializeField]
        private Sprite zendreEsquemaSprite;

        private void Awake()
        {
            if (firstSelectedObject == null && backControlButton != null)
            {
                firstSelectedObject = backControlButton.gameObject;
            }
            squemImage.sprite = aylaEsquemaSprite;
        }

        private void OnEnable()
        {
            if (backControlButton != null)
                backControlButton.onClick.AddListener(OnBackButtonClicked);

            if (aylaControlButton != null)
                aylaControlButton.onClick.AddListener(OnAylaButtonClicked);

            if (zendreControlButton != null)
                zendreControlButton.onClick.AddListener(OnZendreButtonClicked);
        }

        private void OnDisable()
        {
            if (backControlButton != null)
                backControlButton.onClick.RemoveListener(OnBackButtonClicked);
            if (aylaControlButton != null)
                aylaControlButton.onClick.RemoveListener(OnAylaButtonClicked);

            if (zendreControlButton != null)
                zendreControlButton.onClick.RemoveListener(OnZendreButtonClicked);
        }

        private void OnBackButtonClicked()
        {
            if (UiManager.Instance != null)
            {
                UiManager.Instance.SwitchWindow(WindowsIds.ControlEsquemaUI, WindowsIds.OptionsUI);
            }
        }

        private void OnAylaButtonClicked()
        {
            squemImage.sprite = aylaEsquemaSprite;
            aylaControlButton.onClick.RemoveListener(OnAylaButtonClicked);
            aylaControlButton.onClick.AddListener(OnAylaButtonClicked);
        }

        private void OnZendreButtonClicked()
        {
            squemImage.sprite = zendreEsquemaSprite;
            zendreControlButton.onClick.RemoveListener(OnZendreButtonClicked);
            zendreControlButton.onClick.AddListener(OnZendreButtonClicked);
        }
    }
}
