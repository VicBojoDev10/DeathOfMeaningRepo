using TDOM.Unity.Player;
using UnityEngine;
using UnityEngine.UI;

namespace TDOM.Unity.UI
{
    public class PlayerEnergyHud : MonoBehaviour
    {
        public Image FillImage;
        public Text LabelText;

        private PlayerRoot _targetRoot;
        private float _maxEnergy;

        public void Initialize(PlayerRoot root, string labelName, float maxEnergy)
        {
            _targetRoot = root;
            _maxEnergy = maxEnergy;

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

        private void UpdateFill(float currentEnergy)
        {
            if (FillImage != null && _maxEnergy > 0)
            {
                FillImage.fillAmount = currentEnergy / _maxEnergy;
            }
        }
    }
}
