using TDOM.Unity.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TDOM.Unity.UI
{
    public class PlayerEnergyHud : MonoBehaviour
    {
        [SerializeField] private Slider _energiaJugador;
        public TextMeshProUGUI LabelText;

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

        private void UpdateFill(float currentEnergy)
        {
            if (_energiaJugador != null && _maxEnergy > 0f)
            {
                _energiaJugador.SetValueWithoutNotify(
                    Mathf.Clamp(currentEnergy, 0f, _maxEnergy)
                );
            }
        }
    }
}
