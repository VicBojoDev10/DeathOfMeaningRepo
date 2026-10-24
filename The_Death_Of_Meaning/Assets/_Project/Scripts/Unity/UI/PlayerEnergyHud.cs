using TDOM.Unity.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TDOM.Unity.UI
{
    public class PlayerEnergyHud : MonoBehaviour
    {
        [SerializeField]
        private Slider _energiaJugador;
        public TextMeshProUGUI LabelText;

        [Header("Costo pendiente (cargado)")]
        [SerializeField]
        private Color _colorCosto = new Color(1f, 0.85f, 0.2f, 0.85f);

        [SerializeField]
        private Color _colorSinEnergia = new Color(1f, 0.25f, 0.2f, 0.9f);

        [SerializeField, Min(0f)]
        private float _velocidadPulso = 8f;

        private RectTransform _costo;
        private Image _costoImagen;

        private PlayerRoot _targetRoot;
        private float _maxEnergy;

        public void Reinitialize(PlayerRoot root, string labelName, float maxEnergy)
        {
            if (_targetRoot != null)
                _targetRoot.Energia.OnValueChanged -= HandleEnergyChanged;

            _targetRoot = null;
            Initialize(root, labelName, maxEnergy);
        }

        public void Initialize(PlayerRoot root, string labelName, float maxEnergy)
        {
            _targetRoot = root;
            _maxEnergy = maxEnergy;

            if (_energiaJugador == null)
                _energiaJugador = GetComponentInChildren<Slider>(true);

            if (_energiaJugador != null)
            {
                _energiaJugador.minValue = 0f;
                _energiaJugador.maxValue = Mathf.Max(0f, _maxEnergy);
            }

            if (LabelText != null)
                LabelText.text = labelName;

            if (_targetRoot != null)
            {
                _targetRoot.Energia.OnValueChanged += HandleEnergyChanged;
                UpdateFill(_targetRoot.Energia.Value);
            }
        }

        private void OnDestroy()
        {
            if (_targetRoot != null)
            {
                _targetRoot.Energia.OnValueChanged -= HandleEnergyChanged;
            }
        }

        private void HandleEnergyChanged(float prev, float curr)
        {
            UpdateFill(curr);
        }

        public void ActualizarEnergia(float currentEnergy)
        {
            UpdateFill(currentEnergy);
        }

        /// <summary>
        /// Marca sobre la barra cuanta energia se va a gastar (franja que termina en la energia
        /// actual). Con costo 0 se oculta. Se pone roja si no alcanza.
        /// </summary>
        public void MostrarCostoPendiente(float costo, float energiaActual)
        {
            if (costo <= 0f || _maxEnergy <= 0f)
            {
                if (_costo != null && _costo.gameObject.activeSelf)
                    _costo.gameObject.SetActive(false);
                return;
            }

            if (!AsegurarCosto())
                return;

            float actual = Mathf.Clamp(energiaActual, 0f, _maxEnergy);
            float inicio = Mathf.Clamp01((actual - costo) / _maxEnergy);
            float fin = Mathf.Clamp01(actual / _maxEnergy);
            _costo.anchorMin = new Vector2(inicio, 0f);
            _costo.anchorMax = new Vector2(fin, 1f);

            bool alcanza = actual >= costo;
            Color color = alcanza ? _colorCosto : _colorSinEnergia;
            color.a *= 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * _velocidadPulso);
            _costoImagen.color = color;

            if (!_costo.gameObject.activeSelf)
                _costo.gameObject.SetActive(true);
            _costo.SetAsLastSibling();
        }

        // La franja cuelga del mismo contenedor que el relleno del Slider, asi sus anclas
        // coinciden con las del relleno.
        private bool AsegurarCosto()
        {
            if (_costo != null)
                return true;
            if (_energiaJugador == null)
                return false;

            RectTransform padre = null;
            if (_energiaJugador.fillRect != null)
                padre = _energiaJugador.fillRect.parent as RectTransform;
            if (padre == null)
                padre = _energiaJugador.transform as RectTransform;
            if (padre == null)
                return false;

            var go = new GameObject("CostoPendiente", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(padre, false);
            _costo = go.GetComponent<RectTransform>();
            _costo.offsetMin = Vector2.zero;
            _costo.offsetMax = Vector2.zero;
            _costoImagen = go.GetComponent<Image>();
            _costoImagen.raycastTarget = false;
            go.SetActive(false);
            return true;
        }

        private void UpdateFill(float currentEnergy)
        {
            if (_energiaJugador != null && _maxEnergy > 0f)
            {
                _energiaJugador.SetValueWithoutNotify(Mathf.Clamp(currentEnergy, 0f, _maxEnergy));
            }
        }
    }
}
