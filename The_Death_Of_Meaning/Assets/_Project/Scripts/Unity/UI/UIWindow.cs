using DG.Tweening;
using UnityEngine;

namespace TDOM.Unity
{
    public class UIWindow : MonoBehaviour
    {
        [Header("UI Window")]
        [SerializeField]
        private string windowId;

        [Header("UI References")]
        [SerializeField]
        protected Canvas canvas;

        [SerializeField]
        protected CanvasGroup canvasGroup;

        [SerializeField]
        private bool hideOnStart = true;

        [SerializeField]
        private float duration = 1f;

        [SerializeField]
        private Ease easeIn = Ease.InBack;

        [SerializeField]
        private Ease easeOut = Ease.OutBack;
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

        public bool IsOpen => canvas != null && canvas.gameObject.activeInHierarchy;

        private void Awake() { }

        void Start()
        {
            Initialize();
        }

        public virtual void SetInteractable(bool isInteractable)
        {
            if (canvasGroup != null)
            {
                canvasGroup.interactable = isInteractable;
                canvasGroup.blocksRaycasts = isInteractable;
            }
        }

        public virtual void Initialize()
        {
            if (canvas != null)
                canvas.gameObject.SetActive(!hideOnStart);

            if (canvasGroup != null)
            {
                rectTransformCanvasGroup.DOKill();
                rectTransformCanvasGroup.localScale = hideOnStart ? Vector3.zero : Vector3.one;
                SetInteractable(!hideOnStart);
            }
        }

        [Header("Navegación")]
        [SerializeField]
        protected GameObject firstSelectedObject;

        private Coroutine _focusRoutine;

        public GameObject FirstSelectedObject
        {
            get => firstSelectedObject;
            set => firstSelectedObject = value;
        }

        public virtual void FocusFirstElement()
        {
            if (firstSelectedObject != null)
            {
                FocusElement(firstSelectedObject);
            }
        }

        public virtual void FocusElement(GameObject target)
        {
            if (target == null)
                return;

            if (!gameObject.activeInHierarchy)
                return;

            if (_focusRoutine != null)
                StopCoroutine(_focusRoutine);

            _focusRoutine = StartCoroutine(RutinaFocusElement(target));
        }

        private System.Collections.IEnumerator RutinaFocusElement(GameObject target)
        {
            yield return null;
            if (target != null && UnityEngine.EventSystems.EventSystem.current != null)
            {
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(target);
            }
            _focusRoutine = null;
        }

        public virtual void Show()
        {
            if (canvas == null || canvasGroup == null)
                return;

            rectTransformCanvasGroup.DOKill();
            canvas.gameObject.SetActive(true);
            SetInteractable(true);
            rectTransformCanvasGroup.DOScale(Vector3.one, duration).SetUpdate(true).SetEase(easeIn);

            FocusFirstElement();
        }

        public virtual void Hide()
        {
            if (canvas == null || canvasGroup == null)
                return;

            SetInteractable(false);
            rectTransformCanvasGroup.DOKill();
            rectTransformCanvasGroup
                .DOScale(Vector3.zero, duration)
                .SetUpdate(true)
                .SetEase(easeOut)
                .OnComplete(() =>
                {
                    if (canvas != null)
                        canvas.gameObject.SetActive(false);
                });
        }

        private void OnDestroy()
        {
            if (canvasGroup != null)
            {
                rectTransformCanvasGroup.DOKill();
            }
        }
    }
}
