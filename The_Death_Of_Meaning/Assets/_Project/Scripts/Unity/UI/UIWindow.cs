using DG.Tweening;
using UnityEngine;
namespace TDOM.Unity
{
    public class UIWindow : MonoBehaviour
    {
        [Header("UI Window")] [SerializeField] private string windowId;

        [Header("UI References")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private CanvasGroup canvasGroup;

        [SerializeField] private bool hideOnStart = true;

        [SerializeField] private float duration = 1f;

        [SerializeField] private Ease easeIn = Ease.InBack;
        [SerializeField] private Ease easeOut = Ease.OutBack;
        public RectTransform rectTransformCanvasGroup => canvasGroup.GetComponent<RectTransform>();

        public RectTransform rectTransformCanvas => canvas.GetComponent<RectTransform>();

        public bool HideOnStart
        {
            get => hideOnStart;
            set => hideOnStart = value;
        }
        public Ease EaseIn => easeIn;
        public Ease EaseOut => easeOut;
        public string WindowId => windowId;
        private void Awake()
        {

        }
        void Start()
        {
            Initialize();
        }
        public virtual void Initialize()
        {
            canvas.gameObject.SetActive(!hideOnStart);
            rectTransformCanvasGroup.localScale = Vector3.zero;
        }
        public virtual void Show()
        {
            canvas.gameObject.SetActive(true);
            rectTransformCanvasGroup.DOScale(Vector3.one, duration).SetUpdate(true).SetEase(easeIn);
        }
        public virtual void Hide()
        {
            rectTransformCanvasGroup.DOScale(Vector3.zero, duration).SetUpdate(true).SetEase(easeOut).OnComplete (() =>
            {
                canvas.gameObject.SetActive(false);
            });
        }
    }
    public static class WindowsIds
    {
        public const string ConnectionMenuUI = "connectionmenuui";
   
    }
}
