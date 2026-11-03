using UnityEngine;
using UnityEngine.UI;

namespace TDOM.Unity.UI
{
    public class PantallaCarga : CanvasGroupFader
    {
        [SerializeField]
        private RectTransform _spinner;

        [SerializeField]
        private Image _barraProgreso;

        private float _velocidadSpinner;

        public void Configurar(float velocidadSpinnerGradosPorSegundo) =>
            _velocidadSpinner = velocidadSpinnerGradosPorSegundo;

        public void FijarProgreso(float progreso)
        {
            if (_barraProgreso != null)
                _barraProgreso.fillAmount = progreso;
        }

        private void Update()
        {
            if (_spinner == null || !Visible)
                return;

            _spinner.Rotate(0f, 0f, -_velocidadSpinner * Time.unscaledDeltaTime);
        }
    }
}
