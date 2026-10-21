using TDOM.Gameplay.Core;
using TDOM.Unity.Player;
using UnityEngine;
using UnityEngine.UI;

namespace TDOM.Unity.UI
{
    public class DownedSliderUI : MonoBehaviour
    {
        [SerializeField]
        private PlayerRoot _playerRoot;

        [SerializeField]
        private Slider _slider;

        [SerializeField]
        private Image _fillImage;

        [SerializeField]
        private Canvas _canvas;

        [SerializeField]
        private Vector3 _offset = new Vector3(0f, 2.2f, 0f);

        [SerializeField]
        private Color _colorAbatido = new Color(1f, 0.35f, 0.1f, 1f);

        [SerializeField]
        private Color _colorReviviendo = new Color(0.2f, 0.95f, 0.5f, 1f);

        private UnityEngine.Camera _camaraPrincipal;

        private void Awake()
        {
            if (_playerRoot == null)
                _playerRoot = GetComponentInParent<PlayerRoot>();

            ConstruirComponentesSiEsNecesario();
        }

        private void Start()
        {
            ActualizarCamara();
            SuscribirEventos();
            ActualizarEstadoVisual();
        }

        private void OnDestroy()
        {
            DesuscribirEventos();
        }

        private void SuscribirEventos()
        {
            if (_playerRoot == null)
                return;

            _playerRoot.EstadoVida.OnValueChanged += OnEstadoVidaChanged;
            _playerRoot.TiempoAbatidoRestante.OnValueChanged += OnTiempoAbatidoChanged;
            _playerRoot.ProgresoRevivir.OnValueChanged += OnProgresoRevivirChanged;
        }

        private void DesuscribirEventos()
        {
            if (_playerRoot == null)
                return;

            _playerRoot.EstadoVida.OnValueChanged -= OnEstadoVidaChanged;
            _playerRoot.TiempoAbatidoRestante.OnValueChanged -= OnTiempoAbatidoChanged;
            _playerRoot.ProgresoRevivir.OnValueChanged -= OnProgresoRevivirChanged;
        }

        private void OnEstadoVidaChanged(PlayerLifeState anterior, PlayerLifeState actual)
        {
            ActualizarEstadoVisual();
        }

        private void OnTiempoAbatidoChanged(float anterior, float actual)
        {
            if (_playerRoot != null && _playerRoot.ProgresoRevivir.Value <= 0f)
            {
                ActualizarProgreso(_playerRoot.TiempoAbatidoNormalizado, _colorAbatido);
            }
        }

        private void OnProgresoRevivirChanged(float anterior, float actual)
        {
            if (actual > 0f)
            {
                ActualizarProgreso(actual, _colorReviviendo);
            }
            else if (_playerRoot != null)
            {
                ActualizarProgreso(_playerRoot.TiempoAbatidoNormalizado, _colorAbatido);
            }
        }

        private void ActualizarEstadoVisual()
        {
            if (_playerRoot == null)
                return;

            bool estaAbatido = _playerRoot.EstadoVida.Value == PlayerLifeState.Abatido;
            if (_canvas != null && _canvas.gameObject.activeSelf != estaAbatido)
            {
                _canvas.gameObject.SetActive(estaAbatido);
            }

            if (estaAbatido)
            {
                float progreso = _playerRoot.ProgresoRevivir.Value;
                if (progreso > 0f)
                    ActualizarProgreso(progreso, _colorReviviendo);
                else
                    ActualizarProgreso(_playerRoot.TiempoAbatidoNormalizado, _colorAbatido);
            }
        }

        private void ActualizarProgreso(float valor, Color color)
        {
            if (_slider != null)
                _slider.value = Mathf.Clamp01(valor);

            if (_fillImage != null)
                _fillImage.color = color;
        }

        private void LateUpdate()
        {
            if (_canvas == null || !_canvas.gameObject.activeSelf)
                return;

            _canvas.transform.position = transform.position + _offset;

            if (_camaraPrincipal == null)
                ActualizarCamara();

            if (_camaraPrincipal != null)
            {
                _canvas.transform.forward = _camaraPrincipal.transform.forward;
            }
        }

        private void ActualizarCamara()
        {
            _camaraPrincipal = UnityEngine.Camera.main;
        }

        private void ConstruirComponentesSiEsNecesario()
        {
            if (_canvas != null && _slider != null)
                return;

            var canvasGo = new GameObject("DownedCanvas");
            canvasGo.transform.SetParent(transform, false);
            canvasGo.transform.localPosition = _offset;

            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;

            var rectTransform = _canvas.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(160f, 24f);
            rectTransform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

            var sliderGo = new GameObject("DownedSlider");
            sliderGo.transform.SetParent(canvasGo.transform, false);

            var sliderRect = sliderGo.AddComponent<RectTransform>();
            sliderRect.anchorMin = Vector2.zero;
            sliderRect.anchorMax = Vector2.one;
            sliderRect.sizeDelta = Vector2.zero;

            _slider = sliderGo.AddComponent<Slider>();
            _slider.minValue = 0f;
            _slider.maxValue = 1f;
            _slider.interactable = false;

            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(sliderGo.transform, false);
            var bgRect = bgGo.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            var bgImage = bgGo.AddComponent<Image>();
            bgImage.color = new Color(0.08f, 0.08f, 0.08f, 0.85f);

            var fillAreaGo = new GameObject("Fill Area");
            fillAreaGo.transform.SetParent(sliderGo.transform, false);
            var fillAreaRect = fillAreaGo.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0.15f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.85f);
            fillAreaRect.offsetMin = new Vector2(3f, 0f);
            fillAreaRect.offsetMax = new Vector2(-3f, 0f);

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(fillAreaGo.transform, false);
            var fillRect = fillGo.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;

            _fillImage = fillGo.AddComponent<Image>();
            _fillImage.color = _colorAbatido;

            _slider.targetGraphic = bgImage;
            _slider.fillRect = fillRect;

            _canvas.gameObject.SetActive(false);
        }
    }
}
